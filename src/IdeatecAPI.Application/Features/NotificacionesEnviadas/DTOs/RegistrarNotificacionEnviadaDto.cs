namespace IdeatecAPI.Application.Features.NotificacionesEnviadas.DTOs;

public class RegistrarNotificacionEnviadaDto
{
    public int Id { get; set; }
    public bool EmailEnviado { get; set; }
    public bool WhatsappEnviado { get; set; }
}
