using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;
public class SerieCorrelativoRepository : DapperRepository<SerieCorrelativo>, ISerieCorrelativoRepository
{
    public SerieCorrelativoRepository(IDbConnection connection, IDbTransaction? transaction = null) : base(connection, transaction)
    {
    }

    public async Task<IEnumerable<SerieCorrelativo>> GetSerieCorrelativoAsync(int empresaId, string tipoComprobante)
    {
        var sql = @"
            SELECT serieID       AS SerieId,
                   empresaID     AS EmpresaId,
                   tipoComprobante,
                   serie,
                   correlativoActual,
                   estado,
                   fechaActualizacion
            FROM serie
            WHERE empresaID       = @EmpresaId
            AND   tipoComprobante = @TipoComprobante";

        return await _connection.QueryAsync<SerieCorrelativo>(sql, new { EmpresaId = empresaId, TipoComprobante = tipoComprobante }, _transaction);
    }

    public async Task<int> RegistrarSerieCorrelativoAsync(SerieCorrelativo serie)
    {
        var sql = @"
            INSERT INTO serie (
                empresaID, tipoComprobante, serie,
                correlativoActual, estado, fechaActualizacion
            ) VALUES (
                @EmpresaId, @TipoComprobante, @Serie,
                @CorrelativoActual, @Estado, @FechaActualizacion
            );
            SELECT LAST_INSERT_ID();";

        return await _connection.ExecuteScalarAsync<int>(sql, serie, _transaction);
    }
}