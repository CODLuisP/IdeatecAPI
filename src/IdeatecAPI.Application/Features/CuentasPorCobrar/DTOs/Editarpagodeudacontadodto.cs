namespace IdeatecAPI.Application.Features.DeudaContado.DTOs;

public class EditarPagoDeudaContadoDto
{
    public int DeudaPagoId { get; set; }
    public int PagoId { get; set; }
    public decimal MontoPagado { get; set; }
    public DateTime FechaPago { get; set; }
    public string? MedioPago { get; set; }
    public string? EntidadFinanciera { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? Observaciones { get; set; }
    public int? UsuarioRegistroPago { get; set; }
}