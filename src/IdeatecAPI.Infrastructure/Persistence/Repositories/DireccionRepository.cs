using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class DireccionRepository : DapperRepository<Direccion>, IDireccionRepository
{
    public DireccionRepository(IDbConnection connection, IDbTransaction? transaction = null) 
        : base(connection, transaction)
    {
    }

        public async Task CrearDireccionAsync(Direccion direccion)
    {
        var sql = @"
            INSERT INTO direccion (clienteID, direccionLineal, ubigeo, departamento, provincia, distrito, tipoDireccion)
            VALUES (@ClienteId, @DireccionLineal, @Ubigeo, @Departamento, @Provincia, @Distrito, @TipoDireccion)";

        await _connection.ExecuteAsync(sql, direccion, _transaction);
    }

    public async Task<bool> EditarDireccionAsync(Direccion direccion)
    {
        var sql = @"
            UPDATE direccion
            SET direccionLineal = @DireccionLineal,
                ubigeo = @Ubigeo,
                departamento = @Departamento,
                provincia = @Provincia,
                distrito = @Distrito,
                tipoDireccion = @TipoDireccion
            WHERE direccionId = @DireccionId";

        var rows = await _connection.ExecuteAsync(sql, direccion, _transaction);

        return rows > 0;
    }

    public async Task<bool> EliminarDireccionAsync(int direccionId)
    {
        var sql = @"
            UPDATE direccion
            SET estado = 0
            WHERE direccionId = @DireccionId";

        var rows = await _connection.ExecuteAsync(sql, new
        {
            DireccionId = direccionId
        }, _transaction);

        return rows > 0;
    }

}
