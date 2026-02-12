using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdentifierAsync(string identifier);
    Task<bool> UpdateLastAccessAsync(int usuarioId);
    Task<bool> UpdateRefreshTokenAsync(int usuarioId, string refreshToken);
}