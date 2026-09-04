using System.Diagnostics;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.NotaVenta.DTOs;
using IdeatecAPI.Application.Features.Productos.Services;
using Microsoft.Extensions.Logging;

namespace IdeatecAPI.Application.Features.NotaVenta.Services;

public interface INotaVentaService
{
    Task<NotaVentaResponse> GenerarNotaVentaAsync(GenerarNotaVentaDTO dto);
    Task<IEnumerable<ListarComprobanteDTO>> ListarNotasVentaAsync(int sucursalId, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null);
    Task<NotaVentaResponse> AnularNotaVentaAsync(int comprobanteId, string? motivo, int? usuarioId);
    Task<NotaVentaResponse> DevolverItemNotaVentaAsync(int comprobanteId, int detalleId, decimal cantidad, string? motivo, int? usuarioId);
}

public class NotaVentaResponse
{
    public bool Exitoso { get; set; }
    public string? Mensaje { get; set; }
    public int? ComprobanteId { get; set; }
    public string? NumeroCompleto { get; set; }
}

public class NotaVentaService : INotaVentaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductoService _productoService;
    private readonly ILogger<NotaVentaService> _logger;

    public NotaVentaService(IUnitOfWork unitOfWork, IProductoService productoService, ILogger<NotaVentaService> logger)
    {
        _unitOfWork = unitOfWork;
        _productoService = productoService;
        _logger = logger;
    }

    public async Task<NotaVentaResponse> GenerarNotaVentaAsync(GenerarNotaVentaDTO dto)
    {
        if (dto.Detalles == null || dto.Detalles.Count == 0)
            throw new InvalidOperationException("La nota de venta debe tener al menos un detalle.");

        // Medicion por fase: contra una BD remota el costo dominante es la cantidad de
        // viajes de red, no el trabajo en el servidor. Este desglose dice en que fase se
        // van los milisegundos sin tener que adivinar.
        var cronometro = Stopwatch.StartNew();
        var marcas = new List<(string Fase, long Ms)>();
        void Marcar(string fase)
        {
            marcas.Add((fase, cronometro.ElapsedMilliseconds));
            cronometro.Restart();
        }

        _unitOfWork.BeginTransaction();
        Marcar("beginTransaction");
        try
        {
            // Un solo viaje: valida empresa activa, ubica la sucursal por RUC+anexo y reserva
            // el correlativo. Antes eran cuatro consultas, y la de empresa hacia SELECT * sobre
            // una tabla que guarda logos y certificados en base64 para luego descartar el
            // resultado: solo se usaba como comprobacion de existencia.
            var asignacion = await _unitOfWork.Comprobantes.AsignarSerieYCorrelativoAsync(
                dto.Company.NumeroDocumento!,
                dto.Company.EstablecimientoAnexo!,
                "NV"
            ) ?? throw new KeyNotFoundException("No se encontró sucursal activa para el RUC y establecimiento indicados, o la empresa no está activa.");

            var serieNV = asignacion.Serie
                ?? throw new InvalidOperationException("La sucursal no tiene configurada una serie para Nota de Venta.");
            var correlativo = asignacion.Correlativo;
            Marcar("serieYCorrelativo");

            var correlativoStr = correlativo.ToString().PadLeft(8, '0');
            var numeroCompleto = $"{serieNV}-{correlativoStr}";

            var comprobante = new Domain.Entities.Comprobante
            {
                TipoOperacion           = null,
                TipoComprobante         = "NV",
                Serie                   = serieNV,
                Correlativo             = correlativo,
                NumeroCompleto          = numeroCompleto,
                FechaEmision            = dto.FechaEmision,
                HoraEmision             = dto.FechaEmision,
                FechaVencimiento        = dto.FechaVencimiento,
                TipoMoneda              = dto.TipoMoneda,
                TipoCambio              = dto.TipoCambio,
                TipoPago                = dto.TipoPago,
                Observaciones           = dto.Observaciones,

                EmpresaId                    = dto.Company.EmpresaId,
                EmpresaRuc                   = dto.Company.NumeroDocumento,
                EmpresaRazonSocial           = dto.Company.RazonSocial,
                EmpresaNombreComercial        = dto.Company.NombreComercial,
                EmpresaEstablecimientoAnexo  = dto.Company.EstablecimientoAnexo,
                EmpresaDireccion             = dto.Company.DireccionLineal,
                EmpresaProvincia             = dto.Company.Provincia,
                EmpresaDepartamento          = dto.Company.Departamento,
                EmpresaDistrito              = dto.Company.Distrito,
                EmpresaUbigeo                = dto.Company.Ubigeo,

                ClienteId           = dto.Cliente?.ClienteId,
                ClienteTipoDoc      = dto.Cliente?.TipoDocumento,
                ClienteNumDoc       = dto.Cliente?.NumeroDocumento,
                ClienteRazonSocial  = dto.Cliente?.RazonSocial,
                ClienteDireccion    = dto.Cliente?.DireccionLineal,
                ClienteProvincia    = dto.Cliente?.Provincia,
                ClienteDepartamento = dto.Cliente?.Departamento,
                ClienteDistrito     = dto.Cliente?.Distrito,
                ClienteUbigeo       = dto.Cliente?.Ubigeo,
                ClienteCorreo       = dto.Cliente?.Correo,
                EnviadoPorCorreo    = dto.Cliente?.EnviadoPorCorreo,
                ClienteWhatsApp     = dto.Cliente?.WhatsApp,
                EnviadoPorWhatsApp  = dto.Cliente?.EnviadoPorWhatsApp,

                DescuentoGlobal = dto.DescuentoGlobal,
                TotalDescuentos = dto.TotalDescuentos,
                TotalIGV        = dto.TotalIGV,
                ValorVenta      = dto.ValorVenta,
                SubTotal        = dto.SubTotal,
                ImporteTotal    = dto.ImporteTotal,
                MontoCredito    = dto.MontoCredito,
                TotalComisionPagoTarjeta = dto.TotalComisionPagoTarjeta,

                // Campos SUNAT — no aplica para NV
                EstadoSunat    = "NO_APLICA",
                XmlGenerado    = null,
                CodigoHashCPE  = null,

                UsuarioCreacion = dto.UsuarioCreacion,
                FechaCreacion   = AhoraLima(),

                Detalles = dto.Detalles.Select(d => new Domain.Entities.ComprobanteDetalle
                {
                    Item             = d.Item,
                    ProductoId       = d.ProductoId,
                    TrabajadorID     = d.TrabajadorId,
                    Codigo           = d.Codigo,
                    Descripcion      = d.Descripcion,
                    Cantidad         = d.Cantidad,
                    UnidadMedida     = d.UnidadMedida,
                    PrecioUnitario   = d.PrecioUnitario,
                    CodigoTipoDescuento = "00",
                    DescuentoUnitario = d.DescuentoUnitario ?? 0,
                    DescuentoTotal   = d.DescuentoTotal ?? 0,
                    ValorVenta       = d.TotalVentaItem,
                    MontoIGV         = 0,
                    BaseIgv          = 0,
                    PrecioVenta      = d.PrecioVenta,
                    TotalVentaItem   = d.TotalVentaItem,
                    TipoAfectacionIGV = "20",
                    PorcentajeIGV    = 0,
                    Icbper           = 0,
                    FactorIcbper     = 0
                }).ToList(),

                Pagos = dto.Pagos?.Select(p => new Domain.Entities.Pago
                {
                    MedioPago          = p.MedioPago,
                    Monto              = p.Monto,
                    FechaPago          = p.FechaPago,
                    NumeroOperacion    = p.NumeroOperacion,
                    EntidadFinanciera  = p.EntidadFinanciera,
                    Observaciones      = p.Observaciones
                }).ToList() ?? [],

                Cuotas = dto.Cuotas?.Select(c => new Domain.Entities.Cuota
                {
                    NumeroCuota     = c.NumeroCuota,
                    Monto           = c.Monto,
                    FechaVencimiento = c.FechaVencimiento,
                    MontoPagado     = c.MontoPagado,
                    FechaPago       = c.FechaPago,
                    Estado          = c.Estado
                }).ToList() ?? [],

                Leyendas    = [],
                Guias       = [],
                Detracciones = []
            };

            var newId = await _unitOfWork.Comprobantes.GenerarComprobanteAsync(comprobante);
            Marcar("insertComprobante");

            // Descuento de stock ATÓMICO: ocurre dentro de esta misma transacción.
            // Si algún producto no tiene stock suficiente, se lanza excepción y el
            // catch hace Rollback => la nota de venta NO queda registrada. Así nunca
            // se crea una venta sin respaldo de stock (evita sobreventa).
            if (dto.StockItems is { Count: > 0 })
            {
                // Resuelve, en un solo viaje, a qué detalleID quedó cada línea recién
                // insertada, para que el kardex pueda distinguir (p.ej.) la venta de un
                // paquete de la venta de su producto base en vez de fusionarlas.
                var itemADetalleId = await _unitOfWork.Comprobantes.GetItemDetalleIdMapAsync(newId);

                foreach (var it in dto.StockItems)
                {
                    it.ReferenciaTipo = "COMPROBANTE";
                    it.ReferenciaId = newId;
                    if (it.Item is int item && itemADetalleId.TryGetValue(item, out var detalleId))
                        it.ComprobanteDetalleId = detalleId;
                }
                await _productoService.DescontarStockEnTransaccionAsync(dto.StockItems, "SALIDA_VENTA");
            }
            Marcar("stockYKardex");

            _unitOfWork.Commit();
            Marcar("commit");

            _logger.LogInformation(
                "Nota de venta {Numero} con {Detalles} detalle(s) en {Total} ms | {Desglose}",
                numeroCompleto,
                dto.Detalles.Count,
                marcas.Sum(m => m.Ms),
                string.Join(", ", marcas.Select(m => $"{m.Fase}={m.Ms}ms")));

            return new NotaVentaResponse
            {
                Exitoso        = true,
                Mensaje        = "Nota de venta guardada correctamente.",
                ComprobanteId  = newId,
                NumeroCompleto = numeroCompleto
            };
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<ListarComprobanteDTO>> ListarNotasVentaAsync(
        int sucursalId, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null)
    {
        var sucursal = await _unitOfWork.Sucursal.GetByIdSucursalAsync(sucursalId);

        var comprobantes = await _unitOfWork.Comprobantes.GetNotasVentaBySucursalAsync(
            sucursal.EmpresaRuc ?? throw new InvalidOperationException("La sucursal no tiene RUC."),
            sucursal.CodEstablecimiento ?? throw new InvalidOperationException("La sucursal no tiene código de establecimiento."),
            fechaDesde,
            fechaHasta,
            limit,
            offset);

        return comprobantes.Select(MapToListarDto);
    }

    public async Task<NotaVentaResponse> AnularNotaVentaAsync(int comprobanteId, string? motivo, int? usuarioId)
    {
        var comprobante = await _unitOfWork.Comprobantes.GetComprobanteByIdAsync(comprobanteId)
            ?? throw new KeyNotFoundException("Nota de venta no encontrada.");

        if (comprobante.TipoComprobante != "NV")
            throw new InvalidOperationException("Solo se pueden anular notas de venta desde esta operación.");

        if (comprobante.EstadoSunat == "ANULADO")
            throw new InvalidOperationException("La nota de venta ya se encuentra anulada.");

        _unitOfWork.BeginTransaction();
        try
        {
            // Revierte el stock de forma atómica junto con el cambio de estado: si algo falla,
            // ni el stock se devuelve ni la nota queda marcada como anulada.
            await _productoService.RevertirStockPorReferenciaEnTransaccionAsync("COMPROBANTE", comprobanteId);

            await _unitOfWork.Comprobantes.AnularComprobanteAsync(comprobanteId, motivo, usuarioId);

            _unitOfWork.Commit();

            return new NotaVentaResponse
            {
                Exitoso        = true,
                Mensaje        = "Nota de venta anulada correctamente.",
                ComprobanteId  = comprobanteId,
                NumeroCompleto = comprobante.NumeroCompleto
            };
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<NotaVentaResponse> DevolverItemNotaVentaAsync(int comprobanteId, int detalleId, decimal cantidad, string? motivo, int? usuarioId)
    {
        if (cantidad <= 0)
            throw new InvalidOperationException("La cantidad a devolver debe ser mayor a 0.");

        var comprobante = await _unitOfWork.Comprobantes.GetComprobanteByIdAsync(comprobanteId)
            ?? throw new KeyNotFoundException("Nota de venta no encontrada.");

        if (comprobante.TipoComprobante != "NV")
            throw new InvalidOperationException("Solo se pueden devolver artículos de notas de venta desde esta operación.");

        if (comprobante.EstadoSunat == "ANULADO")
            throw new InvalidOperationException("La nota de venta ya se encuentra anulada.");

        var detalles = (await _unitOfWork.Comprobantes.GetDetallesByIdAsync(comprobanteId)).ToList();
        var detalle = detalles.FirstOrDefault(d => d.DetalleId == detalleId)
            ?? throw new KeyNotFoundException("El artículo indicado no pertenece a esta nota de venta.");

        if (cantidad > detalle.Cantidad)
            throw new InvalidOperationException("La cantidad a devolver no puede superar la cantidad vendida.");

        // No se permite devolver por completo el único artículo que sigue activo: para eso
        // está "Anular venta". Así se garantiza que Anular siempre se encuentre, como mucho,
        // con líneas ya llevadas a 0 individualmente, nunca con la única línea viva.
        var itemsActivos = detalles.Where(d => d.Cantidad > 0).ToList();
        if (itemsActivos.Count == 1 && itemsActivos[0].DetalleId == detalleId && cantidad == detalle.Cantidad)
            throw new InvalidOperationException("No puedes devolver el único artículo de la nota; usa \"Anular venta\" para anularla por completo.");

        _unitOfWork.BeginTransaction();
        try
        {
            await _productoService.RevertirStockPorDetalleEnTransaccionAsync(detalleId, cantidad, "COMPROBANTE", comprobanteId);

            var nuevaCantidad = detalle.Cantidad - cantidad;
            var factor = detalle.Cantidad == 0 ? 0 : nuevaCantidad / detalle.Cantidad;
            var nuevoValorVenta = Math.Round((detalle.ValorVenta ?? 0) * factor, 2);
            var nuevoTotalVentaItem = Math.Round((detalle.TotalVentaItem ?? 0) * factor, 2);
            var nuevoDescuentoTotal = Math.Round((detalle.DescuentoTotal ?? 0) * factor, 2);

            await _unitOfWork.Comprobantes.ActualizarCantidadDetalleAsync(detalleId, nuevaCantidad, nuevoValorVenta, nuevoTotalVentaItem, nuevoDescuentoTotal);
            await _unitOfWork.Comprobantes.RecalcularTotalesComprobanteAsync(comprobanteId, usuarioId);

            // El medio de pago registrado (Efectivo, Yape, etc.) no se ajusta solo: se le resta
            // exactamente lo que bajó el total, empezando por el primer pago registrado, hasta
            // agotar la diferencia (igual que se le devolvería el cambio en caja).
            var montoADescontar = (detalle.TotalVentaItem ?? 0) - nuevoTotalVentaItem;
            if (montoADescontar > 0)
            {
                var pagos = await _unitOfWork.Comprobantes.GetPagosByIdAsync(comprobanteId);
                foreach (var pago in pagos)
                {
                    if (montoADescontar <= 0)
                        break;

                    var montoActual = pago.Monto ?? 0;
                    var reduccion = Math.Min(montoActual, montoADescontar);
                    if (reduccion <= 0)
                        continue;

                    await _unitOfWork.Comprobantes.ActualizarMontoPagoAsync(pago.PagoId, montoActual - reduccion);
                    montoADescontar -= reduccion;
                }
            }

            _unitOfWork.Commit();

            return new NotaVentaResponse
            {
                Exitoso        = true,
                Mensaje        = "Artículo devuelto correctamente.",
                ComprobanteId  = comprobanteId,
                NumeroCompleto = comprobante.NumeroCompleto
            };
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    private static ListarComprobanteDTO MapToListarDto(Domain.Entities.Comprobante c) => new()
    {
        ComprobanteId    = c.ComprobanteId,
        TipoOperacion    = c.TipoOperacion ?? "",
        TipoComprobante  = c.TipoComprobante,
        Serie            = c.Serie ?? "",
        Correlativo      = c.Correlativo?.ToString() ?? "",
        NumeroCompleto   = c.NumeroCompleto ?? "",
        TipoCambio       = c.TipoCambio ?? 0,
        FechaEmision     = c.FechaEmision,
        HoraEmision      = c.HoraEmision,
        FechaVencimiento = c.FechaVencimiento,
        TipoMoneda       = c.TipoMoneda ?? "PEN",
        TipoPago         = c.TipoPago,
        Observaciones    = c.Observaciones,

        Cliente = new ClienteDTO
        {
            ClienteId         = c.ClienteId,
            TipoDocumento     = c.ClienteTipoDoc,
            NumeroDocumento   = c.ClienteNumDoc,
            RazonSocial       = c.ClienteRazonSocial,
            DireccionLineal   = c.ClienteDireccion,
            Provincia         = c.ClienteProvincia,
            Departamento      = c.ClienteDepartamento,
            Distrito          = c.ClienteDistrito,
            Ubigeo            = c.ClienteUbigeo,
            Correo            = c.ClienteCorreo,
            EnviadoPorCorreo  = c.EnviadoPorCorreo,
            WhatsApp          = c.ClienteWhatsApp,
            EnviadoPorWhatsApp = c.EnviadoPorWhatsApp
        },

        Company = new EmpresaDTO
        {
            EmpresaId            = c.EmpresaId,
            NumeroDocumento      = c.EmpresaRuc,
            RazonSocial          = c.EmpresaRazonSocial,
            NombreComercial      = c.EmpresaNombreComercial,
            EstablecimientoAnexo = c.EmpresaEstablecimientoAnexo,
            DireccionLineal      = c.EmpresaDireccion,
            Provincia            = c.EmpresaProvincia,
            Departamento         = c.EmpresaDepartamento,
            Distrito             = c.EmpresaDistrito,
            Ubigeo               = c.EmpresaUbigeo
        },

        DescuentoGlobal  = c.DescuentoGlobal ?? 0,
        TotalDescuentos  = c.TotalDescuentos ?? 0,
        TotalIGV         = c.TotalIGV ?? 0,
        TotalImpuestos   = c.TotalImpuestos ?? 0,
        ValorVenta       = c.ValorVenta ?? 0,
        SubTotal         = c.SubTotal ?? 0,
        ImporteTotal     = c.ImporteTotal ?? 0,
        MontoCredito     = c.MontoCredito ?? 0,
        TotalComisionPagoTarjeta = c.TotalComisionPagoTarjeta,
        EstadoSunat      = c.EstadoSunat,
        UsuarioCreacion  = c.UsuarioCreacion,
        FechaCreacion    = c.FechaCreacion,
        FechaModificacion = c.FechaModificacion
    };

    private static DateTime AhoraLima()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Lima");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
    }
}
