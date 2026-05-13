namespace IdeatecAPI.Application.Features.CuentasPorCobrar.DTOs;

public class CuotaDto
{
    public int CuotaId { get; set; }
    public int? ComprobanteId { get; set; }
    public string? NumeroCuota { get; set; }
    public decimal? Monto { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal? MontoPagado { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? Estado { get; set; }
    public int? UsuarioRegistroPago { get; set; }
}