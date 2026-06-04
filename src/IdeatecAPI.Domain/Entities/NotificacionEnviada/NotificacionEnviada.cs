namespace IdeatecAPI.Domain.Entities;

public class NotificacionEnviada
{
    public int Id { get; set; }
    public bool EmailEnviado { get; set; }
    public bool WhatsappEnviado { get; set; }
}
