using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class ValeRepository : DapperRepository<Vale>, IValeRepository
{
    public ValeRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    public async Task<IEnumerable<Vale>> GetAllValesAsync()
    {
        var sql = "SELECT * FROM vale WHERE estado = 1;";
        return await _connection.QueryAsync<Vale>(sql, transaction: _transaction);
    }

    public async Task<bool> RegistrarValeAsync(Vale vale)
    {
        var sql = @"
            INSERT INTO vale
                (nombre, descripcion, fechaemision, duracion, estado)
            VALUES
                (@Nombre, @Descripcion, @FechaEmision, @Duracion, 1);";

        var result = await _connection.ExecuteAsync(sql, vale, _transaction);
        return result > 0;
    }

    public async Task<bool> EditarValeAsync(int idVale, Vale vale)
    {
        var sql = @"
            UPDATE vale
            SET
                nombre       = @Nombre,
                descripcion  = @Descripcion,
                fechaemision = @FechaEmision,
                duracion     = @Duracion
            WHERE idvale = @IdVale;";

        var result = await _connection.ExecuteAsync(sql, new
        {
            vale.Nombre,
            vale.Descripcion,
            vale.FechaEmision,
            vale.Duracion,
            IdVale = idVale
        }, _transaction);

        return result > 0;
    }

    public async Task<bool> EliminarValeAsync(int idVale)
    {
        var sql = @"
            UPDATE vale
            SET estado = 0
            WHERE idvale = @IdVale;";

        var result = await _connection.ExecuteAsync(sql, new { IdVale = idVale }, _transaction);
        return result > 0;
    }
}
