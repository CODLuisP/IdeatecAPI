using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Comprobante.DTOs;

namespace IdeatecAPI.Application.Features.Comprobante.Services;

public interface IComprobanteService
{
    Task<ComprobanteResponse> GenerarComprobanteAsync(GenerarComprobanteDTO dto);
}

public class ComprobanteResponse
{
    public bool Exitoso { get; set; }
    public string? Mensaje { get; set; }
    public int? ComprobanteId { get; set; }
    public string? XmlBase64 { get; set; }
    public string? XmlString { get; set; }
}

public class ComprobanteService : IComprobanteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IComprobanteXmlService _xmlService;

    public ComprobanteService(IUnitOfWork unitOfWork, IComprobanteXmlService xmlService)
    {
        _unitOfWork = unitOfWork;
        _xmlService = xmlService;
    }
    
    public async Task<ComprobanteResponse> GenerarComprobanteAsync(GenerarComprobanteDTO dto)
    {
        // 1. Generar XML
        var xmlResultado = _xmlService.GenerarXml(dto);
        if (!xmlResultado.Exitoso)
            return new ComprobanteResponse { Exitoso = false, Mensaje = xmlResultado.Error };

        // 2. Mapear entidad
        var comprobante = new Domain.Entities.Comprobante
        {
            // Cabecera
            TipoOperacion              = dto.TipoOperacion,
            TipoComprobante            = dto.TipoComprobante,
            Serie                      = dto.Serie,
            Correlativo                = int.TryParse(dto.Correlativo, out var corr) ? corr : null,
            NumeroCompleto             = $"{dto.Serie}-{dto.Correlativo}",
            FechaEmision               = dto.FechaEmision,
            HoraEmision                = dto.FechaEmision,
            FechaVencimiento           = dto.FechaVencimiento,
            TipoMoneda                 = dto.TipoMoneda,
            TipoPago                   = dto.TipoPago,

            // Empresa (aplanado en la tabla)
            EmpresaId                  = dto.Company.EmpresaId,
            EmpresaRuc                 = dto.Company.NumeroDocumento,
            EmpresaRazonSocial         = dto.Company.RazonSocial,
            EmpresaNombreComercial     = dto.Company.NombreComercial,
            EmpresaEstablecimientoAnexo= dto.Company.EstablecimientoAnexo,
            EmpresaDireccion           = dto.Company.DireccionLineal,
            EmpresaProvincia           = dto.Company.Provincia,
            EmpresaDepartamento        = dto.Company.Departamento,
            EmpresaDistrito            = dto.Company.Distrito,
            EmpresaUbigeo              = dto.Company.Ubigeo,

            // Cliente (aplanado en la tabla)
            ClienteId                  = dto.Cliente.ClienteId,
            ClienteTipoDoc             = dto.Cliente.TipoDocumento,
            ClienteNumDoc              = dto.Cliente.NumeroDocumento,
            ClienteRazonSocial         = dto.Cliente.RazonSocial,
            ClienteDireccion           = dto.Cliente.DireccionLineal,
            ClienteProvincia           = dto.Cliente.Provincia,
            ClienteDepartamento        = dto.Cliente.Departamento,
            ClienteDistrito            = dto.Cliente.Distrito,
            ClienteUbigeo              = dto.Cliente.Ubigeo,

            // Totales
            TotalOperacionesGravadas   = dto.MtoOperGravadas,
            TotalOperacionesExoneradas = dto.MtoOperExoneradas,
            TotalOperacionesInafectas  = dto.MtoOperInafectas,
            TotalIGV                   = dto.MtoIGV,
            TotalImpuestos             = dto.TotalImpuestos,
            TotalDescuentos            = dto.TotalDescuentos,
            TotalOtrosCargos           = dto.TotalOtrosCargos,
            ValorVenta                 = dto.ValorVenta,
            SubTotal                   = dto.SubTotal,
            TotalIcbper                = dto.TotalIcbper,
            ImporteTotal               = dto.MtoImpVenta,

            // Estado SUNAT
            EstadoSunat                = "PENDIENTE",
            CodigoHashCPE              = null,
            XmlRespuestaSunat          = null,
            CdrSunat                   = null,
            CodigoRespuestaSunat       = null,
            MensajeRespuestaSunat      = null,
            FechaEnvioSunat            = null,
            UsuarioCreacion            = null,
            XmlGenerado                = xmlResultado.XmlBase64,
            FechaCreacion              = DateTime.Now,

            Detalles = dto.Details.Select(d => new Domain.Entities.ComprobanteDetalle
            {
                Item              = d.Item,
                ProductoId        = d.ProductoId,
                Codigo            = d.Codigo,
                Descripcion       = d.Descripcion,
                Cantidad          = d.Cantidad,
                UnidadMedida      = d.UnidadMedida,
                PrecioUnitario    = d.PrecioUnitario,
                TipoAfectacionIGV = d.TipoAfectacionIGV?.ToString(),
                PorcentajeIGV     = d.PorcentajeIGV,
                MontoIGV          = d.MontoIGV,
                BaseIgv           = d.BaseIgv,
                DescuentoUnitario = d.DescuentoUnitario,
                DescuentoTotal    = d.DescuentoTotal,
                ValorVenta        = d.ValorVenta,
                PrecioVenta       = d.PrecioVenta,
                Icbper            = d.Icbper,
                FactorIcbper      = d.FactorIcbper
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
                NumeroCuota      = c.NumeroCuota,
                Monto            = c.Monto,
                FechaVencimiento = c.FechaVencimiento,
                MontoPagado      = c.MontoPagado,
                FechaPago        = c.FechaPago,
                Estado           = c.Estado
            }).ToList() ?? [],

            Leyendas = dto.Legends != null
                ? new List<Domain.Entities.NoteLegend>
                {
                    new Domain.Entities.NoteLegend
                    {
                        Code  = dto.Legends.Code,
                        Value = dto.Legends.Value
                    }
                }
                : []
        };
        // 3. Guardar en BD
        _unitOfWork.BeginTransaction();
        try
        {
            await _unitOfWork.Comprobantes.GenerarComprobanteAsync(comprobante);
            _unitOfWork.Commit();
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }

        return new ComprobanteResponse
        {
            Exitoso       = true,
            Mensaje       = "Comprobante generado correctamente",
            ComprobanteId = comprobante.ComprobanteId,
            XmlBase64     = xmlResultado.XmlBase64,
            XmlString     = xmlResultado.XmlString 
        };
    }
}