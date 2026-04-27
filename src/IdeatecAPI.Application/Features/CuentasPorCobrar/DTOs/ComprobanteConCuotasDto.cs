namespace IdeatecAPI.Application.Features.CuentasPorCobrar.DTOs;

public class ComprobanteConCuotasDto
{
    public int ComprobanteId { get; set; }
    public string? TipoComprobante { get; set; }
    public string? Serie { get; set; }
    public int? Correlativo { get; set; }
    public string? NumeroCompleto { get; set; }
    public DateTime? FechaEmision { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public string? TipoMoneda { get; set; }
    public string? EstadoSunat { get; set; }
    public string? EstablecimientoAnexo { get; set; }
    public int? UsuarioCreacion { get; set; }
    public string? ClienteNumDoc { get; set; }
    public string? ClienteRznSocial { get; set; }
    public string? ClienteCorreo { get; set; }
    public string? ClienteWhatsApp { get; set; }
    public decimal? ValorVenta { get; set; }
    public decimal? TotalIGV { get; set; }
    public decimal? ImporteTotal { get; set; }
    public decimal? MontoCredito { get; set; }
    public string? TipoPago { get; set; }
    public List<CuotaDto> Cuotas { get; set; } = new();
}