namespace IdeatecAPI.Application.Features.CuentasPorCobrar.DTOs;

public class CuotaDto
{
    public int CuotaId { get; set; }
    public int? ComprobanteId { get; set; }
    public string? NumeroCuota { get; set; }
    public decimal? Monto { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string? MontoPagado { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? Estado { get; set; }
    public decimal? MontoDescuento { get; set; }
    public string? MotivoDescuento { get; set; }
    public decimal? MontoFinal { get; set; }
    public decimal? TasaDescuentoDiaria { get; set; }
    public int? DiasAnticipacion { get; set; }
    public decimal? PorcentajeDescuento { get; set; }
    public string? MedioPago { get; set; }
    public string? EntidadFinanciera { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? Observaciones { get; set; }
    public int? UsuarioRegistroPago { get; set; }
}