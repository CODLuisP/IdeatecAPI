using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class NoteLegendRepository : DapperRepository<NoteLegend>, INoteLegendRepository
{
    public NoteLegendRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    public async Task<IEnumerable<NoteLegend>> GetByComprobanteIdAsync(int comprobanteId)
    {
        var sql = @"
            SELECT * FROM noteLegend 
            WHERE comprobanteID = @ComprobanteId";

        return await _connection.QueryAsync<NoteLegend>(sql, new { ComprobanteId = comprobanteId }, _transaction);
    }

    public async Task<int> CreateLegendAsync(NoteLegend legend)
    {
        var sql = @"
            INSERT INTO noteLegend (comprobanteID, code, value)
            VALUES (@ComprobanteId, @Code, @Value);
            SELECT LAST_INSERT_ID();";

        return await _connection.ExecuteScalarAsync<int>(sql, legend, _transaction);
    }

    public async Task DeleteByComprobanteIdAsync(int comprobanteId)
    {
        var sql = "DELETE FROM noteLegend WHERE comprobanteID = @ComprobanteId";
        await _connection.ExecuteAsync(sql, new { ComprobanteId = comprobanteId }, _transaction);
    }
}