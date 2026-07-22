using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Productos.DTO;
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
            p.codigoBarras      AS CodigoBarras,
            p.esPaquete         AS EsPaquete,
            p.productoBaseId    AS ProductoBaseId,
            p.factorConversion  AS FactorConversion,
            p.urlImagenProducto AS UrlImagenProducto,

            c.categoriaID       AS CategoriaId,
            c.categoriaNombre   AS CategoriaNombre,

            sp.sucursalProductoID        AS SucursalProductoId,
            sp.precioUnitario            AS PrecioUnitario,
            sp.stock                     AS Stock,
            sp.ultimoPrecioCompra        AS UltimoPrecioCompra,
            sp.fechaUltimaCompra         AS FechaUltimaCompra,
            sp.precioMayorista           AS PrecioMayorista,
            sp.cantidadMinimaMayorista   AS CantidadMinimaMayorista,
            sp.enPromocion               AS EnPromocion,
            sp.porcentajeDescuento       AS PorcentajeDescuento,
            sp.usuarioId                 AS UsuarioId,
            sp.ubicacionTienda           AS UbicacionTienda,
            (SELECT MIN(il.fechaVencimiento) FROM inventario_lote il
                WHERE il.sucursalProductoID = sp.sucursalProductoID
                AND il.saldoCantidad > 0
                AND il.fechaVencimiento IS NOT NULL
                AND il.estado = 1) AS ProximoVencimiento
        FROM producto p
        INNER JOIN categoria c
            ON c.categoriaID = p.categoriaID
        INNER JOIN sucursalproducto sp
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
                producto.Categoria = categoria;
                producto.SucursalProducto = sucursalProducto;
                return producto;
            },
            new { SucursalId = sucursalId },
            transaction: _transaction,
            splitOn: "CategoriaId,SucursalProductoId"
        );

        return productos;
    }

    public async Task<IEnumerable<Producto>> GetAllProductosBaseRucAsync(string empresaRuc)
    {
        var sql = @"
            SELECT DISTINCT
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
                p.codigoBarras      AS CodigoBarras,
                p.esPaquete         AS EsPaquete,
                p.productoBaseId    AS ProductoBaseId,
                p.factorConversion  AS FactorConversion,
                p.urlImagenProducto AS UrlImagenProducto,

                c.categoriaID       AS CategoriaId,
                c.categoriaNombre   AS CategoriaNombre
            FROM producto p
            INNER JOIN categoria c ON c.categoriaID = p.categoriaID
            INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
            INNER JOIN sucursal s ON s.sucursalID = sp.sucursalID
            WHERE p.estado = 1
            AND sp.estado = 1
            AND s.estado = 1
            AND s.empresaRuc = @EmpresaRuc
            ORDER BY p.productoID";

        var productos = await _connection.QueryAsync<Producto, Categoria, Producto>(
            sql,
            (producto, categoria) =>
            {
                producto.Categoria = categoria;
                return producto;
            },
            new { EmpresaRuc = empresaRuc },
            transaction: _transaction,
            splitOn: "CategoriaId"
        );

        return productos;
    }

    public async Task<Producto?> GetProductoByIdAsync(int productoId, int sucursalId) {
        var sql = @"
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
                p.codigoBarras      AS CodigoBarras,
                p.esPaquete         AS EsPaquete,
                p.productoBaseId    AS ProductoBaseId,
                p.factorConversion  AS FactorConversion,
                p.urlImagenProducto AS UrlImagenProducto,

                c.categoriaID       AS CategoriaId,
                c.categoriaNombre   AS CategoriaNombre,

                sp.sucursalProductoID AS SucursalProductoId,
                sp.precioUnitario     AS PrecioUnitario,
                sp.stock              AS Stock,
                sp.ultimoPrecioCompra AS UltimoPrecioCompra,
                sp.fechaUltimaCompra  AS FechaUltimaCompra,
                sp.precioMayorista           AS PrecioMayorista,
                sp.cantidadMinimaMayorista   AS CantidadMinimaMayorista,
                sp.enPromocion               AS EnPromocion,
                sp.porcentajeDescuento       AS PorcentajeDescuento,
                sp.usuarioId                 AS UsuarioId,
                sp.ubicacionTienda           AS UbicacionTienda,
                (SELECT MIN(il.fechaVencimiento) FROM inventario_lote il
                    WHERE il.sucursalProductoID = sp.sucursalProductoID
                    AND il.saldoCantidad > 0
                    AND il.fechaVencimiento IS NOT NULL
                    AND il.estado = 1) AS ProximoVencimiento,
                s.nombre              AS NomSucursal
            FROM producto p
            INNER JOIN categoria c ON c.categoriaID = p.categoriaID
            INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
            INNER JOIN sucursal s ON s.sucursalID = sp.sucursalID
            WHERE p.estado = 1
            AND sp.estado = 1
            AND p.productoID = @ProductoId
            AND sp.sucursalID = @SucursalId";

        var result = await _connection.QueryAsync<Producto, Categoria, SucursalProducto, Producto>(
            sql,
            (producto, categoria, sucursalProducto) =>
            {
                producto.Categoria = categoria;
                producto.SucursalProducto = sucursalProducto;
                return producto;
            },
            new { ProductoId = productoId, SucursalId = sucursalId },
            transaction: _transaction,
            splitOn: "CategoriaId,SucursalProductoId"
        );

        return result.FirstOrDefault();
    }

    public async Task<IEnumerable<Producto>> SearchBySucursalAsync(int sucursalId, string palabra)
    {
        var sql = $@"{SelectColumns}
        AND sp.sucursalID = @SucursalId
        AND (p.nomProducto LIKE @Palabra OR p.codigo LIKE @Palabra OR p.codigoBarras = @PalabraExacta)
        ORDER BY p.nomProducto ASC
        LIMIT 10";

        var productos = await _connection.QueryAsync<Producto, Categoria, SucursalProducto, Producto>(
            sql,
            (producto, categoria, sucursalProducto) =>
            {
                producto.Categoria = categoria;
                producto.SucursalProducto = sucursalProducto;
                return producto;
            },
            new { SucursalId = sucursalId, Palabra = $"%{palabra}%", PalabraExacta = palabra },
            transaction: _transaction,
            splitOn: "CategoriaId,SucursalProductoId"
        );

        return productos;
    }

    public async Task<IEnumerable<Producto>> SearchByRucAsync(string empresaRuc, string palabra)
    {
        var sql = @"
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
            p.codigoBarras      AS CodigoBarras,
            p.esPaquete         AS EsPaquete,
            p.productoBaseId    AS ProductoBaseId,
            p.factorConversion  AS FactorConversion,
            p.urlImagenProducto AS UrlImagenProducto,

            c.categoriaID       AS CategoriaId,
            c.categoriaNombre   AS CategoriaNombre,

            sp.sucursalProductoID AS SucursalProductoId,
            sp.precioUnitario     AS PrecioUnitario,
            sp.stock              AS Stock,
            sp.ultimoPrecioCompra AS UltimoPrecioCompra,
            sp.fechaUltimaCompra  AS FechaUltimaCompra,
            sp.precioMayorista           AS PrecioMayorista,
            sp.cantidadMinimaMayorista   AS CantidadMinimaMayorista,
            sp.enPromocion               AS EnPromocion,
            sp.porcentajeDescuento       AS PorcentajeDescuento,
            sp.usuarioId                 AS UsuarioId,
            sp.ubicacionTienda           AS UbicacionTienda,
            (SELECT MIN(il.fechaVencimiento) FROM inventario_lote il
                WHERE il.sucursalProductoID = sp.sucursalProductoID
                AND il.saldoCantidad > 0
                AND il.fechaVencimiento IS NOT NULL
                AND il.estado = 1) AS ProximoVencimiento
        FROM producto p
        INNER JOIN categoria c ON c.categoriaID = p.categoriaID
        INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
        INNER JOIN sucursal s ON s.sucursalID = sp.sucursalID
        WHERE p.estado = 1
          AND sp.estado = 1
          AND s.estado = 1
          AND s.empresaRuc = @EmpresaRuc
          AND (p.nomProducto LIKE @Palabra OR p.codigo LIKE @Palabra OR p.codigoBarras = @PalabraExacta)
        ORDER BY p.nomProducto ASC
        LIMIT 10";

        var productos = await _connection.QueryAsync<Producto, Categoria, SucursalProducto, Producto>(
            sql,
            (producto, categoria, sucursalProducto) =>
            {
                producto.Categoria = categoria;
                producto.SucursalProducto = sucursalProducto;
                return producto;
            },
            new { EmpresaRuc = empresaRuc, Palabra = $"%{palabra}%", PalabraExacta = palabra },
            transaction: _transaction,
            splitOn: "CategoriaId,SucursalProductoId"
        );

        return productos;
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
                categoriaID, urlImagenProducto, codigoBarras, esPaquete, productoBaseId, factorConversion
            ) VALUES (
                @Codigo, @TipoProducto, @CodigoSunat, @NomProducto,
                @UnidadMedida, @TipoAfectacionIGV, @IncluirIGV,
                @CategoriaId, @UrlImagenProducto, @CodigoBarras, @EsPaquete, @ProductoBaseId, @FactorConversion
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
            producto.UrlImagenProducto,
            producto.CodigoBarras,
            producto.EsPaquete,
            producto.ProductoBaseId,
            producto.FactorConversion,
        }, _transaction);
        producto.ProductoId = newId;
        return producto;
    }

    public async Task<SucursalProducto> RegistrarSucursalProductoAsync(SucursalProducto sucursalProducto)
    {
        var sql = @"
            INSERT INTO sucursalproducto (
                productoID, sucursalID, precioUnitario, stock,
                precioMayorista, cantidadMinimaMayorista, enPromocion, porcentajeDescuento,
                usuarioId, ubicacionTienda,
                estado, fechaCreacion
            ) VALUES (
                @ProductoId, @SucursalId, @PrecioUnitario, @Stock,
                @PrecioMayorista, @CantidadMinimaMayorista, @EnPromocion, @PorcentajeDescuento,
                @UsuarioId, @UbicacionTienda,
                @Estado, @FechaCreacion
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
                categoriaID       = @CategoriaId,
                urlImagenProducto = @UrlImagenProducto,
                codigoBarras      = @CodigoBarras,
                esPaquete         = @EsPaquete,
                productoBaseId    = @ProductoBaseId,
                factorConversion  = @FactorConversion
            WHERE productoID = @ProductoId AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, producto, _transaction);
        return filas > 0;
    }

    public async Task<bool> EditarSucursalProductoAsync(SucursalProducto sucursalProducto)
    {
        var sql = @"
            UPDATE sucursalproducto SET
                precioUnitario          = @PrecioUnitario,
                stock                   = @Stock,
                precioMayorista         = @PrecioMayorista,
                cantidadMinimaMayorista = @CantidadMinimaMayorista,
                enPromocion             = @EnPromocion,
                porcentajeDescuento     = @PorcentajeDescuento,
                usuarioId               = @UsuarioId,
                ubicacionTienda         = @UbicacionTienda
            WHERE sucursalProductoID = @SucursalProductoId AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, sucursalProducto, _transaction);
        return filas > 0;
    }

    public async Task<bool> ActualizarStockAsync(int sucursalProductoId, int cantidad)
    {
        var sql = @"UPDATE sucursalproducto 
                    SET stock = stock - @Cantidad
                    WHERE sucursalProductoID = @SucursalProductoId 
                    AND estado = 1
                    AND stock >= @Cantidad";

        var filas = await _connection.ExecuteAsync(sql, new { SucursalProductoId = sucursalProductoId, Cantidad = cantidad }, _transaction);
        return filas > 0;
    }

    public async Task<bool> DevolverStockAsync(int productoId, int sucursalId, int cantidad)
    {
        var sql = @"
            UPDATE sucursalproducto
            SET stock = stock + @Cantidad
            WHERE productoID  = @ProductoId
            AND sucursalID  = @SucursalId
            AND estado      = 1";

        var filas = await _connection.ExecuteAsync(sql, new { ProductoId = productoId, SucursalId = sucursalId, Cantidad = cantidad }, _transaction);
        return filas > 0;
    }
    
    public async Task<bool> EliminarSucursalProductoAsync(int sucursalProductoId)
    {
        var sql = @"UPDATE sucursalproducto SET estado = 0
                    WHERE sucursalProductoID = @SucursalProductoId AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, new { SucursalProductoId = sucursalProductoId }, _transaction);
        return filas > 0;
    }

    public async Task<bool> RegistrarCompraStockAsync(int productoId, int sucursalId, int cantidad, decimal precioCompra)
    {
        var sql = @"
            UPDATE sucursalproducto
            SET stock = stock + @Cantidad,
                ultimoPrecioCompra = @PrecioCompra,
                fechaUltimaCompra = NOW()
            WHERE productoID = @ProductoId
            AND sucursalID = @SucursalId
            AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, new { ProductoId = productoId, SucursalId = sucursalId, Cantidad = cantidad, PrecioCompra = precioCompra }, _transaction);
        return filas > 0;
    }

    public async Task<bool> IncrementarStockSinCostoAsync(int productoId, int sucursalId, int cantidad)
    {
        var sql = @"
            UPDATE sucursalproducto
            SET stock = stock + @Cantidad
            WHERE productoID = @ProductoId
            AND sucursalID = @SucursalId
            AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, new { ProductoId = productoId, SucursalId = sucursalId, Cantidad = cantidad }, _transaction);
        return filas > 0;
    }

    public async Task<bool> ActualizarCostoSinStockAsync(int productoId, int sucursalId, decimal precioCompra)
    {
        var sql = @"
            UPDATE sucursalproducto
            SET ultimoPrecioCompra = @PrecioCompra,
                fechaUltimaCompra = NOW()
            WHERE productoID = @ProductoId
            AND sucursalID = @SucursalId
            AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, new { ProductoId = productoId, SucursalId = sucursalId, PrecioCompra = precioCompra }, _transaction);
        return filas > 0;
    }

    public async Task<Producto?> GetInfoConversionBySucursalProductoIdAsync(int sucursalProductoId)
    {
        var sql = @"
            SELECT
                p.productoID        AS ProductoId,
                p.esPaquete         AS EsPaquete,
                p.productoBaseId    AS ProductoBaseId,
                p.factorConversion  AS FactorConversion,

                sp.sucursalProductoID AS SucursalProductoId,
                sp.sucursalID         AS SucursalId,
                sp.stock              AS Stock
            FROM producto p
            INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
            WHERE sp.sucursalProductoID = @SucursalProductoId
            AND p.estado = 1
            AND sp.estado = 1";

        var result = await _connection.QueryAsync<Producto, SucursalProducto, Producto>(
            sql,
            (producto, sucursalProducto) =>
            {
                producto.SucursalProducto = sucursalProducto;
                return producto;
            },
            new { SucursalProductoId = sucursalProductoId },
            transaction: _transaction,
            splitOn: "SucursalProductoId"
        );

        return result.FirstOrDefault();
    }

    public async Task<bool> DescontarStockBaseAsync(int productoBaseId, int sucursalId, int cantidad)
    {
        var sql = @"
            UPDATE sucursalproducto
            SET stock = stock - @Cantidad
            WHERE productoID = @ProductoBaseId
            AND sucursalID = @SucursalId
            AND estado = 1
            AND stock >= @Cantidad";

        var filas = await _connection.ExecuteAsync(sql, new { ProductoBaseId = productoBaseId, SucursalId = sucursalId, Cantidad = cantidad }, _transaction);
        return filas > 0;
    }

    public async Task<Producto?> ObtenerProductoPorCodigoAsync(string codigo)
    {
        var sql = "SELECT productoID AS ProductoId FROM producto WHERE codigo = @Codigo AND estado = 1";
        return await _connection.QueryFirstOrDefaultAsync<Producto>(sql, new { Codigo = codigo }, _transaction);
    }

    public async Task<bool> ExisteEnSucursalAsync(int productoId, int sucursalId)
    {
        var sql = @"SELECT COUNT(1) FROM sucursalproducto 
                    WHERE productoID = @ProductoId 
                    AND sucursalID = @SucursalId 
                    AND estado = 1";
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { ProductoId = productoId, SucursalId = sucursalId }, _transaction);
        return count > 0;
    }

    public async Task<IEnumerable<Producto>> GetProductosRucDisponiblesAsync(int sucursalId)
    {
        var sql = @"
            SELECT DISTINCT
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
                p.codigoBarras      AS CodigoBarras,
                p.esPaquete         AS EsPaquete,
                p.productoBaseId    AS ProductoBaseId,
                p.factorConversion  AS FactorConversion,

                c.categoriaID       AS CategoriaId,
                c.categoriaNombre   AS CategoriaNombre
            FROM producto p
            INNER JOIN categoria c ON c.categoriaID = p.categoriaID
            INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
            INNER JOIN sucursal s ON s.sucursalID = sp.sucursalID
            WHERE p.estado = 1
            AND sp.estado = 1
            AND s.estado = 1
            AND s.empresaRuc = (
                SELECT empresaRuc FROM sucursal WHERE sucursalID = @SucursalId AND estado = 1
            )
            AND p.productoID NOT IN (
                SELECT productoID FROM sucursalproducto 
                WHERE sucursalID = @SucursalId 
                AND estado = 1
            )
            ORDER BY p.productoID";

        var productos = await _connection.QueryAsync<Producto, Categoria, Producto>(
            sql,
            (producto, categoria) =>
            {
                producto.Categoria = categoria;
                return producto;
            },
            new { SucursalId = sucursalId },
            transaction: _transaction,
            splitOn: "CategoriaId"
        );

        return productos;
    }

    public async Task<IEnumerable<Producto>> SearchProductosRucDisponiblesAsync(int sucursalId, string palabra)
    {
        var sql = @"
            SELECT DISTINCT
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
                p.codigoBarras      AS CodigoBarras,
                p.esPaquete         AS EsPaquete,
                p.productoBaseId    AS ProductoBaseId,
                p.factorConversion  AS FactorConversion,

                c.categoriaID       AS CategoriaId,
                c.categoriaNombre   AS CategoriaNombre
            FROM producto p
            INNER JOIN categoria c ON c.categoriaID = p.categoriaID
            INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
            INNER JOIN sucursal s ON s.sucursalID = sp.sucursalID
            WHERE p.estado = 1
            AND sp.estado = 1
            AND s.estado = 1
            AND s.empresaRuc = (
                SELECT empresaRuc FROM sucursal WHERE sucursalID = @SucursalId AND estado = 1
            )
            AND p.productoID NOT IN (
                SELECT productoID FROM sucursalproducto 
                WHERE sucursalID = @SucursalId AND estado = 1
            )
            AND (p.nomProducto LIKE @Palabra OR p.codigo LIKE @Palabra)
            ORDER BY p.nomProducto
            LIMIT 10";

        var productos = await _connection.QueryAsync<Producto, Categoria, Producto>(
            sql,
            (producto, categoria) =>
            {
                producto.Categoria = categoria;
                return producto;
            },
            new { SucursalId = sucursalId, Palabra = $"%{palabra}%" },
            transaction: _transaction,
            splitOn: "CategoriaId"
        );

        return productos;
    }
    
    public async Task<IEnumerable<Producto>> GetAllProductosRucAsync(string empresaRuc)
    {
        var sql = @"
            SELECT DISTINCT
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
                p.codigoBarras      AS CodigoBarras,
                p.esPaquete         AS EsPaquete,
                p.productoBaseId    AS ProductoBaseId,
                p.factorConversion  AS FactorConversion,

                c.categoriaID       AS CategoriaId,
                c.categoriaNombre   AS CategoriaNombre,

                sp.sucursalProductoID        AS SucursalProductoId,
                sp.precioUnitario            AS PrecioUnitario,
                sp.stock                     AS Stock,
                sp.ultimoPrecioCompra         AS UltimoPrecioCompra,
                sp.fechaUltimaCompra          AS FechaUltimaCompra,
                sp.precioMayorista           AS PrecioMayorista,
                sp.cantidadMinimaMayorista   AS CantidadMinimaMayorista,
                sp.enPromocion               AS EnPromocion,
                sp.porcentajeDescuento       AS PorcentajeDescuento,
                sp.usuarioId                 AS UsuarioId,
                sp.ubicacionTienda           AS UbicacionTienda,
                (SELECT MIN(il.fechaVencimiento) FROM inventario_lote il
                    WHERE il.sucursalProductoID = sp.sucursalProductoID
                    AND il.saldoCantidad > 0
                    AND il.fechaVencimiento IS NOT NULL
                    AND il.estado = 1) AS ProximoVencimiento,

                s.nombre            AS NomSucursal
            FROM producto p
            INNER JOIN categoria c ON c.categoriaID = p.categoriaID
            INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
            INNER JOIN sucursal s ON s.sucursalID = sp.sucursalID
            WHERE p.estado = 1
            AND sp.estado = 1
            AND s.estado = 1
            AND s.empresaRuc = @EmpresaRuc
            ORDER BY p.productoID";

        var productos = await _connection.QueryAsync<Producto, Categoria, SucursalProducto, Producto>(
            sql,
            (producto, categoria, sucursalProducto) =>
            {
                producto.Categoria = categoria;
                producto.SucursalProducto = sucursalProducto;
                return producto;
            },
            new { EmpresaRuc = empresaRuc },
            transaction: _transaction,
            splitOn: "CategoriaId,SucursalProductoId"
        );

        return productos;
    }

    public async Task<IEnumerable<ReporteProductoItemDTO>> GetReporteProductosAsync(ReporteProductoFiltroDTO filtro)
    {
        var sql = new System.Text.StringBuilder(@"
            SELECT
                p.codigo                AS Codigo,
                p.nomProducto           AS NomProducto,
                c.categoriaNombre       AS CategoriaNombre,
                p.tipoProducto          AS TipoProducto,
                p.unidadMedida          AS UnidadMedida,
                p.tipoAfectacionIGV     AS TipoAfectacionIGV,
                p.incluirIGV            AS IncluirIGV,
                s.nombre                AS NomSucursal,
                sp.precioUnitario       AS PrecioUnitario,
                sp.stock                AS Stock
            FROM producto p
            INNER JOIN categoria c   ON c.categoriaID  = p.categoriaID
            INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
            INNER JOIN sucursal s    ON s.sucursalID   = sp.sucursalID
            WHERE p.estado  = 1
            AND sp.estado = 1
            AND s.estado  = 1
            AND s.empresaRuc = @EmpresaRuc");
    
        var parameters = new DynamicParameters();
        parameters.Add("EmpresaRuc", filtro.EmpresaRuc);
    
        // Filtro sucursal
        if (filtro.SucursalId.HasValue)
        {
            sql.Append(" AND sp.sucursalID = @SucursalId");
            parameters.Add("SucursalId", filtro.SucursalId.Value);
        }
    
        // Filtro categoría
        if (filtro.CategoriaId.HasValue)
        {
            sql.Append(" AND p.categoriaID = @CategoriaId");
            parameters.Add("CategoriaId", filtro.CategoriaId.Value);
        }
    
        // Filtro tipo afectación IGV (10=Gravado, 20=Exonerado, 30=Inafecto)
        if (!string.IsNullOrWhiteSpace(filtro.IgvTipo))
        {
            sql.Append(" AND p.tipoAfectacionIGV = @IgvTipo");
            parameters.Add("IgvTipo", filtro.IgvTipo);
        }
    
        // Filtro tipo producto (Bien / Servicio)
        if (!string.IsNullOrWhiteSpace(filtro.TipoProducto))
        {
            sql.Append(" AND p.tipoProducto = @TipoProducto");
            parameters.Add("TipoProducto", filtro.TipoProducto);
        }
    
        // Filtro stock
        switch (filtro.StockFiltro?.ToLower())
        {
            case "sin_stock":
                sql.Append(" AND sp.stock = 0");
                break;
            case "con_stock":
                sql.Append(" AND sp.stock > 0");
                break;
            case "menor_a" when filtro.StockValor.HasValue:
                sql.Append(" AND sp.stock < @StockValor");
                parameters.Add("StockValor", filtro.StockValor.Value);
                break;
        }
    
        sql.Append(" ORDER BY s.nombre, p.nomProducto");
    
        return await _connection.QueryAsync<ReporteProductoItemDTO>(
            sql.ToString(),
            parameters,
            transaction: _transaction
        );
    }

    public async Task<bool> ExisteCodigoBarrasAsync(string codigoBarras)
    {
        var sql = @"SELECT COUNT(1) FROM producto
                    WHERE codigoBarras = @CodigoBarras
                    AND codigoBarras != ''
                    AND estado = 1";
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { CodigoBarras = codigoBarras }, _transaction);
        return count > 0;
    }

}