using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class ProductoRepository : DapperRepository<Producto>, IProductoRepository
{
    public ProductoRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    private const string SelectColumns = @"
        SELECT
            p.productoID        AS ProductoId,
            p.codigo            AS Codigo,
            p.tipoProducto      AS TipoProducto,
            p.codigoSunat       AS CodigoSunat,
            p.nomProducto       AS NomProducto,
            p.unidadMedida      AS UnidadMedida,
            p.tipoAfectacionIGV AS TipoAfectacionIGV,
            p.incluirIGV        AS IncluirIGV,
            p.estado            AS Estado,
            p.fechaCreacion     AS FechaCreacion,
            p.categoriaID       AS CategoriaId,

            c.categoriaID       AS CategoriaId,
            c.categoriaNombre   AS CategoriaNombre,

            sp.sucursalProductoID        AS SucursalProductoId,
            sp.precioUnitario            AS PrecioUnitario,
            sp.stock                     AS Stock
        FROM producto p
        INNER JOIN categoria c
            ON c.categoriaID = p.categoriaID
        INNER JOIN sucursalProducto sp
            ON sp.productoID = p.productoID
        WHERE p.estado = 1
        AND sp.estado = 1";

    public async Task<IEnumerable<Producto>> GetAllProductosAsync(int sucursalId)
    {
        var sql = $"{SelectColumns} AND sp.sucursalID = @SucursalId ORDER BY p.productoID";

        var productos = await _connection.QueryAsync<Producto, Categoria, SucursalProducto, Producto>(
            sql,
            (producto, categoria, sucursalProducto) =>
            {
                producto.Categoria       = categoria;
                producto.SucursalProducto = sucursalProducto;
                return producto;
            },
            new { SucursalId = sucursalId },
            transaction: _transaction,
            splitOn: "CategoriaId,SucursalProductoId"
        );

        return productos;
    }

    public async Task<Producto?> GetProductoByIdAsync(int productoId, int sucursalId)
    {
        var sql = $"{SelectColumns} AND p.productoID = @ProductoId AND sp.sucursalID = @SucursalId";

        var result = await _connection.QueryAsync<Producto, Categoria, SucursalProducto, Producto>(
            sql,
            (producto, categoria, sucursalProducto) =>
            {
                producto.Categoria        = categoria;
                producto.SucursalProducto = sucursalProducto;
                return producto;
            },
            new { ProductoId = productoId, SucursalId = sucursalId },
            transaction: _transaction,
            splitOn: "CategoriaId,SucursalProductoId"
        );

        return result.FirstOrDefault();
    }

    public async Task<bool> ExisteProductoAsync(string codigo)
    {
        var sql = "SELECT COUNT(1) FROM producto WHERE codigo = @Codigo AND estado = 1";
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { Codigo = codigo }, _transaction);
        return count > 0;
    }

    public async Task<Producto> RegistrarProductoAsync(Producto producto)
    {
        var sql = @"
            INSERT INTO producto (
                codigo, tipoProducto, codigoSunat, nomProducto,
                unidadMedida, tipoAfectacionIGV, incluirIGV,
                categoriaID
            ) VALUES (
                @Codigo, @TipoProducto, @CodigoSunat, @NomProducto,
                @UnidadMedida, @TipoAfectacionIGV, @IncluirIGV,
                @CategoriaId
            );
            SELECT LAST_INSERT_ID();";

        var newId = await _connection.ExecuteScalarAsync<int>(sql, new
        {
            producto.Codigo,
            producto.TipoProducto,
            producto.CodigoSunat,
            producto.NomProducto,
            producto.UnidadMedida,
            producto.TipoAfectacionIGV,
            producto.IncluirIGV,
            producto.CategoriaId,
        }, _transaction);
        producto.ProductoId = newId;
        return producto;
    }

    public async Task<SucursalProducto> RegistrarSucursalProductoAsync(SucursalProducto sucursalProducto)
    {
        var sql = @"
            INSERT INTO sucursalProducto (
                productoID, sucursalID, precioUnitario, stock, estado, fechaCreacion
            ) VALUES (
                @ProductoId, @SucursalId, @PrecioUnitario, @Stock, @Estado, @FechaCreacion
            );
            SELECT LAST_INSERT_ID();";

        var newId = await _connection.ExecuteScalarAsync<int>(sql, sucursalProducto, _transaction);
        sucursalProducto.SucursalProductoId = newId;
        return sucursalProducto;
    }

    public async Task<bool> EditarProductoAsync(Producto producto)
    {
        var sql = @"
            UPDATE producto SET
                codigo            = @Codigo,
                tipoProducto      = @TipoProducto,
                codigoSunat       = @CodigoSunat,
                nomProducto       = @NomProducto,
                unidadMedida      = @UnidadMedida,
                tipoAfectacionIGV = @TipoAfectacionIGV,
                incluirIGV        = @IncluirIGV,
                categoriaID       = @CategoriaId
            WHERE productoID = @ProductoId AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, producto, _transaction);
        return filas > 0;
    }

    public async Task<bool> EditarSucursalProductoAsync(SucursalProducto sucursalProducto)
    {
        var sql = @"
            UPDATE sucursalProducto SET
                precioUnitario = @PrecioUnitario,
                stock          = @Stock
            WHERE sucursalProductoID = @SucursalProductoId AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, sucursalProducto, _transaction);
        return filas > 0;
    }

    public async Task<bool> EliminarSucursalProductoAsync(int sucursalProductoId)
    {
        var sql = @"UPDATE sucursalProducto SET estado = 0 
                    WHERE sucursalProductoID = @SucursalProductoId AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, new { SucursalProductoId = sucursalProductoId }, _transaction);
        return filas > 0;
    }
}