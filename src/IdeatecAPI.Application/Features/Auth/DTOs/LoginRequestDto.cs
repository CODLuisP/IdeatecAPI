using System.ComponentModel.DataAnnotations;

namespace IdeatecAPI.Application.Features.Auth.DTOs;

public class LoginRequestDto
{
    [Required(ErrorMessage = "El identificador es requerido")]
    public string Identifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Password { get; set; } = string.Empty;

    public string? Environment { get; set; }

    public bool RememberMe { get; set; } = false;
}