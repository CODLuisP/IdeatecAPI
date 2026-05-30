namespace IdeatecAPI.Application.Features.NotificacionesEnviadas.DTOs;

public class RegistrarNotificacionEnviadaDto
{
    public string? NumDoc { get; set; }
    public string? PeriodoTipo { get; set; }
    public string? Moneda { get; set; }
    public string? TipoDoc { get; set; }
    public bool? EmailEnviado { get; set; }
    public bool? WhatsappEnviado { get; set; }
    public DateTime? FechaFin { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public int? UsuarioId { get; set; }
}
