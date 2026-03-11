using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class GuiaRemisionDetalleRepository : IGuiaRemisionDetalleRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public GuiaRemisionDetalleRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection  = connection;
        _transaction = transaction;
    }

    public async Task<IEnumerable<GuiaRemisionDetalle>> GetByGuiaIdAsync(int guiaId)
    {
        var sql = @"SELECT
                        detalleId   AS DetalleId,
                        guiaId      AS GuiaId,
                        cantidad    AS Cantidad,
                        unidad      AS Unidad,
                        descripcion AS Descripcion,
                        codigo      AS Codigo
                    FROM guiaRemisionDetalle
                    WHERE guiaId = @GuiaId";

        return await _connection.QueryAsync<GuiaRemisionDetalle>(sql, new { GuiaId = guiaId }, _transaction);
    }

    public async Task CreateAsync(GuiaRemisionDetalle detalle)
    {
        var sql = @"INSERT INTO guiaRemisionDetalle (
                        guiaId, cantidad, unidad, descripcion, codigo
                    ) VALUES (
                        @GuiaId, @Cantidad, @Unidad, @Descripcion, @Codigo
                    )";

        await _connection.ExecuteAsync(sql, detalle, _transaction);
    }
}