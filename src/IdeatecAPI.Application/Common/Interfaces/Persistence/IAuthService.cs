using IdeatecAPI.Application.Features.Auth.DTOs;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<LoginResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(int usuarioId);

}