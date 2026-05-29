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

    public async Task<IEnumerable<NotificacionEnviada>> GetAllNotificacionesEnviadasAsync()
    {
        var sql = "SELECT * FROM notificacionenviada";
        return await _connection.QueryAsync<NotificacionEnviada>(sql, transaction: _transaction);
    }

    public async Task<bool> RegistrarNotificacionEnviadaAsync(NotificacionEnviada notificacion)
    {
        var sql = @"
            INSERT INTO notificacionenviada
                (numdoc, periodoTipo, moneda, tipoDoc, emailEnviado, whatsappEnviado, fechafin, fechaEnvio, usuarioId)
            VALUES
                (@NumDoc, @PeriodoTipo, @Moneda, @TipoDoc, @EmailEnviado, @WhatsappEnviado, @FechaFin, @FechaEnvio, @UsuarioId);";

        var result = await _connection.ExecuteAsync(sql, notificacion, _transaction);
        return result > 0;
    }

    public async Task<bool> EditarNotificacionEnviadaAsync(NotificacionEnviada notificacion)
    {
        var sql = @"
            UPDATE notificacionenviada
            SET
                numdoc          = @NumDoc,
                periodoTipo     = @PeriodoTipo,
                moneda          = @Moneda,
                tipoDoc         = @TipoDoc,
                emailEnviado    = @EmailEnviado,
                whatsappEnviado = @WhatsappEnviado,
                fechafin        = @FechaFin,
                fechaEnvio      = @FechaEnvio,
                usuarioId       = @UsuarioId
            WHERE id = @Id;";

        var result = await _connection.ExecuteAsync(sql, notificacion, _transaction);
        return result > 0;
    }
}
