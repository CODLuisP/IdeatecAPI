using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class RetryRobotRepository : IRetryRobotRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public RetryRobotRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<IEnumerable<int>> GetPendientesComprobantesAsync(bool incluirEnviadoEnResumen)
    {
        const string sql = @"
            SELECT comprobanteID
            FROM comprobante
            WHERE estadoSunat      = 'PENDIENTE'
              AND tipoComprobante IN ('01', '03')
              AND (@IncluirResumen = 1 OR enviadoEnResumen = 0)
            ORDER BY fechaCreacion ASC";

        return await _connection.QueryAsync<int>(
            sql,
            new { IncluirResumen = incluirEnviadoEnResumen ? 1 : 0 },
            _transaction);
    }

    public async Task<IEnumerable<int>> GetPendientesNotasAsync(bool incluirEnviadoEnResumen)
    {
        const string sql = @"
            SELECT comprobanteID
            FROM comprobante
            WHERE estadoSunat      = 'PENDIENTE'
              AND tipoComprobante IN ('07', '08')
              AND (@IncluirResumen = 1 OR enviadoEnResumen = 0)
            ORDER BY fechaCreacion ASC";

        return await _connection.QueryAsync<int>(
            sql,
            new { IncluirResumen = incluirEnviadoEnResumen ? 1 : 0 },
            _transaction);
    }
}
