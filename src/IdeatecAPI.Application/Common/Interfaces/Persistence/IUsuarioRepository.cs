using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdentifierAsync(string identifier);
    Task<bool> UpdateLastAccessAsync(int usuarioId);
    Task<bool> UpdateRefreshTokenAsync(int usuarioId, string refreshToken);

    Task<int> CreateAsync(Usuario usuario);
    Task<Usuario?> GetByIdAsync(int id);
    Task<IEnumerable<Usuario>> GetAllAsync(bool soloActivos = true, string? ruc = null, string? sucursalID = null, int? usuarioId = null);
    Task<bool> UpdateAsync(Usuario usuario);
    Task<bool> DeleteAsync(int id); // Soft delete (cambiar estado a false)
    Task<bool> ExistsAsync(string username, string? email = null, string? ruc = null, int? excludeId = null);

    // ── Métodos para recuperación de contraseña ──

    // Busca usuario por email o username (para forgot-password)
    Task<Usuario?> GetByEmailOrUsernameAsync(string emailOrUsername);

    // Busca usuario por el token de reset
    Task<Usuario?> GetByResetTokenAsync(string token);

    // Guarda el token de reset y su expiración
    Task SaveResetTokenAsync(int usuarioId, string token, DateTime expires);

    // Actualiza la contraseña y limpia el token
    Task UpdatePasswordAsync(int usuarioId, string hashedPassword);

    Task<bool> ExisteSuperadminAsync(string ruc);
}