using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdentifierAsync(string identifier);
    Task<bool> UpdateLastAccessAsync(int usuarioId);
    Task<bool> UpdateRefreshTokenAsync(int usuarioId, string refreshToken);

    Task<int> CreateAsync(Usuario usuario);
    Task<Usuario?> GetByIdAsync(int id);
    Task<IEnumerable<Usuario>> GetAllAsync(bool soloActivos = true);
    Task<bool> UpdateAsync(Usuario usuario);
    Task<bool> DeleteAsync(int id); // Soft delete (cambiar estado a false)
    Task<bool> ExistsAsync(string username, string email, string? ruc = null, int? excludeId = null);

}