namespace IdeatecAPI.Application.Features.CuentasPorCobrar.DTOs;

public class CuotaPagoDto
{
    public int CuotaPagoId { get; set; }
    public int CuotaId { get; set; }
    public decimal MontoPagado { get; set; }
    public DateTime FechaPago { get; set; }
    public string? MedioPago { get; set; }
    public string? EntidadFinanciera { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? Observaciones { get; set; }
    public int? UsuarioRegistroPago { get; set; }
    public DateTime FechaRegistro { get; set; }
}