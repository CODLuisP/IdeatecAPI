using System.ComponentModel.DataAnnotations;

namespace IdeatecAPI.Application.Features.Auth.DTOs;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "El username es requerido")]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "El username debe tener entre 4 y 50 caracteres")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener mínimo 6 caracteres")]
    [StringLength(255)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es requerido")]
    [StringLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [StringLength(11)]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe tener exactamente 11 dígitos")]
    public string? Ruc { get; set; }

    [StringLength(200)]
    public string? RazonSocial { get; set; }

    [StringLength(20)]
    [Phone(ErrorMessage = "Teléfono inválido")]
    public string? Telefono { get; set; }

    [StringLength(20)]
    public string? Rol { get; set; } = "usuario"; // Por defecto "usuario"
}