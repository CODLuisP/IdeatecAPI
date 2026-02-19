using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities.Cliente;
using IdeatecAPI.Domain.Entities.CatalogosSunat;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories.Clientes
{
    public class ClienteRepository : DapperRepository<Cliente>, IClienteRepository
    {
        public ClienteRepository(IDbConnection connection, IDbTransaction? transaction = null) : base(connection, transaction)
        {
        }

        public async Task<IEnumerable<Cliente>> GetAllClientesAsync()
        {
            var sql = @"
                    SELECT
                        c.clienteID          AS ClienteId,
                        c.numeroDocumento    AS NumeroDocumento,
                        c.razonSocial        AS RazonSocialNombre,
                        c.nombreComercial    AS NombreComercial,
                        c.fechaCreacion      AS FechaCreacion,
                        c.telefono           AS Telefono,
                        c.email              AS Correo,
                        c.estado             AS Estado,

                        td.tipoDocumentoId   AS TipoDocumentoId,
                        td.tipoDocumento     AS TipoDocumentoNombre,

                        d.direccionId        AS DireccionId,
                        d.ubigeo             AS Ubigeo,
                        d.departamento       AS Departamento,
                        d.provincia          AS Provincia,
                        d.distrito           AS Distrito,
                        d.tipoDireccion      AS TipoDireccion
                    FROM cliente c
                    LEFT JOIN tipodocumento td 
                        ON td.tipoDocumentoId = c.tipoDocumentoId

                    LEFT JOIN direccion d 
                        ON d.clienteID = c.clienteID
                        AND d.tipoDireccion = 'FISCAL'
                        AND d.estado = 1
                    ORDER BY c.clienteID;";
    var clientes = await _connection.QueryAsync<Cliente, TipoDocumento, Direccion, Cliente>(
        sql,
        (cliente, tipoDoc, direccion) =>
        {
            cliente.TipoDocumentoCliente = tipoDoc;

            if (direccion != null && direccion.DireccionId != 0)
                cliente.Direcciones = new List<Direccion> { direccion };
            else
                cliente.Direcciones = new List<Direccion>();

            return cliente;
        },
        transaction: _transaction,
        splitOn: "TipoDocumentoId,DireccionId"
    );

    return clientes;
        }
    }
}