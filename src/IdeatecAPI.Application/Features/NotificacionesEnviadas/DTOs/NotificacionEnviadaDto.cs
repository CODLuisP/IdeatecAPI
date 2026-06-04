namespace IdeatecAPI.Application.Features.NotificacionesEnviadas.DTOs;

public class NotificacionEnviadaDto
{
    public int Id { get; set; }
    public bool EmailEnviado { get; set; }
    public bool WhatsappEnviado { get; set; }
}
