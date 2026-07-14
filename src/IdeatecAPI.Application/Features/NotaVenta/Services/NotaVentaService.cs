using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.NotaVenta.DTOs;

namespace IdeatecAPI.Application.Features.NotaVenta.Services;

public interface INotaVentaService
{
    Task<NotaVentaResponse> GenerarNotaVentaAsync(GenerarNotaVentaDTO dto);
    Task<IEnumerable<ListarComprobanteDTO>> ListarNotasVentaAsync(int sucursalId, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null);
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

    public NotaVentaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<NotaVentaResponse> GenerarNotaVentaAsync(GenerarNotaVentaDTO dto)
    {
        if (dto.Detalles == null || dto.Detalles.Count == 0)
            throw new InvalidOperationException("La nota de venta debe tener al menos un detalle.");

        _ = await _unitOfWork.Empresas.GetEmpresaByRucAsync(dto.Company.NumeroDocumento ?? "")
            ?? throw new KeyNotFoundException($"Empresa con RUC {dto.Company.NumeroDocumento} no encontrada.");

        _unitOfWork.BeginTransaction();
        try
        {
            var sucursalId = await _unitOfWork.Comprobantes.GetSucursalIdByRucAndAnexoAsync(
                dto.Company.NumeroDocumento!,
                dto.Company.EstablecimientoAnexo!
            ) ?? throw new KeyNotFoundException("No se encontró sucursal activa para el RUC y establecimiento indicados.");

            // Leer serie de NV directamente desde la sucursal
            var sucursal = await _unitOfWork.Sucursal.GetByIdSucursalAsync(sucursalId);
            var serieNV = sucursal.SerieNotaVenta
                ?? throw new InvalidOperationException("La sucursal no tiene configurada una serie para Nota de Venta.");

            // Obtener correlativo y actualizar atomicamente (SELECT FOR UPDATE + UPDATE dentro de la transacción)
            var correlativo = await _unitOfWork.Comprobantes.ObtenerYIncrementarCorrelativoAsync(sucursalId, "NV", serieNV);
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
            _unitOfWork.Commit();

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
