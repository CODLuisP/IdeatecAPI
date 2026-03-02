using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

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
                    d.direccionLineal    AS DireccionLineal,
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
                    AND d.estado = 1
                ORDER BY c.clienteID;";
        var clienteDictionary = new Dictionary<int, Cliente>();

        var clientes = await _connection.QueryAsync<Cliente, TipoDocumento, Direccion, Cliente>(
            sql,
            (cliente, tipoDoc, direccion) =>
            {
                if (!clienteDictionary.TryGetValue(cliente.ClienteId, out var clienteEntry))
                {
                    clienteEntry = cliente;
                    clienteEntry.TipoDocumentoCliente = tipoDoc;
                    clienteEntry.Direcciones = new List<Direccion>();

                    clienteDictionary.Add(clienteEntry.ClienteId, clienteEntry);
                }

                if (direccion != null && direccion.DireccionId != 0)
                {
                    clienteEntry.Direcciones.Add(direccion);
                }

                return clienteEntry;
            },
            transaction: _transaction,
            splitOn: "TipoDocumentoId,DireccionId"
        );

        return clienteDictionary.Values;
    }

    public async Task<Cliente?> GetClienteByIdAsync(int clienteId)
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
                d.direccionLineal    AS DireccionLineal,
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
                AND d.estado = 1
            WHERE c.clienteID = @ClienteId
            AND c.estado = 1;";

        var clienteDictionary = new Dictionary<int, Cliente>();

        await _connection.QueryAsync<Cliente, TipoDocumento, Direccion, Cliente>(
            sql,
            (cliente, tipoDoc, direccion) =>
            {
                if (!clienteDictionary.TryGetValue(cliente.ClienteId, out var clienteEntry))
                {
                    clienteEntry = cliente;
                    clienteEntry.TipoDocumentoCliente = tipoDoc;
                    clienteEntry.Direcciones = new List<Direccion>();

                    clienteDictionary.Add(clienteEntry.ClienteId, clienteEntry);
                }

                if (direccion != null && direccion.DireccionId != 0)
                {
                    clienteEntry.Direcciones.Add(direccion);
                }

                return clienteEntry;
            },
            new { ClienteId = clienteId },
            transaction: _transaction,
            splitOn: "TipoDocumentoId,DireccionId"
        );

        return clienteDictionary.Values.FirstOrDefault();
    }
        
    public async Task<int> RegistrarClienteAsync(Cliente cliente)
    {
        var sql = @"
            INSERT INTO cliente (tipoDocumentoId, numeroDocumento, razonSocial, nombreComercial, telefono, email)
            VALUES (@TipoDocumentoId, @NumeroDocumento, @RazonSocialNombre, @NombreComercial, @Telefono, @Correo);
            SELECT LAST_INSERT_ID();";

        return await _connection.ExecuteScalarAsync<int>(sql, cliente, _transaction);
    }

    public async Task<bool> EditarClienteAsync(Cliente cliente)
    {
        var sql = @"
            UPDATE cliente
            SET 
                numeroDocumento = @NumeroDocumento,
                razonSocial     = @RazonSocialNombre,
                nombreComercial = @NombreComercial,
                telefono        = @Telefono,
                email           = @Correo
            WHERE clienteID = @ClienteId;";

        var result = await _connection.ExecuteAsync(
            sql,
            cliente,
            _transaction
        );

        return result > 0;
    }

    public async Task<bool> EliminarClienteAsync(int clienteId)
    {
        var sql = @"
            UPDATE cliente
            SET estado = 0
            WHERE clienteID = @ClienteId;";

        var result = await _connection.ExecuteAsync(
            sql,
            new { ClienteId = clienteId },
            _transaction
        );

        return result > 0;
    }

    public async Task<Cliente?> GetByNumDocAsync(string numeroDocumento)
    {
        var sql = "SELECT * FROM cliente WHERE numeroDocumento = @NumeroDocumento AND estado = 1";
        return await _connection.QueryFirstOrDefaultAsync<Cliente>(sql, new { NumeroDocumento = numeroDocumento }, _transaction);
    }
}