namespace IdeatecAPI.Application.Features.ComunicacionBaja.DTOs;

public class BajaDto
{
    public int BajaId { get; set; }
    public string Correlativo { get; set; } = string.Empty;
    public DateTime FecGeneracion { get; set; }
    public DateTime FecComunicacion { get; set; }
    public string? EmpresaRuc { get; set; }
    public string? EmpresaRazonSocial { get; set; }
    public string EstadoSunat { get; set; } = string.Empty;
    public string? CodigoRespuestaSunat { get; set; }
    public string? MensajeRespuestaSunat { get; set; }
    public string? TicketSunat { get; set; }
    public DateTime? FechaEnvioSunat { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<BajaDetalleDto> Details { get; set; } = new();
}