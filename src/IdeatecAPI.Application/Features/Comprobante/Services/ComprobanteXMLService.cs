using System.IO.Compression;
using System.Text;
using IdeatecAPI.Application.Common.Interfaces;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Detraccion.DTOs;
using IdeatecAPI.Application.Features.Notas.DTOs;
using IdeatecAPI.Application.Features.Notas.Services;
using Microsoft.Extensions.Configuration;

namespace IdeatecAPI.Application.Features.Comprobante.Services;

public interface IComprobanteService
{
    //comprobantes por ruc
    Task<IEnumerable<ObtenerComprobanteDTO>> GetByRucAndFechasAsync(string ruc, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null);

    //Comprobantes de un cliente en un ruc
    Task<IEnumerable<ObtenerComprobanteDTO>> GetByDocClienteAndFechasAsync(string rucEmpresa, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta);

    //comprobantes por usuario de un ruc
    Task<IEnumerable<ObtenerComprobanteDTO>> GetByDocUsuarioAndFechasAsync(string rucEmpresa, int usuarioCreacion, DateTime? fechaDesde, DateTime? fechaHasta);

    //comprobantes de una sucursal, la empresa se obtiene internamente de esa sucursal
    Task<IEnumerable<ObtenerComprobanteDTO>> GetBySucursalAndFechasAsync(int sucursalId, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null);


    //obtener Listado de campos solo de la tabla comrpobantes sin su relaciones
    Task<IEnumerable<ListarComprobanteDTO>> GetListadoByRucAndFechasAsync(string ruc, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null);
    Task<IEnumerable<ListarComprobanteDTO>> GetListadoByDocClienteAndFechasAsync(string rucEmpresa, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta);
    Task<IEnumerable<ListarComprobanteDTO>> GetListadoByDocUsuarioAndFechasAsync(string rucEmpresa, int usuarioCreacion, DateTime? fechaDesde, DateTime? fechaHasta);
    Task<IEnumerable<ListarComprobanteDTO>> GetListadoBySucursalAndFechasAsync(int sucursalId, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null);
    Task<IEnumerable<ListarComprobanteDTO>> GetListadoByClienteAndSucursalAsync(int sucursalId, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta);

    //Traer solo detalles de un comprobante
    Task<ComprobanteDetallesDTO?> GetDetallesByComprobanteIdAsync(int comprobanteId);

    //Comprobante unico por ruc estado sunat ACEPTADO
    Task<ObtenerComprobanteDTO?> GetByRucSerieNumeroAsync(string ruc, string serie, int numero);

    //Comprobante unico por ruc estado todos
    Task<ObtenerComprobanteDTO?> GetByComprobanteUnicoAsync(string ruc, string serie, int numero);

    Task ActualizarCorreoWhatsappAsync(int comprobanteId, ActualizarCorreoWhatsappDTO dto);
    Task<int> GetCantidadByClienteNumDocAsync(string clienteNumDoc);
    Task<ComprobanteResponse> GenerarComprobanteAsync(GenerarComprobanteDTO dto); //Guardar en BD
    Task<ObtenerComprobanteDTO?> GetComprobanteByIdAsync(int comprobanteId);
    Task<IEnumerable<ObtenerComprobanteDTO>> GetComprobanteByEstadoAsync(string estado);
    Task<ComprobanteResponse> SendToSunatAsync(int comprobanteId); // Generar XML, firmar y enviar a sunat
}

public class ComprobanteResponse
{
    public bool Exitoso { get; set; }
    public string? Mensaje { get; set; }
    public int? ComprobanteId { get; set; }
    public string? XmlBase64 { get; set; }
    public string? XmlString { get; set; }
    public string? RutaZip { get; set; }
    public string? EstadoSunat { get; set; }
    public string? CodigoRespuesta { get; set; }
    public string? MensajeRespuesta { get; set; }
    public string? CdrBase64 { get; set; }
}

public class ComprobanteService : IComprobanteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IComprobanteXmlService _xmlService;
    private readonly IXmlSignerService _xmlSigner;
    private readonly ISunatSenderService _sunatSender;
    private readonly IConfiguration _configuration;
    //private readonly IWebSocketNotifier _wsNotifier;
    private readonly string _rutaXml;

    public ComprobanteService(
        IUnitOfWork unitOfWork,
        IComprobanteXmlService xmlService,
        IXmlSignerService xmlSigner,
        ISunatSenderService sunatSender,
        //IWebSocketNotifier wsNotifier,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _xmlService = xmlService;
        _xmlSigner = xmlSigner;
        _sunatSender = sunatSender;
        _configuration = configuration;
        //_wsNotifier = wsNotifier;
        _rutaXml = configuration["Storage:RutaXml"] ?? Path.Combine(Directory.GetCurrentDirectory(), "XmlFiles");
    }

    public async Task<IEnumerable<ObtenerComprobanteDTO>> GetByRucAndFechasAsync(string ruc, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null)
    {
        var comprobantes = await _unitOfWork.Comprobantes.GetByRucAndFechasAsync(ruc, fechaDesde, fechaHasta, limit);

        var lista = new List<ObtenerComprobanteDTO>();
        foreach (var comprobante in comprobantes)
        {
            var detalles = (await _unitOfWork.Comprobantes.GetDetallesByIdAsync(comprobante.ComprobanteId)).ToList();
            var pagos = (await _unitOfWork.Comprobantes.GetPagosByIdAsync(comprobante.ComprobanteId)).ToList();
            var cuotas = (await _unitOfWork.Comprobantes.GetCuotasByIdAsync(comprobante.ComprobanteId)).ToList();
            var leyendas = (await _unitOfWork.Comprobantes.GetLeyendasByIdAsync(comprobante.ComprobanteId)).ToList();
            var guias = (await _unitOfWork.Comprobantes.GetGuiasByIdAsync(comprobante.ComprobanteId)).ToList();
            var detracciones = (await _unitOfWork.Comprobantes.GetDetraccionesByIdAsync(comprobante.ComprobanteId)).ToList();
            lista.Add(MapToDto(comprobante, detalles, pagos, cuotas, leyendas, guias, detracciones));
        }
        return lista;
    }

    public async Task<IEnumerable<ObtenerComprobanteDTO>> GetByDocClienteAndFechasAsync(string rucEmpresa, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var comprobantes = await _unitOfWork.Comprobantes.GetByDocClienteAndFechasAsync(rucEmpresa, clienteNumDoc, fechaDesde, fechaHasta);

        var lista = new List<ObtenerComprobanteDTO>();
        foreach (var comprobante in comprobantes)
        {
            var detalles = (await _unitOfWork.Comprobantes.GetDetallesByIdAsync(comprobante.ComprobanteId)).ToList();
            var pagos = (await _unitOfWork.Comprobantes.GetPagosByIdAsync(comprobante.ComprobanteId)).ToList();
            var cuotas = (await _unitOfWork.Comprobantes.GetCuotasByIdAsync(comprobante.ComprobanteId)).ToList();
            var leyendas = (await _unitOfWork.Comprobantes.GetLeyendasByIdAsync(comprobante.ComprobanteId)).ToList();
            var guias = (await _unitOfWork.Comprobantes.GetGuiasByIdAsync(comprobante.ComprobanteId)).ToList();
            var detracciones = (await _unitOfWork.Comprobantes.GetDetraccionesByIdAsync(comprobante.ComprobanteId)).ToList();
            lista.Add(MapToDto(comprobante, detalles, pagos, cuotas, leyendas, guias, detracciones));
        }
        return lista;
    }

    public async Task<IEnumerable<ObtenerComprobanteDTO>> GetBySucursalAndFechasAsync(int sucursalId, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null)
    {
        // 1. Obtener sucursal para extraer ruc y codEstablecimiento
        var sucursal = await _unitOfWork.Sucursal.GetByIdSucursalAsync(sucursalId);

        // 2. Buscar comprobantes con esos datos
        var comprobantes = await _unitOfWork.Comprobantes.GetBySucursalAndFechasAsync(
            sucursal.EmpresaRuc ?? throw new InvalidOperationException("La sucursal no tiene RUC"),
            sucursal.CodEstablecimiento ?? throw new InvalidOperationException("La sucursal no tiene código de establecimiento"),
            fechaDesde,
            fechaHasta,
            limit);

        var lista = new List<ObtenerComprobanteDTO>();
        foreach (var comprobante in comprobantes)
        {
            var detalles = (await _unitOfWork.Comprobantes.GetDetallesByIdAsync(comprobante.ComprobanteId)).ToList();
            var pagos = (await _unitOfWork.Comprobantes.GetPagosByIdAsync(comprobante.ComprobanteId)).ToList();
            var cuotas = (await _unitOfWork.Comprobantes.GetCuotasByIdAsync(comprobante.ComprobanteId)).ToList();
            var leyendas = (await _unitOfWork.Comprobantes.GetLeyendasByIdAsync(comprobante.ComprobanteId)).ToList();
            var guias = (await _unitOfWork.Comprobantes.GetGuiasByIdAsync(comprobante.ComprobanteId)).ToList();
            var detracciones = (await _unitOfWork.Comprobantes.GetDetraccionesByIdAsync(comprobante.ComprobanteId)).ToList();
            lista.Add(MapToDto(comprobante, detalles, pagos, cuotas, leyendas, guias, detracciones));
        }
        return lista;
    }

    public async Task<IEnumerable<ObtenerComprobanteDTO>> GetByDocUsuarioAndFechasAsync(string rucEmpresa, int usuarioCreacion, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var comprobantes = await _unitOfWork.Comprobantes.GetByDocUsuarioAndFechasAsync(rucEmpresa, usuarioCreacion, fechaDesde, fechaHasta);

        var lista = new List<ObtenerComprobanteDTO>();
        foreach (var comprobante in comprobantes)
        {
            var detalles = (await _unitOfWork.Comprobantes.GetDetallesByIdAsync(comprobante.ComprobanteId)).ToList();
            var pagos = (await _unitOfWork.Comprobantes.GetPagosByIdAsync(comprobante.ComprobanteId)).ToList();
            var cuotas = (await _unitOfWork.Comprobantes.GetCuotasByIdAsync(comprobante.ComprobanteId)).ToList();
            var leyendas = (await _unitOfWork.Comprobantes.GetLeyendasByIdAsync(comprobante.ComprobanteId)).ToList();
            var guias = (await _unitOfWork.Comprobantes.GetGuiasByIdAsync(comprobante.ComprobanteId)).ToList();
            var detracciones = (await _unitOfWork.Comprobantes.GetDetraccionesByIdAsync(comprobante.ComprobanteId)).ToList();
            lista.Add(MapToDto(comprobante, detalles, pagos, cuotas, leyendas, guias, detracciones));
        }
        return lista;
    }

    public async Task<int> GetCantidadByClienteNumDocAsync(string clienteNumDoc)
    {
        return await _unitOfWork.Comprobantes.GetCantidadByClienteNumDocAsync(clienteNumDoc);
    }

    public async Task<IEnumerable<ListarComprobanteDTO>> GetListadoByRucAndFechasAsync(string ruc, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null)
    {
        var comprobantes = await _unitOfWork.Comprobantes.GetByRucAndFechasAsync(ruc, fechaDesde, fechaHasta, limit);
        return comprobantes.Select(MapToListarDto);
    }

    public async Task<IEnumerable<ListarComprobanteDTO>> GetListadoByDocClienteAndFechasAsync(string rucEmpresa, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var comprobantes = await _unitOfWork.Comprobantes.GetByDocClienteAndFechasAsync(rucEmpresa, clienteNumDoc, fechaDesde, fechaHasta);
        return comprobantes.Select(MapToListarDto);
    }

    public async Task<IEnumerable<ListarComprobanteDTO>> GetListadoByDocUsuarioAndFechasAsync(string rucEmpresa, int usuarioCreacion, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var comprobantes = await _unitOfWork.Comprobantes.GetByDocUsuarioAndFechasAsync(rucEmpresa, usuarioCreacion, fechaDesde, fechaHasta);
        return comprobantes.Select(MapToListarDto);
    }

    public async Task<IEnumerable<ListarComprobanteDTO>> GetListadoBySucursalAndFechasAsync(int sucursalId, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null)
    {
        var sucursal = await _unitOfWork.Sucursal.GetByIdSucursalAsync(sucursalId);
        var comprobantes = await _unitOfWork.Comprobantes.GetBySucursalAndFechasAsync(
            sucursal.EmpresaRuc ?? throw new InvalidOperationException("La sucursal no tiene RUC"),
            sucursal.CodEstablecimiento ?? throw new InvalidOperationException("La sucursal no tiene código de establecimiento"),
            fechaDesde,
            fechaHasta,
            limit);
        return comprobantes.Select(MapToListarDto);
    }

    public async Task<IEnumerable<ListarComprobanteDTO>> GetListadoByClienteAndSucursalAsync(int sucursalId, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var sucursal = await _unitOfWork.Sucursal.GetByIdSucursalAsync(sucursalId);

        var comprobantes = await _unitOfWork.Comprobantes.GetByClienteAndSucursalAsync(
            sucursal.EmpresaRuc ?? throw new InvalidOperationException("La sucursal no tiene RUC"),
            sucursal.CodEstablecimiento ?? throw new InvalidOperationException("La sucursal no tiene código de establecimiento"),
            clienteNumDoc,
            fechaDesde,
            fechaHasta);

        return comprobantes.Select(MapToListarDto);
    }
    public async Task ActualizarCorreoWhatsappAsync(int comprobanteId, ActualizarCorreoWhatsappDTO dto)
    {
        var existe = await _unitOfWork.Comprobantes.GetByIdAsync(comprobanteId)
            ?? throw new KeyNotFoundException($"Comprobante {comprobanteId} no encontrado");

        await _unitOfWork.Comprobantes.UpdateCorreoWhatsappAsync(
            comprobanteId,
            dto.Correo,
            dto.EnviadoPorCorreo,
            dto.WhatsApp,
            dto.EnviadoPorWhatsApp
        );
    }
    public async Task<ComprobanteDetallesDTO?> GetDetallesByComprobanteIdAsync(int comprobanteId)
    {
        var existe = await _unitOfWork.Comprobantes.GetByIdAsync(comprobanteId);
        if (existe == null) return null;

        var datos = await _unitOfWork.Comprobantes.GetDatosCompletosByComprobanteIdAsync(comprobanteId);
        
        var detalles     = datos.Detalles.ToList();
        var pagos        = datos.Pagos.ToList();
        var cuotas       = datos.Cuotas.ToList();
        var leyendas     = datos.Leyendas.ToList();
        var guias        = datos.Guias.ToList();
        var detracciones = datos.Detracciones.ToList();

        return new ComprobanteDetallesDTO
        {
            ComprobanteId = comprobanteId,
            Details = detalles.Select(d => new DetalleFacturaDTO
            {
                DetalleId = d.DetalleId,
                ComprobanteId = d.ComprobanteId,
                Item = d.Item,
                ProductoId = d.ProductoId,
                Codigo = d.Codigo,
                Descripcion = d.Descripcion,
                Cantidad = d.Cantidad,
                UnidadMedida = d.UnidadMedida,
                PrecioUnitario = d.PrecioUnitario,
                TipoAfectacionIGV = d.TipoAfectacionIGV,
                PorcentajeIGV = d.PorcentajeIGV ?? 0,
                MontoIGV = d.MontoIGV ?? 0,
                BaseIgv = d.BaseIgv ?? 0,
                CodigoTipoDescuento = d.CodigoTipoDescuento ?? "",
                DescuentoUnitario = d.DescuentoUnitario ?? 0,
                DescuentoTotal = d.DescuentoTotal ?? 0,
                ValorVenta = d.ValorVenta ?? 0,
                PrecioVenta = d.PrecioVenta ?? 0,
                TotalVentaItem = d.TotalVentaItem ?? 0,
                Icbper = d.Icbper ?? 0,
                FactorIcbper = d.FactorIcbper ?? 0
            }).ToList(),

            Pagos = pagos.Select(p => new DetallePagosDTO
            {
                ComprobanteId = p.ComprobanteId,
                MedioPago = p.MedioPago,
                Monto = p.Monto,
                FechaPago = p.FechaPago,
                NumeroOperacion = p.NumeroOperacion,
                EntidadFinanciera = p.EntidadFinanciera,
                Observaciones = p.Observaciones
            }).ToList(),

            Cuotas = cuotas.Select(c => new DetalleCuotasDTO
            {
                ComprobanteId = c.ComprobanteId,
                NumeroCuota = c.NumeroCuota,
                Monto = c.Monto,
                FechaVencimiento = c.FechaVencimiento,
                MontoPagado = c.MontoPagado,
                FechaPago = c.FechaPago,
                Estado = c.Estado
            }).ToList(),

            Legends = leyendas.Select(l => new NoteLegendDto
            {
                Code = l.Code,
                Value = l.Value
            }).ToList(),

            Guias = guias.Select(g => new GuiaComprobanteDTO
            {
                ComprobanteId = g.ComprobanteId,
                GuiaTipoDoc = g.GuiaTipoDoc,
                GuiaNumeroCompleto = g.GuiaNumeroCompleto
            }).ToList(),

            Detracciones = detracciones.Select(d => new DetraccionDTO
            {
                ComprobanteID = d.ComprobanteID,
                CodigoBienDetraccion = d.CodigoBienDetraccion,
                CodigoMedioPago = d.CodigoMedioPago,
                CuentaBancoDetraccion = d.CuentaBancoDetraccion,
                PorcentajeDetraccion = d.PorcentajeDetraccion,
                MontoDetraccion = d.MontoDetraccion,
                Observacion = d.Observacion
            }).ToList()
        };
    }

    // ── 1. GENERAR Y GUARDAR ─────────────────────────────────────────────────
    public async Task<ComprobanteResponse> GenerarComprobanteAsync(GenerarComprobanteDTO dto)
    {
        // ── 1. Validaciones puras ─────────────────────────────────────────────
        if (dto.TipoComprobante != "01" && dto.TipoComprobante != "03")
            throw new InvalidOperationException("TipoDoc debe ser '01' o '03'");

        if (dto.Details == null || dto.Details.Count == 0)
            throw new InvalidOperationException("El comprobante debe tener al menos un detalle");

        //if (dto.Cuotas?.Count == 0 && dto.Pagos?.Count == 0)
        //throw new InvalidOperationException("El comprobante debe tener al menos un pago o una cuota");

        // ── 2. Buscar empresa por RUC ─────────────────────────────────────────
        var empresa = await _unitOfWork.Empresas.GetEmpresaByRucAsync(dto.Company.NumeroDocumento ?? "")
            ?? throw new KeyNotFoundException($"Empresa con RUC {dto.Company.NumeroDocumento} no encontrada");

        // ── 4. BeginTransaction ───────────────────────────────────────────────
        _unitOfWork.BeginTransaction();
        try
        {
            // Validación de duplicidad (SIN modificar tu mapeo)
            if (int.TryParse(dto.Correlativo, out var correlativoInt))
            {
                if (await _unitOfWork.Notes.ExisteNoteAsync(
                        empresa.Id,
                        dto.TipoComprobante,
                        dto.Serie,
                        correlativoInt))
                {
                    throw new InvalidOperationException(
                        $"Ya existe el comprobante {dto.Serie}-{dto.Correlativo}");
                }
            }

            var comprobante = new Domain.Entities.Comprobante
            {
                TipoOperacion = dto.TipoOperacion,
                TipoComprobante = dto.TipoComprobante,
                Serie = dto.Serie,
                Correlativo = int.TryParse(dto.Correlativo, out var corr) ? corr : null,
                NumeroCompleto = $"{dto.Serie}-{dto.Correlativo}",
                FechaEmision = dto.FechaEmision,
                HoraEmision = dto.FechaEmision,
                FechaVencimiento = dto.FechaVencimiento,
                TipoMoneda = dto.TipoMoneda,
                TipoCambio = dto.TipoCambio,
                TipoPago = dto.TipoPago,
                EmpresaId = dto.Company.EmpresaId,
                EmpresaRuc = dto.Company.NumeroDocumento,
                EmpresaRazonSocial = dto.Company.RazonSocial,
                EmpresaNombreComercial = dto.Company.NombreComercial,
                EmpresaEstablecimientoAnexo = dto.Company.EstablecimientoAnexo,
                EmpresaDireccion = dto.Company.DireccionLineal,
                EmpresaProvincia = dto.Company.Provincia,
                EmpresaDepartamento = dto.Company.Departamento,
                EmpresaDistrito = dto.Company.Distrito,
                EmpresaUbigeo = dto.Company.Ubigeo,
                ClienteId = dto.Cliente?.ClienteId,
                ClienteTipoDoc = dto.Cliente?.TipoDocumento,
                ClienteNumDoc = dto.Cliente?.NumeroDocumento,
                ClienteRazonSocial = dto.Cliente?.RazonSocial,
                ClienteDireccion = dto.Cliente?.DireccionLineal,
                ClienteProvincia = dto.Cliente?.Provincia,
                ClienteDepartamento = dto.Cliente?.Departamento,
                ClienteDistrito = dto.Cliente?.Distrito,
                ClienteUbigeo = dto.Cliente?.Ubigeo,
                ClienteCorreo = dto.Cliente?.Correo,
                EnviadoPorCorreo = dto.Cliente?.EnviadoPorCorreo,
                ClienteWhatsApp = dto.Cliente?.WhatsApp,
                EnviadoPorWhatsApp = dto.Cliente?.EnviadoPorWhatsApp,
                CodigoTipoDescGlobal = dto.CodigoTipoDescGlobal,
                DescuentoGlobal = dto.DescuentoGlobal,
                TotalOperacionesGravadas = dto.TotalOperacionesGravadas,
                TotalOperacionesExoneradas = dto.TotalOperacionesExoneradas,
                TotalOperacionesInafectas = dto.TotalOperacionesInafectas,
                TotalOperacionesGratuitas = dto.TotalOperacionesGratuitas,
                TotalIgvGratuitas = dto.TotalIgvGratuitas,
                TotalIGV = dto.TotalIGV,
                TotalImpuestos = dto.TotalImpuestos,
                TotalDescuentos = dto.TotalDescuentos,
                TotalOtrosCargos = dto.TotalOtrosCargos,
                ValorVenta = dto.ValorVenta,
                SubTotal = dto.SubTotal,
                TotalIcbper = dto.TotalIcbper,
                ImporteTotal = dto.ImporteTotal,
                MontoCredito = dto.MontoCredito,
                EstadoSunat = "PENDIENTE",
                XmlGenerado = null,
                EnviadoEnResumen = dto.EnviadoEnResumen,
                UsuarioCreacion = dto.UsuarioCreacion,
                FechaCreacion = DateTime.Now,

                Detalles = dto.Details.Select(d => new Domain.Entities.ComprobanteDetalle
                {
                    Item = d.Item,
                    ProductoId = d.ProductoId,
                    Codigo = d.Codigo,
                    Descripcion = d.Descripcion,
                    Cantidad = d.Cantidad,
                    UnidadMedida = d.UnidadMedida,
                    PrecioUnitario = d.PrecioUnitario,
                    TipoAfectacionIGV = d.TipoAfectacionIGV?.ToString(),
                    PorcentajeIGV = d.PorcentajeIGV,
                    MontoIGV = d.MontoIGV,
                    BaseIgv = d.BaseIgv,
                    CodigoTipoDescuento = d.CodigoTipoDescuento,
                    DescuentoUnitario = d.DescuentoUnitario,
                    DescuentoTotal = d.DescuentoTotal,
                    ValorVenta = d.ValorVenta,
                    PrecioVenta = d.PrecioVenta,
                    TotalVentaItem = d.TotalVentaItem,
                    Icbper = d.Icbper,
                    FactorIcbper = d.FactorIcbper
                }).ToList(),

                Pagos = dto.Pagos?.Select(p => new Domain.Entities.Pago
                {
                    MedioPago = p.MedioPago,
                    Monto = p.Monto,
                    FechaPago = p.FechaPago,
                    NumeroOperacion = p.NumeroOperacion,
                    EntidadFinanciera = p.EntidadFinanciera,
                    Observaciones = p.Observaciones
                }).ToList() ?? [],

                Cuotas = dto.Cuotas?.Select(c => new Domain.Entities.Cuota
                {
                    NumeroCuota = c.NumeroCuota,
                    Monto = c.Monto,
                    FechaVencimiento = c.FechaVencimiento,
                    MontoPagado = c.MontoPagado,
                    FechaPago = c.FechaPago,
                    Estado = c.Estado
                }).ToList() ?? [],

                Leyendas = dto.Legends?.Select(l => new Domain.Entities.NoteLegend
                {
                    Code = l.Code,
                    Value = l.Value
                }).ToList() ?? [],

                Guias = dto.Guias?.Select(g => new Domain.Entities.GuiaComprobante
                {
                    GuiaTipoDoc = g.GuiaTipoDoc,
                    GuiaNumeroCompleto = g.GuiaNumeroCompleto,
                }).ToList() ?? [],

                Detracciones = dto.Detracciones?.Select(d => new Domain.Entities.Detraccion
                {
                    CodigoBienDetraccion = d.CodigoBienDetraccion,
                    CodigoMedioPago = d.CodigoMedioPago,
                    CuentaBancoDetraccion = d.CuentaBancoDetraccion,
                    PorcentajeDetraccion = d.PorcentajeDetraccion,
                    MontoDetraccion = d.MontoDetraccion,
                    Observacion = d.Observacion
                }).ToList() ?? [],
            };

            int newComprobanteId;

            newComprobanteId = await _unitOfWork.Comprobantes
                .GenerarComprobanteAsync(comprobante);

            comprobante.ComprobanteId = newComprobanteId;

            _unitOfWork.Commit();

            var sucursalIdNotify = await _unitOfWork.Comprobantes.GetSucursalIdByRucAndAnexoAsync(
                comprobante.EmpresaRuc!,
                comprobante.EmpresaEstablecimientoAnexo!
            );

            //_ = Task.Run(() => _wsNotifier.NotifyAsync(sucursalIdNotify, comprobante.EmpresaRuc));

            return new ComprobanteResponse
            {
                Exitoso = true,
                Mensaje = "Comprobante guardado correctamente",
                ComprobanteId = newComprobanteId,
                EstadoSunat = "PENDIENTE"
            };
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // ── 2. FIRMAR + ENVIAR A SUNAT ───────────────────────────────────────────
    public async Task<ComprobanteResponse> SendToSunatAsync(int comprobanteId)
    {
        // 1. Cargar comprobante desde BD
        var comprobante = await _unitOfWork.Comprobantes.GetByIdAsync(comprobanteId)
            ?? throw new KeyNotFoundException($"Comprobante {comprobanteId} no encontrado");

        if (comprobante.EstadoSunat == "ACEPTADO")
            throw new InvalidOperationException("El comprobante ya fue aceptado por SUNAT");

        // 2. Cargar empresa
        var empresa = await _unitOfWork.Empresas.GetEmpresaByIdAsync(comprobante.EmpresaId)
            ?? throw new KeyNotFoundException($"Empresa {comprobante.EmpresaId} no encontrada");

        if (string.IsNullOrEmpty(empresa.CertificadoPem))
            throw new InvalidOperationException("La empresa no tiene certificado digital configurado");

        if (string.IsNullOrEmpty(empresa.SolUsuario) || string.IsNullOrEmpty(empresa.SolClave))
            throw new InvalidOperationException("La empresa no tiene credenciales SOL configuradas");

        // 3. Recuperar el XML directamente de la BD (Ultrarrápido) o regenerar como Fallback
        string xmlSinFirmar;

        if (!string.IsNullOrEmpty(comprobante.XmlGenerado))
        {
            // El XML ya existe, nos saltamos decenas de consultas y reconstrucción
            xmlSinFirmar = comprobante.XmlGenerado;
        }
        else
        {
            // FALLBACK: Si es un comprobante muy antiguo sin XML guardado, usamos el método rápido (1 sola consulta)
            var datos = await _unitOfWork.Comprobantes.GetDatosCompletosByComprobanteIdAsync(comprobanteId);

            var dto = new GenerarComprobanteDTO
            {
                UblVersion = "2.1",
                TipoOperacion = comprobante.TipoOperacion!,
                TipoComprobante = comprobante.TipoComprobante!,
                Serie = comprobante.Serie!,
                Correlativo = comprobante.Correlativo?.ToString() ?? "0",
                FechaEmision = comprobante.FechaEmision,
                HoraEmision = comprobante.HoraEmision,
                FechaVencimiento = comprobante.FechaVencimiento,
                TipoMoneda = comprobante.TipoMoneda!,
                TipoPago = comprobante.TipoPago,
                TipoCambio = comprobante.TipoCambio,
                CodigoTipoDescGlobal = comprobante.CodigoTipoDescGlobal ?? "",
                DescuentoGlobal = comprobante.DescuentoGlobal ?? 0,
                TotalOperacionesGravadas = comprobante.TotalOperacionesGravadas ?? 0,
                TotalOperacionesExoneradas = comprobante.TotalOperacionesExoneradas ?? 0,
                TotalOperacionesInafectas = comprobante.TotalOperacionesInafectas ?? 0,
                TotalOperacionesGratuitas = comprobante.TotalOperacionesGratuitas ?? 0,
                TotalIgvGratuitas = comprobante.TotalIgvGratuitas ?? 0,
                TotalIGV = comprobante.TotalIGV ?? 0,
                TotalImpuestos = comprobante.TotalImpuestos ?? 0,
                TotalDescuentos = comprobante.TotalDescuentos ?? 0,
                TotalOtrosCargos = comprobante.TotalOtrosCargos ?? 0,
                ValorVenta = comprobante.ValorVenta ?? 0,
                SubTotal = comprobante.SubTotal ?? 0,
                TotalIcbper = comprobante.TotalIcbper ?? 0,
                ImporteTotal = comprobante.ImporteTotal ?? 0,
                MontoCredito = comprobante.MontoCredito ?? 0,

                Company = new EmpresaDTO
                {
                    EmpresaId = comprobante.EmpresaId,
                    NumeroDocumento = comprobante.EmpresaRuc!,
                    RazonSocial = comprobante.EmpresaRazonSocial!,
                    NombreComercial = comprobante.EmpresaNombreComercial,
                    EstablecimientoAnexo = comprobante.EmpresaEstablecimientoAnexo,
                    DireccionLineal = comprobante.EmpresaDireccion,
                    Provincia = comprobante.EmpresaProvincia,
                    Departamento = comprobante.EmpresaDepartamento,
                    Distrito = comprobante.EmpresaDistrito,
                    Ubigeo = comprobante.EmpresaUbigeo
                },

                Cliente = new ClienteDTO
                {
                    ClienteId = comprobante.ClienteId ?? 0,
                    TipoDocumento = comprobante.ClienteTipoDoc!,
                    NumeroDocumento = comprobante.ClienteNumDoc!,
                    RazonSocial = comprobante.ClienteRazonSocial!,
                    DireccionLineal = comprobante.ClienteDireccion,
                    Provincia = comprobante.ClienteProvincia,
                    Departamento = comprobante.ClienteDepartamento,
                    Distrito = comprobante.ClienteDistrito,
                    Ubigeo = comprobante.ClienteUbigeo
                },

                Details = datos.Detalles.Select(d => new DetalleFacturaDTO
                {
                    Item = d.Item,
                    ProductoId = d.ProductoId ?? 0,
                    Codigo = d.Codigo,
                    Descripcion = d.Descripcion,
                    Cantidad = d.Cantidad,
                    UnidadMedida = d.UnidadMedida,
                    PrecioUnitario = d.PrecioUnitario,
                    TipoAfectacionIGV = d.TipoAfectacionIGV,
                    PorcentajeIGV = d.PorcentajeIGV ?? 0,
                    MontoIGV = d.MontoIGV ?? 0,
                    BaseIgv = d.BaseIgv ?? 0,
                    CodigoTipoDescuento = d.CodigoTipoDescuento ?? "",
                    DescuentoUnitario = d.DescuentoUnitario ?? 0,
                    DescuentoTotal = d.DescuentoTotal ?? 0,
                    ValorVenta = d.ValorVenta ?? 0,
                    PrecioVenta = d.PrecioVenta ?? 0,
                    TotalVentaItem = d.TotalVentaItem ?? 0,
                    Icbper = d.Icbper ?? 0,
                    FactorIcbper = d.FactorIcbper ?? 0
                }).ToList(),

                Cuotas = datos.Cuotas.Select(c => new DetalleCuotasDTO
                {
                    NumeroCuota = c.NumeroCuota,
                    Monto = c.Monto,
                    FechaVencimiento = c.FechaVencimiento,
                    MontoPagado = c.MontoPagado,
                    FechaPago = c.FechaPago,
                    Estado = c.Estado
                }).ToList(),

                Legends = datos.Leyendas.Select(l => new NoteLegendDto
                {
                    Code = l.Code,
                    Value = l.Value
                }).ToList(),

                Guias = datos.Guias.Select(g => new GuiaComprobanteDTO
                {
                    GuiaTipoDoc = g.GuiaTipoDoc,
                    GuiaNumeroCompleto = g.GuiaNumeroCompleto,
                }).ToList(),

                Detracciones = datos.Detracciones.Select(d => new DetraccionDTO
                {
                    ComprobanteID = d.ComprobanteID,
                    CodigoBienDetraccion = d.CodigoBienDetraccion,
                    CodigoMedioPago = d.CodigoMedioPago,
                    CuentaBancoDetraccion = d.CuentaBancoDetraccion,
                    PorcentajeDetraccion = d.PorcentajeDetraccion,
                    MontoDetraccion = d.MontoDetraccion,
                    Observacion = d.Observacion
                }).ToList(),
            };

            var xmlResultado = _xmlService.GenerarXml(dto);
            if (!xmlResultado.Exitoso)
                throw new InvalidOperationException($"Error regenerando XML: {xmlResultado.Error}");
            
            xmlSinFirmar = xmlResultado.XmlString!;
        }

        // 6. Firmar XML
        var xmlFirmadoBytes = _xmlSigner.SignXmlToBytes(
            xmlSinFirmar,
            empresa.CertificadoPem!,
            empresa.CertificadoPassword ?? ""
        );
        var xmlFirmadoString = Encoding.UTF8.GetString(xmlFirmadoBytes);
        var nombreArchivo = $"{empresa.Ruc}-{comprobante.TipoComprobante}-{comprobante.Serie}-{comprobante.Correlativo:D8}";

        // 7. Guardar ZIP firmado localmente
        await GuardarArchivosAsync(
            ruc: empresa.Ruc,
            razonSocial: comprobante.EmpresaRazonSocial!,
            tipoComprobante: comprobante.TipoComprobante!,
            nombreArchivo: nombreArchivo,
            xmlFirmadoBytes: xmlFirmadoBytes,
            cdrBase64: null
        );

        // 8. Enviar a SUNAT
        SunatResponse sunatResponse;
        try
        {
            sunatResponse = await _sunatSender.SendNoteAsync(
                xmlFirmadoBytes,
                nombreArchivo,
                empresa.Ruc,
                empresa.SolUsuario!,
                empresa.SolClave!,
                empresa.Environment
            );
        }
        catch (HttpRequestException ex)
        {
            await _unitOfWork.Comprobantes.UpdateEstadoSunatAsync(
                comprobanteId,
                "PENDIENTE",
                null,
                $"Error de conexión con SUNAT: {ex.Message}",
                xmlFirmadoString,
                null
            );

            var sucursalIdNotify = await _unitOfWork.Comprobantes.GetSucursalIdByRucAndAnexoAsync(
                comprobante.EmpresaRuc!,
                comprobante.EmpresaEstablecimientoAnexo!
            );

            //_ = Task.Run(() => _wsNotifier.NotifyAsync(sucursalIdNotify, comprobante.EmpresaRuc));

            return new ComprobanteResponse
            {
                Exitoso = false,
                Mensaje = "No se pudo conectar con SUNAT. El comprobante quedó como PENDIENTE para reenvío.",
                ComprobanteId = comprobanteId,
                EstadoSunat = "PENDIENTE",
                CodigoRespuesta = null,
                MensajeRespuesta = ex.Message,
                CdrBase64 = null
            };
        }

        // 9. Guardar CDR
        if (!string.IsNullOrEmpty(sunatResponse.CdrBase64))
        {
            await GuardarArchivosAsync(
                ruc: empresa.Ruc,
                razonSocial: comprobante.EmpresaRazonSocial!,
                tipoComprobante: comprobante.TipoComprobante!,
                nombreArchivo: nombreArchivo,
                xmlFirmadoBytes: null,
                cdrBase64: sunatResponse.CdrBase64
            );
        }

        // 10. Actualizar estado BD
        var nuevoEstado = sunatResponse.Success
            ? (sunatResponse.TieneObservaciones ? "ACEPTADO_CON_OBSERVACIONES" : "ACEPTADO")
            : "RECHAZADO";

        await _unitOfWork.Comprobantes.UpdateEstadoSunatAsync(
            comprobanteId,
            nuevoEstado,
            sunatResponse.CodigoRespuesta,
            sunatResponse.Descripcion,
            xmlFirmadoString,
            sunatResponse.CdrBase64
        );

        // Obtener sucursalId desde ruc + anexo
        var sucursalId = await _unitOfWork.Comprobantes.GetSucursalIdByRucAndAnexoAsync(
            comprobante.EmpresaRuc!,
            comprobante.EmpresaEstablecimientoAnexo!
        );

        //_ = Task.Run(() => _wsNotifier.NotifyAsync(sucursalId, comprobante.EmpresaRuc));

        return new ComprobanteResponse
        {
            Exitoso = sunatResponse.Success,
            Mensaje = sunatResponse.Descripcion,
            ComprobanteId = comprobanteId,
            EstadoSunat = nuevoEstado,
            CodigoRespuesta = sunatResponse.CodigoRespuesta,
            MensajeRespuesta = sunatResponse.Descripcion,
            CdrBase64 = sunatResponse.CdrBase64
        };
    }

    // ── HELPERS ──────────────────────────────────────────────────────────────
    private async Task GuardarArchivosAsync(string ruc, string razonSocial, string tipoComprobante,
        string nombreArchivo, byte[]? xmlFirmadoBytes, string? cdrBase64)
    {
        var empresaCarpeta = LimpiarNombreCarpeta(razonSocial);
        var tipoCarpeta = tipoComprobante switch
        {
            "01" => "Facturas",
            "03" => "Boletas",
            "07" => "NotasCredito",
            "08" => "NotasDebito",
            _ => tipoComprobante
        };

        var carpeta = Path.Combine(_rutaXml, empresaCarpeta, tipoCarpeta);
        Directory.CreateDirectory(carpeta);

        // Guardar ZIP firmado
        if (xmlFirmadoBytes != null)
        {
            using var memStream = new MemoryStream();
            using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zip.CreateEntry($"{nombreArchivo}.xml");
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(xmlFirmadoBytes);
            }
            await File.WriteAllBytesAsync(
                Path.Combine(carpeta, $"{nombreArchivo}.zip"),
                memStream.ToArray());
        }

        // Guardar CDR
        if (!string.IsNullOrEmpty(cdrBase64))
        {
            var cdrBytes = Convert.FromBase64String(cdrBase64);
            await File.WriteAllBytesAsync(
                Path.Combine(carpeta, $"R-{nombreArchivo}.zip"),
                cdrBytes);
        }
    }

    private static string LimpiarNombreCarpeta(string nombre) =>
        string.Concat(nombre
            .Replace("/", "").Replace("\\", "").Replace(":", "")
            .Replace("*", "").Replace("?", "").Replace("\"", "")
            .Replace("<", "").Replace(">", "").Replace("|", "")
            .Trim());


    public async Task<ObtenerComprobanteDTO?> GetComprobanteByIdAsync(int comprobanteId)
    {
        var comprobante = await _unitOfWork.Comprobantes.GetByIdAsync(comprobanteId);
        if (comprobante == null)
            return null;

        var datos = await _unitOfWork.Comprobantes.GetDatosCompletosByComprobanteIdAsync(comprobanteId);

        return MapToDto(comprobante, 
            datos.Detalles.ToList(), 
            datos.Pagos.ToList(), 
            datos.Cuotas.ToList(), 
            datos.Leyendas.ToList(), 
            datos.Guias.ToList(), 
            datos.Detracciones.ToList());
    }

    public async Task<ObtenerComprobanteDTO?> GetByRucSerieNumeroAsync(string ruc, string serie, int numero)
    {
        var comprobante = await _unitOfWork.Comprobantes.GetByRucSerieNumeroAsync(ruc, serie, numero);
        if (comprobante == null)
            return null;

        var comprobanteId = comprobante.ComprobanteId;
        var datos = await _unitOfWork.Comprobantes.GetDatosCompletosByComprobanteIdAsync(comprobanteId);

        return MapToDto(comprobante, 
            datos.Detalles.ToList(), 
            datos.Pagos.ToList(), 
            datos.Cuotas.ToList(), 
            datos.Leyendas.ToList(), 
            datos.Guias.ToList(), 
            datos.Detracciones.ToList());
    }

    public async Task<ObtenerComprobanteDTO?> GetByComprobanteUnicoAsync(string ruc, string serie, int numero)
    {
        var comprobante = await _unitOfWork.Comprobantes.GetByComprobanteUnicoAsync(ruc, serie, numero);
        if (comprobante == null)
            return null;

        var comprobanteId = comprobante.ComprobanteId;
        var detalles = (await _unitOfWork.Comprobantes.GetDetallesByIdAsync(comprobanteId)).ToList();
        var pagos = (await _unitOfWork.Comprobantes.GetPagosByIdAsync(comprobanteId)).ToList();
        var cuotas = (await _unitOfWork.Comprobantes.GetCuotasByIdAsync(comprobanteId)).ToList();
        var leyendas = (await _unitOfWork.Comprobantes.GetLeyendasByIdAsync(comprobanteId)).ToList();
        var guias = (await _unitOfWork.Comprobantes.GetGuiasByIdAsync(comprobanteId)).ToList();
        var detracciones = (await _unitOfWork.Comprobantes.GetDetraccionesByIdAsync(comprobanteId)).ToList();

        return MapToDto(comprobante, detalles, pagos, cuotas, leyendas, guias, detracciones);
    }

    public async Task<IEnumerable<ObtenerComprobanteDTO>> GetComprobanteByEstadoAsync(string estado)
    {
        var comprobantes = await _unitOfWork.Comprobantes.GetByEstadoAsync(estado);

        var lista = new List<ObtenerComprobanteDTO>();

        foreach (var comprobante in comprobantes)
        {
            var detalles = (await _unitOfWork.Comprobantes
                .GetDetallesByIdAsync(comprobante.ComprobanteId)).ToList();

            var pagos = (await _unitOfWork.Comprobantes
                .GetPagosByIdAsync(comprobante.ComprobanteId)).ToList();

            var cuotas = (await _unitOfWork.Comprobantes
                .GetCuotasByIdAsync(comprobante.ComprobanteId)).ToList();

            var leyendas = (await _unitOfWork.Comprobantes
                .GetLeyendasByIdAsync(comprobante.ComprobanteId)).ToList();

            var guias = (await _unitOfWork.Comprobantes
                .GetGuiasByIdAsync(comprobante.ComprobanteId)).ToList();

            var detracciones = (await _unitOfWork.Comprobantes
                .GetDetraccionesByIdAsync(comprobante.ComprobanteId)).ToList();


            lista.Add(MapToDto(comprobante, detalles, pagos, cuotas, leyendas, guias, detracciones));
        }

        return lista;
    }

    private static ObtenerComprobanteDTO MapToDto(
    Domain.Entities.Comprobante comprobante,
    List<Domain.Entities.ComprobanteDetalle> detalles,
    List<Domain.Entities.Pago> pagos,
    List<Domain.Entities.Cuota> cuotas,
    List<Domain.Entities.NoteLegend> leyendas,
    List<Domain.Entities.GuiaComprobante> guias,
    List<Domain.Entities.Detraccion> detracciones)
    {
        return new ObtenerComprobanteDTO
        {
            ComprobanteId = comprobante.ComprobanteId,
            UblVersion = "2.1",
            TipoOperacion = comprobante.TipoOperacion ?? "0101",
            TipoComprobante = comprobante.TipoComprobante,
            Serie = comprobante.Serie ?? "",
            Correlativo = comprobante.Correlativo?.ToString() ?? "",
            NumeroCompleto = comprobante.NumeroCompleto ?? "",
            TipoCambio = comprobante.TipoCambio ?? 0,
            FechaEmision = comprobante.FechaEmision,
            HoraEmision = comprobante.HoraEmision,
            FechaVencimiento = comprobante.FechaVencimiento,
            TipoMoneda = comprobante.TipoMoneda ?? "PEN",
            TipoPago = comprobante.TipoPago,

            Cliente = new ClienteDTO
            {
                ClienteId = comprobante.ClienteId,
                TipoDocumento = comprobante.ClienteTipoDoc,
                NumeroDocumento = comprobante.ClienteNumDoc,
                RazonSocial = comprobante.ClienteRazonSocial,
                DireccionLineal = comprobante.ClienteDireccion,
                Provincia = comprobante.ClienteProvincia,
                Departamento = comprobante.ClienteDepartamento,
                Distrito = comprobante.ClienteDistrito,
                Ubigeo = comprobante.ClienteUbigeo,
                Correo = comprobante.ClienteCorreo,
                EnviadoPorCorreo = comprobante.EnviadoPorCorreo,
                WhatsApp = comprobante.ClienteWhatsApp,
                EnviadoPorWhatsApp = comprobante.EnviadoPorWhatsApp
            },

            Company = new EmpresaDTO
            {
                EmpresaId = comprobante.EmpresaId,
                NumeroDocumento = comprobante.EmpresaRuc,
                RazonSocial = comprobante.EmpresaRazonSocial,
                NombreComercial = comprobante.EmpresaNombreComercial,
                EstablecimientoAnexo = comprobante.EmpresaEstablecimientoAnexo,
                DireccionLineal = comprobante.EmpresaDireccion,
                Provincia = comprobante.EmpresaProvincia,
                Departamento = comprobante.EmpresaDepartamento,
                Distrito = comprobante.EmpresaDistrito,
                Ubigeo = comprobante.EmpresaUbigeo
            },

            // Totales
            CodigoTipoDescGlobal = comprobante.CodigoTipoDescGlobal ?? "",
            DescuentoGlobal = comprobante.DescuentoGlobal ?? 0,
            TotalOperacionesGravadas = comprobante.TotalOperacionesGravadas ?? 0,
            TotalOperacionesExoneradas = comprobante.TotalOperacionesExoneradas ?? 0,
            TotalOperacionesInafectas = comprobante.TotalOperacionesInafectas ?? 0,
            TotalOperacionesGratuitas = comprobante.TotalOperacionesGratuitas ?? 0,
            TotalIgvGratuitas = comprobante.TotalIgvGratuitas ?? 0,
            TotalIGV = comprobante.TotalIGV ?? 0,
            TotalImpuestos = comprobante.TotalImpuestos ?? 0,
            TotalDescuentos = comprobante.TotalDescuentos ?? 0,
            TotalOtrosCargos = comprobante.TotalOtrosCargos ?? 0,
            ValorVenta = comprobante.ValorVenta ?? 0,
            SubTotal = comprobante.SubTotal ?? 0,
            TotalIcbper = comprobante.TotalIcbper ?? 0,
            ImporteTotal = comprobante.ImporteTotal ?? 0,

            //Obtener para pdf Notas
            TipDocAfectado = comprobante.TipDocAfectado,
            NumDocAfectado = comprobante.NumDocAfectado,
            TipoNotaCreditoDebito = comprobante.TipoNotaCreditoDebito,
            MotivoNota = comprobante.MotivoNota,

            Details = detalles.Select(d => new DetalleFacturaDTO
            {
                DetalleId = d.DetalleId,
                ComprobanteId = d.ComprobanteId,
                Item = d.Item,
                ProductoId = d.ProductoId,
                Codigo = d.Codigo,
                Descripcion = d.Descripcion,
                Cantidad = d.Cantidad,
                UnidadMedida = d.UnidadMedida,
                PrecioUnitario = d.PrecioUnitario,
                TipoAfectacionIGV = d.TipoAfectacionIGV,
                PorcentajeIGV = d.PorcentajeIGV ?? 0,
                MontoIGV = d.MontoIGV ?? 0,
                BaseIgv = d.BaseIgv ?? 0,
                CodigoTipoDescuento = d.CodigoTipoDescuento ?? "",
                DescuentoUnitario = d.DescuentoUnitario ?? 0,
                DescuentoTotal = d.DescuentoTotal ?? 0,
                ValorVenta = d.ValorVenta ?? 0,
                PrecioVenta = d.PrecioVenta ?? 0,
                TotalVentaItem = d.TotalVentaItem ?? 0,
                Icbper = d.Icbper ?? 0,
                FactorIcbper = d.FactorIcbper ?? 0
            }).ToList(),

            Pagos = pagos.Select(p => new DetallePagosDTO
            {
                ComprobanteId = p.ComprobanteId,
                MedioPago = p.MedioPago,
                Monto = p.Monto,
                FechaPago = p.FechaPago,
                NumeroOperacion = p.NumeroOperacion,
                EntidadFinanciera = p.EntidadFinanciera,
                Observaciones = p.Observaciones
            }).ToList(),

            Cuotas = cuotas.Select(c => new DetalleCuotasDTO
            {
                ComprobanteId = c.ComprobanteId,
                NumeroCuota = c.NumeroCuota,
                Monto = c.Monto,
                FechaVencimiento = c.FechaVencimiento,
                MontoPagado = c.MontoPagado,
                FechaPago = c.FechaPago,
                Estado = c.Estado
            }).ToList(),

            Legends = leyendas.Select(l => new NoteLegendDto
            {
                Code = l.Code,
                Value = l.Value
            }).ToList(),

            Guias = guias.Select(g => new GuiaComprobanteDTO
            {
                ComprobanteId = g.ComprobanteId,
                GuiaTipoDoc = g.GuiaTipoDoc,
                GuiaNumeroCompleto = g.GuiaNumeroCompleto,
            }).ToList(),

            Detracciones = detracciones.Select(d => new DetraccionDTO
            {
                ComprobanteID = d.ComprobanteID,
                CodigoBienDetraccion = d.CodigoBienDetraccion,
                CodigoMedioPago = d.CodigoMedioPago,
                CuentaBancoDetraccion = d.CuentaBancoDetraccion,
                PorcentajeDetraccion = d.PorcentajeDetraccion,
                MontoDetraccion = d.MontoDetraccion,
                Observacion = d.Observacion
            }).ToList(),

            PdfGenerado = comprobante.PdfGenerado,
            EnviadoEnResumen = comprobante.EnviadoEnResumen,
            EstadoSunat = comprobante.EstadoSunat,
            CodigoRespuestaSunat = comprobante.CodigoRespuestaSunat,
            MensajeRespuestaSunat = comprobante.MensajeRespuestaSunat,
            FechaEnvioSunat = comprobante.FechaEnvioSunat,
            UsuarioCreacion = comprobante.UsuarioCreacion,
            FechaCreacion = comprobante.FechaCreacion,
            UsuarioModificacion = comprobante.UsuarioModificacion,
            FechaModificacion = comprobante.FechaModificacion
        };
    }

    private static ListarComprobanteDTO MapToListarDto(Domain.Entities.Comprobante c) => new()
    {
        ComprobanteId = c.ComprobanteId,
        TipoOperacion = c.TipoOperacion ?? "0101",
        TipoComprobante = c.TipoComprobante,
        Serie = c.Serie ?? "",
        Correlativo = c.Correlativo?.ToString() ?? "",
        NumeroCompleto = c.NumeroCompleto ?? "",
        TipoCambio = c.TipoCambio ?? 0,
        FechaEmision = c.FechaEmision,
        HoraEmision = c.HoraEmision,
        FechaVencimiento = c.FechaVencimiento,
        TipoMoneda = c.TipoMoneda ?? "PEN",
        TipoPago = c.TipoPago,

        Cliente = new ClienteDTO
        {
            ClienteId = c.ClienteId,
            TipoDocumento = c.ClienteTipoDoc,
            NumeroDocumento = c.ClienteNumDoc,
            RazonSocial = c.ClienteRazonSocial,
            DireccionLineal = c.ClienteDireccion,
            Provincia = c.ClienteProvincia,
            Departamento = c.ClienteDepartamento,
            Distrito = c.ClienteDistrito,
            Ubigeo = c.ClienteUbigeo,
            Correo = c.ClienteCorreo,
            EnviadoPorCorreo = c.EnviadoPorCorreo,
            WhatsApp = c.ClienteWhatsApp,
            EnviadoPorWhatsApp = c.EnviadoPorWhatsApp
        },

        Company = new EmpresaDTO
        {
            EmpresaId = c.EmpresaId,
            NumeroDocumento = c.EmpresaRuc,
            RazonSocial = c.EmpresaRazonSocial,
            NombreComercial = c.EmpresaNombreComercial,
            EstablecimientoAnexo = c.EmpresaEstablecimientoAnexo,
            DireccionLineal = c.EmpresaDireccion,
            Provincia = c.EmpresaProvincia,
            Departamento = c.EmpresaDepartamento,
            Distrito = c.EmpresaDistrito,
            Ubigeo = c.EmpresaUbigeo
        },

        CodigoTipoDescGlobal = c.CodigoTipoDescGlobal ?? "",
        DescuentoGlobal = c.DescuentoGlobal ?? 0,
        TotalOperacionesGravadas = c.TotalOperacionesGravadas ?? 0,
        TotalOperacionesExoneradas = c.TotalOperacionesExoneradas ?? 0,
        TotalOperacionesInafectas = c.TotalOperacionesInafectas ?? 0,
        TotalOperacionesGratuitas = c.TotalOperacionesGratuitas ?? 0,
        TotalIgvGratuitas = c.TotalIgvGratuitas ?? 0,
        TotalIGV = c.TotalIGV ?? 0,
        TotalImpuestos = c.TotalImpuestos ?? 0,
        TotalDescuentos = c.TotalDescuentos ?? 0,
        TotalOtrosCargos = c.TotalOtrosCargos ?? 0,
        TotalIcbper = c.TotalIcbper ?? 0,
        ValorVenta = c.ValorVenta ?? 0,
        SubTotal = c.SubTotal ?? 0,
        ImporteTotal = c.ImporteTotal ?? 0,
        MontoCredito = c.MontoCredito ?? 0,

        ComprobanteAfectadoId = c.ComprobanteAfectadoId,
        TipDocAfectado = c.TipDocAfectado,
        NumDocAfectado = c.NumDocAfectado,
        TipoNotaCreditoDebito = c.TipoNotaCreditoDebito,
        MotivoNota = c.MotivoNota,
        Observaciones = c.Observaciones,

        EstadoSunat = c.EstadoSunat,
        CodigoHashCPE = c.CodigoHashCPE,
        CodigoRespuestaSunat = c.CodigoRespuestaSunat,
        MensajeRespuestaSunat = c.MensajeRespuestaSunat,
        PdfGenerado = c.PdfGenerado,
        EnviadoEnResumen = c.EnviadoEnResumen,
        FechaEnvioSunat = c.FechaEnvioSunat,

        UsuarioCreacion = c.UsuarioCreacion,
        FechaCreacion = c.FechaCreacion,
        UsuarioModificacion = c.UsuarioModificacion,
        FechaModificacion = c.FechaModificacion
    };

}