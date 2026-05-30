namespace IdeatecAPI.Domain.Entities;

public class Vale
{
    public int IdVale { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public DateTime? FechaEmision { get; set; }
    public string? Duracion { get; set; }
    public bool? Estado { get; set; }
}
