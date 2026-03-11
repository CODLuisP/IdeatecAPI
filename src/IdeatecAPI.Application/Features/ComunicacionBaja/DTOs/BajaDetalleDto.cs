namespace IdeatecAPI.Application.Features.ComunicacionBaja.DTOs;

public class BajaDetalleDto
{
    public int DetalleId { get; set; }
    public string TipoDoc { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Correlativo { get; set; } = string.Empty;
    public string DesMotivoBaja { get; set; } = string.Empty;
}