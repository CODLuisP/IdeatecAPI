using IdeatecAPI.Application.Features.Auth.DTOs;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IUsuarioService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<bool> ChangePasswordAsync(int usuarioId, string currentPassword, string newPassword);
}