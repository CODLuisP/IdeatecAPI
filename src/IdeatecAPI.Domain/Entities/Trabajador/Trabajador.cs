namespace IdeatecAPI.Domain.Entities;

public class Trabajador
{
    public int Id { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? Dni { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public bool Estado { get; set; }
    public int SucursalId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}