namespace IdeatecAPI.Application.Features.Auth.DTOs;

public class RegisterResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UsuarioDto? User { get; set; }
}