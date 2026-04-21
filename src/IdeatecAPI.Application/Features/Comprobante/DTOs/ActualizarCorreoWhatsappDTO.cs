namespace IdeatecAPI.Application.Features.Comprobante.DTOs;
public class ActualizarCorreoWhatsappDTO
{
    public string? Correo { get; set; }
    public bool? EnviadoPorCorreo { get; set; }
    public string? WhatsApp { get; set; }
    public bool? EnviadoPorWhatsApp { get; set; }
}