namespace IdeatecAPI.Application.Features.Reportes.DTOs;

public class ControlCajaRequestDto
{
    public string Ruc { get; set; } = string.Empty;
    public string? CodEstablecimiento { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? UsuarioCreacion { get; set; }
    public string? ClienteNumDoc { get; set; }
    public int? Limit { get; set; }
}