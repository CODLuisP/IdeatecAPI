namespace IdeatecAPI.Application.Features.Vales.DTOs;

public class EditarValeDto
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public DateTime? FechaEmision { get; set; }
    public string? Duracion { get; set; }
    public bool? Estado { get; set; }
}
