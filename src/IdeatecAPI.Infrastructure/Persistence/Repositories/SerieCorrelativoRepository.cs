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

    public async Task<IEnumerable<SerieCorrelativo>> GetSerieCorrelativoAsync(string empresaRuc, string tipoComprobante)
    {
        var sql = @"
            SELECT serieID       AS SerieId,
                   empresaRuc     AS EmpresaRuc,
                   tipoComprobante,
                   serie,
                   correlativoActual,
                   estado,
                   fechaActualizacion
            FROM serie
            WHERE empresaRuc       = @EmpresaRuc
            AND   tipoComprobante = @TipoComprobante";

        return await _connection.QueryAsync<SerieCorrelativo>(sql, new { EmpresaRuc = empresaRuc, TipoComprobante = tipoComprobante }, _transaction);
    }

    public async Task<int> RegistrarSerieCorrelativoAsync(SerieCorrelativo serie)
    {
        var sql = @"
            INSERT INTO serie (
                empresaRuc, tipoComprobante, serie,
                correlativoActual, estado, fechaActualizacion
            ) VALUES (
                @EmpresaRuc, @TipoComprobante, @Serie,
                @CorrelativoActual, @Estado, @FechaActualizacion
            );
            SELECT LAST_INSERT_ID();";

        return await _connection.ExecuteScalarAsync<int>(sql, serie, _transaction);
    }
}