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
                usuarioID as UsuarioID,
                username as Username,
                password as Password,
                nombreCompleto as NombreCompleto,
                email as Email,
                rol as Rol,
                estado as Estado,
                ruc as Ruc,
                razonSocial as RazonSocial,
                imagen as Imagen,
                tokenVersion as TokenVersion,
                fechaCreacion as FechaCreacion,
                fechaUltimoAcceso as FechaUltimoAcceso
            FROM usuario 
            WHERE (email = @Identifier OR username = @Identifier OR ruc = @Identifier)
            AND estado = 1
            LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<Usuario>(
            sql,
            new { Identifier = identifier },
            _transaction
        );
    }

    public async Task<bool> UpdateLastAccessAsync(int usuarioId)
    {
        var sql = @"
            UPDATE usuario 
            SET fechaUltimoAcceso = @Now 
            WHERE usuarioID = @UsuarioId";

        var rowsAffected = await _connection.ExecuteAsync(
            sql,
            new { UsuarioId = usuarioId, Now = DateTime.UtcNow },
            _transaction
        );

        return rowsAffected > 0;
    }

    public async Task<bool> UpdateRefreshTokenAsync(int usuarioId, string refreshToken)
    {
        var sql = @"
            UPDATE usuario 
            SET refreshToken = @RefreshToken 
            WHERE usuarioID = @UsuarioId";

        var rowsAffected = await _connection.ExecuteAsync(
            sql,
            new { UsuarioId = usuarioId, RefreshToken = refreshToken },
            _transaction
        );

        return rowsAffected > 0;
    }

    public async Task<int> CreateAsync(Usuario usuario)
    {
        var sql = @"
        INSERT INTO usuario (
            username, 
            password, 
            nombreCompleto, 
            email, 
            rol, 
            estado,
            ruc, 
            razonSocial,
            imagen,
            tokenVersion,
            fechaCreacion
        )
        VALUES (
            @Username, 
            @Password, 
            @NombreCompleto, 
            @Email, 
            @Rol, 
            @Estado,
            @Ruc, 
            @RazonSocial,
            @Imagen,
            @TokenVersion,
            @FechaCreacion
        );
        SELECT LAST_INSERT_ID();";

        return await _connection.ExecuteScalarAsync<int>(sql, usuario, _transaction);
    }

    public new async Task<Usuario?> GetByIdAsync(int id)
    {
        var sql = @"
        SELECT 
            usuarioID as UsuarioID,
            username as Username,
            password as Password,
            nombreCompleto as NombreCompleto,
            email as Email,
            emailVerified as EmailVerified,
            imagen as Imagen,
            rol as Rol,
            estado as Estado,
            ruc as Ruc,
            razonSocial as RazonSocial,
            tokenVersion as TokenVersion,
            refreshToken as RefreshToken,
            fechaCreacion as FechaCreacion,
            fechaUltimoAcceso as FechaUltimoAcceso,
            fechaActualizacion as FechaActualizacion
        FROM usuario 
        WHERE usuarioID = @Id";

        return await _connection.QueryFirstOrDefaultAsync<Usuario>(
            sql,
            new { Id = id },
            _transaction
        );
    }


    public async Task<IEnumerable<Usuario>> GetAllAsync(bool soloActivos = true)
    {
        var sql = @"
        SELECT 
            usuarioID as UsuarioID,
            username as Username,
            nombreCompleto as NombreCompleto,
            email as Email,
            emailVerified as EmailVerified,
            imagen as Imagen,
            rol as Rol,
            estado as Estado,
            ruc as Ruc,
            razonSocial as RazonSocial,
            fechaCreacion as FechaCreacion,
            fechaUltimoAcceso as FechaUltimoAcceso
        FROM usuario";

        if (soloActivos)
        {
            sql += " WHERE estado = 1";
        }

        sql += " ORDER BY fechaCreacion DESC";

        return await _connection.QueryAsync<Usuario>(sql, transaction: _transaction);
    }

    public new async Task<bool> UpdateAsync(Usuario usuario)
    {
        var sql = @"
        UPDATE usuario SET
            username = @Username,
            nombreCompleto = @NombreCompleto,
            email = @Email,
            rol = @Rol,
            ruc = @Ruc,
            razonSocial = @RazonSocial,
            imagen = @Imagen,
            fechaActualizacion = @FechaActualizacion
        WHERE usuarioID = @UsuarioID";

        usuario.FechaActualizacion = DateTime.UtcNow;

        var rowsAffected = await _connection.ExecuteAsync(
            sql,
            usuario,
            _transaction
        );

        return rowsAffected > 0;
    }

    public new async Task<bool> DeleteAsync(int id)
    {
        // Soft delete - cambiar estado a inactivo
        var sql = @"
        UPDATE usuario 
        SET estado = 0, 
            fechaActualizacion = @Now 
        WHERE usuarioID = @Id";

        var rowsAffected = await _connection.ExecuteAsync(
            sql,
            new { Id = id, Now = DateTime.UtcNow },
            _transaction
        );

        return rowsAffected > 0;
    }

    public async Task<bool> ExistsAsync(string username, string email, string? ruc = null, int? excludeId = null)
    {
        var sql = @"
        SELECT COUNT(1) 
        FROM usuario 
        WHERE (username = @Username OR email = @Email";

        if (!string.IsNullOrEmpty(ruc))
        {
            sql += " OR ruc = @Ruc";
        }

        sql += ")";

        if (excludeId.HasValue)
        {
            sql += " AND usuarioID != @ExcludeId";
        }

        var count = await _connection.ExecuteScalarAsync<int>(
            sql,
            new { Username = username, Email = email, Ruc = ruc, ExcludeId = excludeId },
            _transaction
        );

        return count > 0;
    }
}