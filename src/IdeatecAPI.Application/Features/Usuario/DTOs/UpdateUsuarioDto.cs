using System.ComponentModel.DataAnnotations;

namespace IdeatecAPI.Application.Features.Usuario.DTOs;

public class UpdateUsuarioDto
{
    [Required]
    public int UsuarioID { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 4)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Rol { get; set; }
}