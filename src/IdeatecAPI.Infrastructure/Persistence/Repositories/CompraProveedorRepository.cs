using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class CompraProveedorRepository : DapperRepository<CompraProveedor>, ICompraProveedorRepository
{
    public CompraProveedorRepository(IDbConnection connection, IDbTransaction? transaction = null) : base(connection, transaction)
    {
    }

    private const string SelectBase = @"
        SELECT
            cp.idCompraProveedor AS CompraProveedorId,
            cp.idProveedor       AS ProveedorId,
            pr.razonSocial       AS RazonSocialProveedor,
            cp.idSucursal        AS SucursalId,
            s.nombre             AS NomSucursal,
            cp.idProducto        AS ProductoId,
            p.nomProducto        AS NomProducto,
            cp.precioCompra      AS PrecioCompra,
            cp.cantidad          AS Cantidad,
            cp.unidadMedida      AS UnidadMedida,
            cp.docReferencia     AS DocReferencia,
            cp.fechaCreacion     AS FechaCreacion,
            cp.idUsuario         AS IdUsuario
        FROM compraproveedor cp
        INNER JOIN proveedor pr ON pr.idProveedor = cp.idProveedor
        INNER JOIN sucursal s ON s.sucursalID = cp.idSucursal
        INNER JOIN producto p ON p.productoID = cp.idProducto";

    public async Task<IEnumerable<CompraProveedor>> GetAllBySucursalAsync(int sucursalId)
    {
        var sql = $@"{SelectBase}
            WHERE cp.idSucursal = @SucursalId
            ORDER BY cp.fechaCreacion DESC;";

        return await _connection.QueryAsync<CompraProveedor>(sql, new { SucursalId = sucursalId }, _transaction);
    }

    public async Task<IEnumerable<CompraProveedor>> GetAllByProveedorAsync(int proveedorId)
    {
        var sql = $@"{SelectBase}
            WHERE cp.idProveedor = @ProveedorId
            ORDER BY cp.fechaCreacion DESC;";

        return await _connection.QueryAsync<CompraProveedor>(sql, new { ProveedorId = proveedorId }, _transaction);
    }

    public async Task<IEnumerable<CompraProveedor>> GetByDocReferenciaAsync(string docReferencia, int sucursalId)
    {
        var sql = $@"{SelectBase}
            WHERE cp.docReferencia = @DocReferencia
            AND cp.idSucursal = @SucursalId
            ORDER BY cp.idCompraProveedor;";

        return await _connection.QueryAsync<CompraProveedor>(sql, new { DocReferencia = docReferencia, SucursalId = sucursalId }, _transaction);
    }

    public async Task<CompraProveedor?> GetByIdAsync(int compraProveedorId)
    {
        var sql = $@"{SelectBase}
            WHERE cp.idCompraProveedor = @CompraProveedorId;";

        return await _connection.QueryFirstOrDefaultAsync<CompraProveedor>(sql, new { CompraProveedorId = compraProveedorId }, _transaction);
    }

    public async Task<CompraProveedor> RegistrarAsync(CompraProveedor compra)
    {
        var sql = @"
            INSERT INTO compraproveedor
                (idProveedor, idSucursal, idProducto, precioCompra, cantidad, unidadMedida, fechaCreacion, docReferencia, idUsuario)
            VALUES
                (@ProveedorId, @SucursalId, @ProductoId, @PrecioCompra, @Cantidad, @UnidadMedida, @FechaCreacion, @DocReferencia, @IdUsuario);
            SELECT LAST_INSERT_ID();";

        var newId = await _connection.ExecuteScalarAsync<int>(sql, compra, _transaction);
        compra.CompraProveedorId = newId;
        return compra;
    }

    public async Task<bool> EliminarAsync(int compraProveedorId)
    {
        var sql = @"DELETE FROM compraproveedor WHERE idCompraProveedor = @CompraProveedorId;";
        var result = await _connection.ExecuteAsync(sql, new { CompraProveedorId = compraProveedorId }, _transaction);
        return result > 0;
    }
}
