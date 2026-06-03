namespace IdeatecAPI.Application.Features.DeudaContado.DTOs;

public class PagoDeudaContadoDto
{
    public int DeudaPagoID { get; set; }
    public int PagoID { get; set; }
    public decimal MontoPagado { get; set; }
    public DateTime FechaPago { get; set; }
    public string? MedioPago { get; set; }
    public string? EntidadFinanciera { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? Observaciones { get; set; }
    public int? UsuarioRegistroPago { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string? TipoMoneda { get; set; }
}