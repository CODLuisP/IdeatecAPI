using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class ProveedorRepository : DapperRepository<Proveedor>, IProveedorRepository
{
    public ProveedorRepository(IDbConnection connection, IDbTransaction? transaction = null) : base(connection, transaction)
    {
    }

    public async Task<IEnumerable<Proveedor>> GetAllByRucEmpresaAsync(string rucEmpresa)
    {
        var sql = @"
            SELECT
                idProveedor      AS ProveedorId,
                numDocumento     AS NumDocumento,
                razonSocial      AS RazonSocial,
                nombreComercial  AS NombreComercial,
                direccion        AS Direccion,
                telefono         AS Telefono,
                email            AS Email,
                personaContacto  AS PersonaContacto,
                rucEmpresa       AS RucEmpresa,
                estado           AS Estado,
                fechaCreacion    AS FechaCreacion,
                idUsuario        AS IdUsuario
            FROM proveedor
            WHERE rucEmpresa = @RucEmpresa
            AND estado = 1
            ORDER BY razonSocial;";

        return await _connection.QueryAsync<Proveedor>(sql, new { RucEmpresa = rucEmpresa }, _transaction);
    }

    public async Task<Proveedor?> GetByIdAsync(int proveedorId)
    {
        var sql = @"
            SELECT
                idProveedor      AS ProveedorId,
                numDocumento     AS NumDocumento,
                razonSocial      AS RazonSocial,
                nombreComercial  AS NombreComercial,
                direccion        AS Direccion,
                telefono         AS Telefono,
                email            AS Email,
                personaContacto  AS PersonaContacto,
                rucEmpresa       AS RucEmpresa,
                estado           AS Estado,
                fechaCreacion    AS FechaCreacion,
                idUsuario        AS IdUsuario
            FROM proveedor
            WHERE idProveedor = @ProveedorId
            AND estado = 1;";

        return await _connection.QueryFirstOrDefaultAsync<Proveedor>(sql, new { ProveedorId = proveedorId }, _transaction);
    }

    public async Task<Proveedor?> GetByIdRucEmpresaAsync(string rucEmpresa, int proveedorId)
    {
        var sql = @"
            SELECT
                idProveedor      AS ProveedorId,
                numDocumento     AS NumDocumento,
                razonSocial      AS RazonSocial,
                nombreComercial  AS NombreComercial,
                direccion        AS Direccion,
                telefono         AS Telefono,
                email            AS Email,
                personaContacto  AS PersonaContacto,
                rucEmpresa       AS RucEmpresa,
                estado           AS Estado,
                fechaCreacion    AS FechaCreacion,
                idUsuario        AS IdUsuario
            FROM proveedor
            WHERE rucEmpresa = @RucEmpresa
            AND idProveedor = @ProveedorId
            AND estado = 1;";

        return await _connection.QueryFirstOrDefaultAsync<Proveedor>(sql, new { RucEmpresa = rucEmpresa, ProveedorId = proveedorId }, _transaction);
    }

    public async Task<Proveedor?> GetByNumDocRucEmpresaAsync(string rucEmpresa, string numDocumento)
    {
        var sql = @"
            SELECT idProveedor AS ProveedorId
            FROM proveedor
            WHERE rucEmpresa = @RucEmpresa
            AND numDocumento = @NumDocumento
            AND estado = 1
            LIMIT 1;";

        return await _connection.QueryFirstOrDefaultAsync<Proveedor>(sql, new { RucEmpresa = rucEmpresa, NumDocumento = numDocumento }, _transaction);
    }

    public async Task<Proveedor> RegistrarAsync(Proveedor proveedor)
    {
        var sql = @"
            INSERT INTO proveedor
                (numDocumento, razonSocial, nombreComercial, direccion, telefono, email, personaContacto, rucEmpresa, estado, fechaCreacion, idUsuario)
            VALUES
                (@NumDocumento, @RazonSocial, @NombreComercial, @Direccion, @Telefono, @Email, @PersonaContacto, @RucEmpresa, @Estado, @FechaCreacion, @IdUsuario);
            SELECT LAST_INSERT_ID();";

        var newId = await _connection.ExecuteScalarAsync<int>(sql, proveedor, _transaction);
        proveedor.ProveedorId = newId;
        return proveedor;
    }

    public async Task<bool> EditarAsync(Proveedor proveedor)
    {
        var sql = @"
            UPDATE proveedor
            SET
                numDocumento    = @NumDocumento,
                razonSocial     = @RazonSocial,
                nombreComercial = @NombreComercial,
                direccion       = @Direccion,
                telefono        = @Telefono,
                email           = @Email,
                personaContacto = @PersonaContacto
            WHERE idProveedor = @ProveedorId;";

        var result = await _connection.ExecuteAsync(sql, proveedor, _transaction);
        return result > 0;
    }

    public async Task<bool> EliminarAsync(int proveedorId)
    {
        var sql = @"UPDATE proveedor SET estado = 0 WHERE idProveedor = @ProveedorId;";
        var result = await _connection.ExecuteAsync(sql, new { ProveedorId = proveedorId }, _transaction);
        return result > 0;
    }

    public async Task<IEnumerable<Proveedor>> SearchByRucEmpresaAsync(string rucEmpresa, string palabra)
    {
        var sql = @"
            SELECT
                idProveedor      AS ProveedorId,
                numDocumento     AS NumDocumento,
                razonSocial      AS RazonSocial,
                nombreComercial  AS NombreComercial,
                direccion        AS Direccion,
                telefono         AS Telefono,
                email            AS Email,
                personaContacto  AS PersonaContacto,
                rucEmpresa       AS RucEmpresa,
                estado           AS Estado,
                fechaCreacion    AS FechaCreacion,
                idUsuario        AS IdUsuario
            FROM proveedor
            WHERE rucEmpresa = @RucEmpresa
            AND estado = 1
            AND (razonSocial LIKE @Palabra OR numDocumento LIKE @Palabra OR nombreComercial LIKE @Palabra)
            ORDER BY razonSocial ASC
            LIMIT 10;";

        return await _connection.QueryAsync<Proveedor>(sql, new { RucEmpresa = rucEmpresa, Palabra = $"%{palabra}%" }, _transaction);
    }
}
