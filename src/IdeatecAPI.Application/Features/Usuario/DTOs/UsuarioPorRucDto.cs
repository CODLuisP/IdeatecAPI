namespace IdeatecAPI.Application.Features.Usuario.DTOs;

public class UsuarioPorRucDto
{
    public int UsuarioID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string? SucursalID { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Ruc { get; set; }
}