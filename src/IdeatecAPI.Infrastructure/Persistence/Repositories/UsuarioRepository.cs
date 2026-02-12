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
}