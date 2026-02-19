using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities.Cliente;

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
            INSERT INTO direccion (clienteID, ubigeo, departamento, provincia, distrito, tipoDireccion)
            VALUES (@ClienteId, @Ubigeo, @Departamento, @Provincia, @Distrito, @TipoDireccion)";

        await _connection.ExecuteAsync(sql, direccion, _transaction);
    }
}
