using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class NotificacionEnviadaRepository : DapperRepository<NotificacionEnviada>, INotificacionEnviadaRepository
{
    public NotificacionEnviadaRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    public async Task<IEnumerable<NotificacionEnviada>> GetAllAsync()
    {
        var sql = @"
            SELECT
                id              AS Id,
                emailEnviado    AS EmailEnviado,
                whatsappEnviado AS WhatsappEnviado
            FROM notificacionenviada
            ORDER BY id;";

        return await _connection.QueryAsync<NotificacionEnviada>(sql, transaction: _transaction);
    }

    public async Task<bool> RegistrarAsync(NotificacionEnviada notificacion)
    {
        var sql = @"
            INSERT INTO notificacionenviada (id, emailEnviado, whatsappEnviado)
            VALUES (@Id, @EmailEnviado, @WhatsappEnviado);";

        var result = await _connection.ExecuteAsync(sql, notificacion, _transaction);
        return result > 0;
    }

    public async Task<bool> EditarAsync(NotificacionEnviada notificacion)
    {
        var sql = @"
            UPDATE notificacionenviada
            SET
                emailEnviado    = @EmailEnviado,
                whatsappEnviado = @WhatsappEnviado
            WHERE id = @Id;";

        var result = await _connection.ExecuteAsync(sql, notificacion, _transaction);
        return result > 0;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var sql = "DELETE FROM notificacionenviada WHERE id = @Id;";
        var result = await _connection.ExecuteAsync(sql, new { Id = id }, _transaction);
        return result > 0;
    }
}
