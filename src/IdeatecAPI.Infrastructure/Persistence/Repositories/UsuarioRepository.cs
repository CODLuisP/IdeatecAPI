using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class UsuarioRepository : DapperRepository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    public async Task<Usuario?> GetByIdentifierAsync(string identifier)
    {
        var sql = @"
        SELECT 
            u.usuarioID           AS UsuarioID,
            u.username            AS Username,
            u.password            AS Password,
            u.email               AS Email,
            u.rol                 AS Rol,
            u.estado              AS Estado,
            u.ruc                 AS Ruc,
            u.sucursalID          AS SucursalID,
            s.nombre              AS NombreSucursal,
            COALESCE(e.nombreComercial, e.razonSocial) AS NombreEmpresa,
            e.environment         AS Environment,
            u.tokenVersion        AS TokenVersion,
            u.fechaCreacion       AS FechaCreacion,
            u.fechaUltimoAcceso   AS FechaUltimoAcceso
        FROM usuario u
        LEFT JOIN sucursal s ON s.sucursalID = u.sucursalID
        LEFT JOIN empresa e ON e.ruc = u.ruc
        WHERE (u.username = @Identifier OR u.email = @Identifier OR (u.ruc = @Identifier AND u.rol = 'superadmin'))
        AND u.estado = 1
        LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Usuario>(
            sql, new { Identifier = identifier }, _transaction);
    }

    public async Task<bool> UpdateLastAccessAsync(int usuarioId)
    {
        var sql = @"
            UPDATE usuario 
            SET fechaUltimoAcceso = @Now 
            WHERE usuarioID = @UsuarioId";

        var rowsAffected = await _connection.ExecuteAsync(
            sql, new { UsuarioId = usuarioId, Now = DateTime.UtcNow }, _transaction);

        return rowsAffected > 0;
    }

    public async Task<bool> UpdateRefreshTokenAsync(int usuarioId, string refreshToken)
    {
        var sql = @"
            UPDATE usuario 
            SET refreshToken = @RefreshToken 
            WHERE usuarioID = @UsuarioId";

        var rowsAffected = await _connection.ExecuteAsync(
            sql, new { UsuarioId = usuarioId, RefreshToken = refreshToken }, _transaction);

        return rowsAffected > 0;
    }

    public async Task<int> CreateAsync(Usuario usuario)
    {
        var sql = @"
            INSERT INTO usuario (
                username, password, email, rol, estado,
                ruc, sucursalID, tokenVersion, fechaCreacion
            ) VALUES (
                @Username, @Password, @Email, @Rol, @Estado,
                @Ruc, @SucursalID, @TokenVersion, @FechaCreacion
            );
            SELECT LAST_INSERT_ID();";

        return await _connection.ExecuteScalarAsync<int>(sql, usuario, _transaction);
    }

    public new async Task<Usuario?> GetByIdAsync(int id)
    {
        var sql = @"
        SELECT 
            u.usuarioID           AS UsuarioID,
            u.username            AS Username,
            u.password            AS Password,
            u.sucursalID          AS SucursalID,
            s.nombre              AS NombreSucursal,
            u.email               AS Email,
            u.emailVerified       AS EmailVerified,
            u.rol                 AS Rol,
            u.estado              AS Estado,
            u.ruc                 AS Ruc,
            u.tokenVersion        AS TokenVersion,
            u.refreshToken        AS RefreshToken,
            u.fechaCreacion       AS FechaCreacion,
            u.fechaUltimoAcceso   AS FechaUltimoAcceso,
            u.fechaActualizacion  AS FechaActualizacion
        FROM usuario u
        LEFT JOIN sucursal s ON s.sucursalID = u.sucursalID
        WHERE u.usuarioID = @Id";

        return await _connection.QueryFirstOrDefaultAsync<Usuario>(
            sql, new { Id = id }, _transaction);
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync(bool soloActivos = true, string? ruc = null, string? sucursalID = null, int? usuarioId = null)
    {
        var sql = @"
        SELECT 
            u.usuarioID           AS UsuarioID,
            u.username            AS Username,
            u.email               AS Email,
            u.emailVerified       AS EmailVerified,
            u.rol                 AS Rol,
            u.estado              AS Estado,
            u.ruc                 AS Ruc,
            u.sucursalID          AS SucursalID,
            s.nombre              AS NombreSucursal,
            u.fechaCreacion       AS FechaCreacion,
            u.fechaUltimoAcceso   AS FechaUltimoAcceso
        FROM usuario u
        LEFT JOIN sucursal s ON s.sucursalID = u.sucursalID
        WHERE 1=1";

        if (soloActivos)
            sql += " AND u.estado = 1";

        if (!string.IsNullOrEmpty(ruc))
            sql += " AND u.ruc = @Ruc";

        if (!string.IsNullOrEmpty(sucursalID))
            sql += " AND u.sucursalID = @SucursalID AND u.rol != 'superadmin'";
        
        if (usuarioId.HasValue)
            sql += " AND u.usuarioID = @UsuarioId";

        sql += " ORDER BY u.fechaCreacion DESC";

        return await _connection.QueryAsync<Usuario>(sql, new { Ruc = ruc, SucursalID = sucursalID, UsuarioId = usuarioId }, transaction: _transaction);
    }

    public new async Task<bool> UpdateAsync(Usuario usuario)
    {
        var sql = @"
            UPDATE usuario SET
                username            = @Username,
                email               = @Email,
                sucursalID          = @SucursalID,
                rol                 = @Rol,
                ruc                 = @Ruc,
                fechaActualizacion  = @FechaActualizacion
            WHERE usuarioID = @UsuarioID";

        usuario.FechaActualizacion = DateTime.UtcNow;

        var rowsAffected = await _connection.ExecuteAsync(sql, usuario, _transaction);

        return rowsAffected > 0;
    }

    public new async Task<bool> DeleteAsync(int id)
    {
        var sql = "DELETE FROM usuario WHERE usuarioID = @Id";
        var rows = await _connection.ExecuteAsync(sql, new { Id = id }, _transaction);
        return rows > 0;
    }

    public async Task<bool> ExistsAsync(string username, string? email = null, string? ruc = null, int? excludeId = null)
{
    var sql = @"SELECT COUNT(1) FROM usuario WHERE username = @Username";

    if (excludeId.HasValue)
        sql += " AND usuarioID != @ExcludeId";

    var count = await _connection.ExecuteScalarAsync<int>(
        sql, new { Username = username, ExcludeId = excludeId }, _transaction);

    return count > 0;
}

    // ── Recuperación de contraseña ─────────────────────────────────────────

    public async Task<Usuario?> GetByEmailOrUsernameAsync(string emailOrUsername)
    {
        var sql = @"
            SELECT 
                usuarioID            AS UsuarioID,
                username             AS Username,
                email                AS Email,
                estado               AS Estado,
                resetPasswordToken   AS ResetPasswordToken,
                resetPasswordExpires AS ResetPasswordExpires
            FROM usuario
            WHERE (email = @valor OR username = @valor) AND estado = 1
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Usuario>(
            sql, new { valor = emailOrUsername }, _transaction);
    }

    public async Task<Usuario?> GetByResetTokenAsync(string token)
    {
        var sql = @"
            SELECT 
                usuarioID            AS UsuarioID,
                email                AS Email,
                estado               AS Estado,
                resetPasswordToken   AS ResetPasswordToken,
                resetPasswordExpires AS ResetPasswordExpires
            FROM usuario
            WHERE resetPasswordToken = @token
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Usuario>(
            sql, new { token }, _transaction);
    }

    public async Task SaveResetTokenAsync(int usuarioId, string token, DateTime expires)
    {
        var sql = @"
            UPDATE usuario
            SET resetPasswordToken   = @token,
                resetPasswordExpires = @expires,
                fechaActualizacion   = @now
            WHERE usuarioID = @usuarioId";

        await _connection.ExecuteAsync(
            sql, new { token, expires, now = DateTime.UtcNow, usuarioId }, _transaction);
    }

    public async Task UpdatePasswordAsync(int usuarioId, string hashedPassword)
    {
        var sql = @"
            UPDATE usuario
            SET password             = @hashedPassword,
                resetPasswordToken   = NULL,
                resetPasswordExpires = NULL,
                tokenVersion         = tokenVersion + 1,
                fechaActualizacion   = @now
            WHERE usuarioID = @usuarioId";

        await _connection.ExecuteAsync(
            sql, new { hashedPassword, now = DateTime.UtcNow, usuarioId }, _transaction);
    }

    public async Task<bool> ExisteSuperadminAsync(string ruc)
    {
        var sql = @"
        SELECT COUNT(1) FROM usuario 
        WHERE ruc = @Ruc AND rol = 'superadmin' AND estado = 1";

        var count = await _connection.ExecuteScalarAsync<int>(
            sql, new { Ruc = ruc }, _transaction);

        return count > 0;
    }
}