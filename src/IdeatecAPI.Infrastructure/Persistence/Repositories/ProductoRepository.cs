using System.Data;
using System.Linq;
using Dapper;
using MySqlConnector;
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

    // Quita tildes/diéresis y pasa a minúsculas, para que la búsqueda no distinga mayúsculas
    // ni acentos. Debe reflejar exactamente el mismo mapeo que ExpresionColumnaNormalizada,
    // para que el token buscado en C# coincida con lo que MySQL normaliza en la columna.
    private static string NormalizarTexto(string texto) => texto
        .ToLowerInvariant()
        .Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u')
        .Replace('ü', 'u').Replace('ñ', 'n');

    private static string ExpresionColumnaNormalizada(string columna) =>
        $"LOWER(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE({columna}, 'á','a'), 'é','e'), 'í','i'), 'ó','o'), 'ú','u'), 'ü','u'), 'ñ','n'))";

    // Búsqueda "difusa" por nombre: exige que todas las palabras de la búsqueda aparezcan en
    // el nombre (en cualquier orden, como substrings), ignorando mayúsculas y tildes. Así
    // "Alacena Mayonesa" o "MAYONESA 250" encuentran "Mayonesa Alacena 250 g".
    private static (string Sql, DynamicParameters Parametros) ConstruirCondicionNombre(string palabra, string columna, string prefijoParametro)
    {
        var tokens = NormalizarTexto(palabra).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var parametros = new DynamicParameters();

        if (tokens.Length == 0)
            return ("1 = 0", parametros);

        var expresion = ExpresionColumnaNormalizada(columna);
        var condiciones = new List<string>();

        for (var i = 0; i < tokens.Length; i++)
        {
            var nombreParametro = $"{prefijoParametro}{i}";
            condiciones.Add($"{expresion} LIKE @{nombreParametro}");
            parametros.Add(nombreParametro, $"%{tokens[i]}%");
        }

        return (string.Join(" AND ", condiciones), parametros);
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
            sp.alertaVencimientoActiva   AS AlertaVencimientoActiva,
            sp.alertaStockBajoActiva     AS AlertaStockBajoActiva,
            sp.stockMinimoAlerta         AS StockMinimoAlerta,
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

    // El catalogo completo se trae en varios lotes paralelos, cada uno por su
    // propia conexion. El motivo NO es la base (ejecuta en ~19 ms) ni Dapper
    // (cuesta 0 sobre el lector crudo): es el arranque lento de TCP. La conexion
    // que entrega el pool lleva rato ociosa, perdio su ventana de congestion y
    // vuelve a crecer desde cero, asi que traer ~1 MB cuesta ~690 ms en vez de
    // los ~200 ms que cuesta por una conexion caliente.
    //
    // Ese arranque es POR CONEXION: varias ramificando a la vez suman ventana
    // mas rapido. Medido contra sucursal 18 (1249 productos), desde conexion
    // fria: 1 consulta 690 ms | 2 lotes 521 ms | 3 lotes 352 ms | 4 lotes 369 ms
    // | 6 lotes 364 ms. De 3 en adelante no baja: ahi manda el RTT.
    //
    // Cuesta 3 conexiones del pool por request en vez de 1 (ver MaximumPoolSize).
    private const int LotesCatalogo = 3;

    public async Task<IEnumerable<Producto>> GetAllProductosAsync(int sucursalId)
    {
        // Dentro de una transaccion no se puede repartir: las otras conexiones
        // no la verian. Tampoco tiene sentido si el driver no es MySqlConnector.
        if (_transaction != null || _connection is not MySqlConnection plantilla)
            return await LeerLoteCatalogoAsync(_connection, _transaction, sucursalId, null);

        var tareas = Enumerable.Range(0, LotesCatalogo).Select(async lote =>
        {
            // Clone() conserva la cadena completa (con credenciales) y toma otra
            // conexion del mismo pool.
            await using var conexion = plantilla.Clone();
            await conexion.OpenAsync();
            return await LeerLoteCatalogoAsync(conexion, null, sucursalId, lote);
        }).ToArray();

        var partes = await Task.WhenAll(tareas);

        // Los lotes vuelven entremezclados; se restaura el orden por productoID
        // que traia la consulta original.
        return partes.SelectMany(p => p).OrderBy(p => p.ProductoId).ToList();
    }

    private static async Task<IEnumerable<Producto>> LeerLoteCatalogoAsync(
        IDbConnection conexion, IDbTransaction? transaccion, int sucursalId, int? lote)
    {
        // Repartir por el resto de la division reparte parejo y no depende de
        // rangos de id, que quedan con huecos al dar de baja productos.
        var filtroLote = lote is null
            ? string.Empty
            : $" AND (sp.sucursalProductoID % {LotesCatalogo}) = {lote}";

        var sql = $"{SelectColumns} AND sp.sucursalID = @SucursalId{filtroLote} ORDER BY p.productoID";

        return await conexion.QueryAsync<Producto, Categoria, SucursalProducto, Producto>(
            sql,
            (producto, categoria, sucursalProducto) =>
            {
                producto.Categoria = categoria;
                producto.SucursalProducto = sucursalProducto;
                return producto;
            },
            new { SucursalId = sucursalId },
            transaction: transaccion,
            splitOn: "CategoriaId,SucursalProductoId"
        );
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
            sp.alertaVencimientoActiva   AS AlertaVencimientoActiva,
            sp.alertaStockBajoActiva     AS AlertaStockBajoActiva,
            sp.stockMinimoAlerta         AS StockMinimoAlerta,
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
        var (condicionNombre, parametrosNombre) = ConstruirCondicionNombre(palabra, "p.nomProducto", "Nombre");

        var sql = $@"{SelectColumns}
        AND sp.sucursalID = @SucursalId
        AND (({condicionNombre}) OR p.codigo LIKE @Palabra OR p.codigoBarras = @PalabraExacta)
        ORDER BY p.nomProducto ASC
        LIMIT 10";

        var parametros = new DynamicParameters();
        parametros.AddDynamicParams(parametrosNombre);
        parametros.Add("SucursalId", sucursalId);
        parametros.Add("Palabra", $"%{palabra}%");
        parametros.Add("PalabraExacta", palabra);

        var productos = await _connection.QueryAsync<Producto, Categoria, SucursalProducto, Producto>(
            sql,
            (producto, categoria, sucursalProducto) =>
            {
                producto.Categoria = categoria;
                producto.SucursalProducto = sucursalProducto;
                return producto;
            },
            parametros,
            transaction: _transaction,
            splitOn: "CategoriaId,SucursalProductoId"
        );

        return productos;
    }

    public async Task<IEnumerable<Producto>> SearchByRucAsync(string empresaRuc, string palabra)
    {
        var (condicionNombreRuc, parametrosNombreRuc) = ConstruirCondicionNombre(palabra, "p.nomProducto", "Nombre");

        var sql = $@"
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
            sp.alertaVencimientoActiva   AS AlertaVencimientoActiva,
            sp.alertaStockBajoActiva     AS AlertaStockBajoActiva,
            sp.stockMinimoAlerta         AS StockMinimoAlerta,
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
          AND (({condicionNombreRuc}) OR p.codigo LIKE @Palabra OR p.codigoBarras = @PalabraExacta)
        ORDER BY p.nomProducto ASC
        LIMIT 10";

        var parametrosRuc = new DynamicParameters();
        parametrosRuc.AddDynamicParams(parametrosNombreRuc);
        parametrosRuc.Add("EmpresaRuc", empresaRuc);
        parametrosRuc.Add("Palabra", $"%{palabra}%");
        parametrosRuc.Add("PalabraExacta", palabra);

        var productos = await _connection.QueryAsync<Producto, Categoria, SucursalProducto, Producto>(
            sql,
            (producto, categoria, sucursalProducto) =>
            {
                producto.Categoria = categoria;
                producto.SucursalProducto = sucursalProducto;
                return producto;
            },
            parametrosRuc,
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
                alertaVencimientoActiva, alertaStockBajoActiva, stockMinimoAlerta,
                estado, fechaCreacion
            ) VALUES (
                @ProductoId, @SucursalId, @PrecioUnitario, @Stock,
                @PrecioMayorista, @CantidadMinimaMayorista, @EnPromocion, @PorcentajeDescuento,
                @UsuarioId, @UbicacionTienda,
                @AlertaVencimientoActiva, @AlertaStockBajoActiva, @StockMinimoAlerta,
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
                ubicacionTienda         = @UbicacionTienda,
                alertaVencimientoActiva = @AlertaVencimientoActiva,
                alertaStockBajoActiva   = @AlertaStockBajoActiva,
                stockMinimoAlerta       = @StockMinimoAlerta
            WHERE sucursalProductoID = @SucursalProductoId AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, sucursalProducto, _transaction);
        return filas > 0;
    }

    public async Task<bool> ActualizarStockAsync(int sucursalProductoId, decimal cantidad)
    {
        var sql = @"UPDATE sucursalproducto 
                    SET stock = stock - @Cantidad
                    WHERE sucursalProductoID = @SucursalProductoId 
                    AND estado = 1
                    AND stock >= @Cantidad";

        var filas = await _connection.ExecuteAsync(sql, new { SucursalProductoId = sucursalProductoId, Cantidad = cantidad }, _transaction);
        return filas > 0;
    }

    public async Task<IEnumerable<StockBloqueadoDTO>> GetStockParaDescontarAsync(IEnumerable<int> sucursalProductoIds)
    {
        var ids = sucursalProductoIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        // FOR UPDATE bloquea las filas de stock que la venta va a modificar, de modo que la
        // validacion en memoria y el UPDATE posterior sean atomicos frente a otra venta
        // simultanea del mismo producto.
        var sql = @"
            SELECT sucursalProductoID AS SucursalProductoId,
                   stock              AS Stock
            FROM sucursalproducto
            WHERE sucursalProductoID IN @SucursalProductoIds
            AND estado = 1
            FOR UPDATE";

        return await _connection.QueryAsync<StockBloqueadoDTO>(sql, new { SucursalProductoIds = ids }, _transaction);
    }

    public Task<int> DescontarStockBatchAsync(IReadOnlyDictionary<int, decimal> descuentosPorSucursalProducto) =>
        RestarEnLoteAsync("sucursalproducto", "sucursalProductoID", "stock", descuentosPorSucursalProducto, "estado = 1");

    public async Task<bool> DevolverStockAsync(int productoId, int sucursalId, decimal cantidad)
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

    public async Task<bool> RegistrarCompraStockAsync(int productoId, int sucursalId, decimal cantidad, decimal precioCompra)
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

    public async Task<bool> IncrementarStockSinCostoAsync(int productoId, int sucursalId, decimal cantidad)
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

    public async Task<IEnumerable<InfoConversionStockDTO>> GetInfoConversionBySucursalProductoIdsAsync(
        IEnumerable<int> sucursalProductoIds)
    {
        var ids = sucursalProductoIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        // El LEFT JOIN resuelve de una vez el sucursalProductoID del producto base dentro de
        // la misma sucursal, que antes costaba una consulta extra por cada paquete vendido.
        var sql = @"
            SELECT
                sp.sucursalProductoID  AS SucursalProductoId,
                p.productoID           AS ProductoId,
                sp.sucursalID          AS SucursalId,
                p.esPaquete            AS EsPaquete,
                p.productoBaseId       AS ProductoBaseId,
                p.factorConversion     AS FactorConversion,
                spb.sucursalProductoID AS BaseSucursalProductoId,
                sp.stock               AS Stock,
                spb.stock              AS BaseStock
            FROM producto p
            INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
            LEFT JOIN sucursalproducto spb
                   ON spb.productoID = p.productoBaseId
                  AND spb.sucursalID = sp.sucursalID
                  AND spb.estado = 1
            WHERE sp.sucursalProductoID IN @SucursalProductoIds
            AND p.estado = 1
            AND sp.estado = 1";

        return await _connection.QueryAsync<InfoConversionStockDTO>(sql, new { SucursalProductoIds = ids }, _transaction);
    }

    public async Task<bool> DescontarStockBaseAsync(int productoBaseId, int sucursalId, decimal cantidad)
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
        var (condicionNombreDisponibles, parametrosNombreDisponibles) = ConstruirCondicionNombre(palabra, "p.nomProducto", "Nombre");

        var sql = $@"
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
            AND (({condicionNombreDisponibles}) OR p.codigo LIKE @Palabra)
            ORDER BY p.nomProducto
            LIMIT 10";

        var parametrosDisponibles = new DynamicParameters();
        parametrosDisponibles.AddDynamicParams(parametrosNombreDisponibles);
        parametrosDisponibles.Add("SucursalId", sucursalId);
        parametrosDisponibles.Add("Palabra", $"%{palabra}%");

        var productos = await _connection.QueryAsync<Producto, Categoria, Producto>(
            sql,
            (producto, categoria) =>
            {
                producto.Categoria = categoria;
                return producto;
            },
            parametrosDisponibles,
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
            sp.alertaVencimientoActiva   AS AlertaVencimientoActiva,
            sp.alertaStockBajoActiva     AS AlertaStockBajoActiva,
            sp.stockMinimoAlerta         AS StockMinimoAlerta,
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
                p.codigoBarras          AS CodigoBarras,
                p.nomProducto           AS NomProducto,
                c.categoriaNombre       AS CategoriaNombre,
                p.tipoProducto          AS TipoProducto,
                p.unidadMedida          AS UnidadMedida,
                p.tipoAfectacionIGV     AS TipoAfectacionIGV,
                p.incluirIGV            AS IncluirIGV,
                s.nombre                AS NomSucursal,
                p.urlImagenProducto     AS UrlImagenProducto,
                sp.ultimoPrecioCompra   AS PrecioCompra,
                sp.precioUnitario       AS PrecioUnitario,
                -- Un paquete/caja no lleva stock propio: se deriva del producto base
                -- (unidades del base / factor de conversión), igual que en la lista de productos.
                CASE
                    WHEN p.esPaquete = 1
                     AND p.productoBaseId IS NOT NULL
                     AND IFNULL(p.factorConversion, 0) > 0
                    THEN FLOOR(IFNULL((SELECT spb.stock
                                       FROM sucursalproducto spb
                                       WHERE spb.productoID = p.productoBaseId
                                         AND spb.sucursalID = sp.sucursalID
                                         AND spb.estado = 1
                                       LIMIT 1), 0) / p.factorConversion)
                    ELSE sp.stock
                END                     AS Stock
            FROM producto p
            LEFT  JOIN categoria c   ON c.categoriaID  = p.categoriaID
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

    public async Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, int sucursalId)
    {
        var sql = @"SELECT COUNT(1) FROM producto p
                    INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID AND sp.estado = 1
                    INNER JOIN sucursal s ON s.sucursalID = sp.sucursalID AND s.estado = 1
                    WHERE p.codigoBarras = @CodigoBarras
                    AND p.codigoBarras != ''
                    AND p.estado = 1
                    AND s.empresaRuc = (
                        SELECT empresaRuc FROM sucursal WHERE sucursalID = @SucursalId AND estado = 1
                    )";
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { CodigoBarras = codigoBarras, SucursalId = sucursalId }, _transaction);
        return count > 0;
    }

}