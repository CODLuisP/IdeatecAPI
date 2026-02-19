namespace IdeatecAPI.Application.Features.Auth.DTOs;

public class LoginResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UsuarioDto? User { get; set; }
}