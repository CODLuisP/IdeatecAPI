namespace IdeatecAPI.Application.Features.Trabajadores.DTOs;

public class ObtenerTrabajadorDTO
{
    public int Id { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    public string? Dni { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public bool Estado { get; set; }
    public int SucursalId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RegistrarTrabajadorDTO
{
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? Dni { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
    public int SucursalId { get; set; }
}

public class EditarTrabajadorDTO
{
    public int? Id { get; set; }
    public int? SucursalId { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? Dni { get; set; }
    public string? Celular { get; set; }
    public string? Email { get; set; }
}