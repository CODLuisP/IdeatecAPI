using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class NotificacionDiasRepository : DapperRepository<NotificacionDias>, INotificacionDiasRepository
{
    public NotificacionDiasRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    public async Task<IEnumerable<NotificacionDias>> GetAllNotificacionDiasAsync()
    {
        var sql = "SELECT * FROM notificaciondias";
        return await _connection.QueryAsync<NotificacionDias>(sql, transaction: _transaction);
    }

    public async Task<bool> RegistrarNotificacionDiasAsync(NotificacionDias notificacionDias)
    {
        var sql = @"
            INSERT INTO notificaciondias
                (periodo, dias)
            VALUES
                (@Periodo, @Dias);";

        var result = await _connection.ExecuteAsync(sql, notificacionDias, _transaction);
        return result > 0;
    }

    public async Task<bool> EditarNotificacionDiasAsync(NotificacionDias notificacionDias)
    {
        var sql = @"
            UPDATE notificaciondias
            SET
                periodo = @Periodo,
                dias    = @Dias
            WHERE id = @Id;";

        var result = await _connection.ExecuteAsync(sql, notificacionDias, _transaction);
        return result > 0;
    }
}
