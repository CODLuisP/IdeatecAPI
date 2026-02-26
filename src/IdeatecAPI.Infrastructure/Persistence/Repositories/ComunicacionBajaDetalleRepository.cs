using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class ComunicacionBajaDetalleRepository : IComunicacionBajaDetalleRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public ComunicacionBajaDetalleRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection  = connection;
        _transaction = transaction;
    }

    public async Task<IEnumerable<ComunicacionBajaDetalle>> GetByBajaIdAsync(int bajaId)
    {
        var sql = @"SELECT
                        detalleId     AS DetalleId,
                        bajaId        AS BajaId,
                        tipoDoc       AS TipoDoc,
                        serie         AS Serie,
                        correlativo   AS Correlativo,
                        desMotivoBaja AS DesMotivoBaja
                    FROM comunicacionBajaDetalle
                    WHERE bajaId = @BajaId";
        return await _connection.QueryAsync<ComunicacionBajaDetalle>(sql, new { BajaId = bajaId }, _transaction);
    }

    public async Task CreateAsync(ComunicacionBajaDetalle detalle)
    {
        var sql = @"INSERT INTO comunicacionBajaDetalle (
                        bajaId, tipoDoc, serie, correlativo, desMotivoBaja
                    ) VALUES (
                        @BajaId, @TipoDoc, @Serie, @Correlativo, @DesMotivoBaja
                    )";
        await _connection.ExecuteAsync(sql, detalle, _transaction);
    }
}