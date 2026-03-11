namespace IdeatecAPI.Application.Features.ComunicacionBaja.DTOs;

public class CreateBajaDetalleDto
{
    public string TipoDoc { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Correlativo { get; set; } = string.Empty;
    public string DesMotivoBaja { get; set; } = string.Empty;
}