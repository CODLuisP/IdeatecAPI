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

    public async Task<IEnumerable<Producto>> GetAllProductosAsync()
    {
        var sql = @"
            SELECT 
                p.productoID       AS ProductoId,
                p.codigo           AS Codigo,
                p.tipoProducto     AS TipoProducto,
                p.codigoSunat      AS CodigoSunat,
                p.descripcion      AS Descripcion,
                p.unidadMedida     AS UnidadMedida,
                p.precioUnitario   AS PrecioUnitario,
                p.tipoAfectacionIGV AS TipoAfectacionIGV,
                p.incluirIGV       AS IncluirIGV,
                p.stock            AS Stock,
                p.estado           AS Estado,
                p.fechaCreacion    AS FechaCreacion,
                p.categoriaID      AS CategoriaId,

                c.categoriaID      AS CategoriaId,
                c.categoriaNombre  AS CategoriaNombre,
                c.descripcion      AS Descripcion,
                c.estado           AS Estado

            FROM producto p
            INNER JOIN categoria c 
                ON c.categoriaID = p.categoriaID
            WHERE p.estado = 1
            ORDER BY p.productoID;";

        var productos = await _connection.QueryAsync<Producto, Categoria, Producto>(
            sql,
            (producto, categoria) =>
            {
                producto.Categoria = categoria;
                return producto;
            },
            transaction: _transaction,
            splitOn: "CategoriaId"
        );

        return productos;
    }

    public async Task<Producto?> GetProductoByIdAsync(int id)
    {
        var sql = @"
            SELECT 
                p.productoID       AS ProductoId,
                p.codigo           AS Codigo,
                p.tipoProducto     AS TipoProducto,
                p.codigoSunat      AS CodigoSunat,
                p.descripcion      AS Descripcion,
                p.unidadMedida     AS UnidadMedida,
                p.precioUnitario   AS PrecioUnitario,
                p.tipoAfectacionIGV AS TipoAfectacionIGV,
                p.incluirIGV       AS IncluirIGV,
                p.stock            AS Stock,
                p.estado           AS Estado,
                p.fechaCreacion    AS FechaCreacion,
                p.categoriaID      AS CategoriaId,

                c.categoriaID      AS CategoriaId,
                c.categoriaNombre  AS CategoriaNombre,
                c.descripcion      AS Descripcion,
                c.estado           AS Estado

            FROM producto p
            INNER JOIN categoria c 
                ON c.categoriaID = p.categoriaID
            WHERE p.productoID = @Id
            AND p.estado = 1;";

        var result = await _connection.QueryAsync<Producto, Categoria, Producto>(
            sql,
            (producto, categoria) =>
            {
                producto.Categoria = categoria;
                return producto;
            },
            new { Id = id },
            _transaction,
            splitOn: "CategoriaId"
        );

        return result.FirstOrDefault();
    }

    public async Task<bool> ExisteProductoAsync(string codigo)
    {
        var sql = "SELECT COUNT(1) FROM producto WHERE codigo = @Codigo AND estado = 1";
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { Codigo = codigo }, _transaction);
        return count > 0;
    }

    public async Task<bool> RegistrarProductoAsync(Producto producto)
    {
        var sql = @"
            INSERT INTO producto
            (codigo, tipoProducto, codigoSunat, descripcion, unidadMedida,
             precioUnitario, tipoAfectacionIGV, incluirIGV, stock,
             categoriaID, estado, fechaCreacion)
            VALUES
            (@Codigo, @TipoProducto, @CodigoSunat, @Descripcion, @UnidadMedida,
             @PrecioUnitario, @TipoAfectacionIGV, @IncluirIGV, @Stock,
             @CategoriaId, @Estado, @FechaCreacion);";

        var result = await _connection.ExecuteAsync(sql, producto, _transaction);
        return result > 0;
    }

    public async Task<bool> EditarProductoAsync(Producto producto)
    {
        var sql = @"
            UPDATE producto
            SET
                codigo = @Codigo,
                tipoProducto = @TipoProducto,
                codigoSunat = @CodigoSunat,
                descripcion = @Descripcion,
                unidadMedida = @UnidadMedida,
                precioUnitario = @PrecioUnitario,
                tipoAfectacionIGV = @TipoAfectacionIGV,
                incluirIGV = @IncluirIGV,
                stock = @Stock,
                categoriaID = @CategoriaId
            WHERE productoID = @ProductoId;";

        var result = await _connection.ExecuteAsync(sql, producto, _transaction);
        return result > 0;
    }

    public async Task<bool> EliminarProductoAsync(int productoId)
    {
        var sql = @"
            UPDATE producto
            SET estado = 0
            WHERE productoID = @ProductoId;";

        var result = await _connection.ExecuteAsync(
            sql,
            new { ProductoId = productoId },
            _transaction
        );

        return result > 0;
    }
}