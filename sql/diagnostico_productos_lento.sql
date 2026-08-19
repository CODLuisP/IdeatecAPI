-- =====================================================================
-- Diagnóstico: ¿dónde se van los ~880 ms de GET /api/productos/{sucursalId}?
--
-- No es un script de migración: son mediciones, todas de solo lectura.
-- Usa el profiler de MySQL, que reporta el tiempo DENTRO del servidor, para
-- poder separar "ejecutar la consulta" de "traer los datos por la red".
--
-- La salida es grande (Q3 devuelve ~1000 filas). Redirigirla a un archivo:
--
--   mysql -h ... -D ideafactudemo < sql/diagnostico_productos_lento.sql > salida.txt
--
-- Lo que importa está al final, en SHOW PROFILES:
--
--   Q1  SELECT 1              -> piso: la consulta más barata posible.
--   Q2  agregada CON subconsulta   -> ejecución completa, sin transferir filas.
--   Q3  la consulta real          -> ejecución + envío de las ~1000 filas.
--   Q4  agregada SIN subconsulta   -> lo mismo que Q2 pero sin vencimientos.
--
--   Q2 - Q4  = lo que cuesta la subconsulta de vencimiento.
--   Q3 - Q2  = lo que cuesta mandar los datos.
--   880 ms (Postman) - Q3 = lo que se va fuera de MySQL (red hasta el cliente,
--                           Dapper, serialización JSON).
-- =====================================================================

SET @sucursal := 11;
SET profiling = 1;
SET profiling_history_size = 20;

-- Q1 · Piso: ida y vuelta sin trabajo real.
SELECT 1 AS ping;

-- Q2 · Ejecución completa. El COUNT/SUM de afuera obliga a MySQL a resolver
--      todas las filas y la subconsulta, pero solo devuelve 1 fila: el tiempo
--      es ejecución pura, sin transferencia.
SELECT COUNT(*) AS filas, SUM(LENGTH(sub.NomProducto)) AS peso
FROM (
    SELECT
        p.nomProducto AS NomProducto,
        (SELECT MIN(il.fechaVencimiento) FROM inventario_lote il
            WHERE il.sucursalProductoID = sp.sucursalProductoID
            AND il.saldoCantidad > 0
            AND il.fechaVencimiento IS NOT NULL
            AND il.estado = 1) AS ProximoVencimiento
    FROM producto p
    INNER JOIN categoria c ON c.categoriaID = p.categoriaID
    INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
    WHERE p.estado = 1 AND sp.estado = 1 AND sp.sucursalID = @sucursal
) sub;

-- Q3 · La consulta real del endpoint, con todas sus columnas y sus ~1000 filas.
SELECT
    p.productoID, p.codigo, p.tipoProducto, p.codigoSunat, p.nomProducto,
    p.unidadMedida, p.tipoAfectacionIGV, p.incluirIGV, p.estado,
    p.fechaCreacion, p.codigoBarras, p.esPaquete, p.productoBaseId,
    p.factorConversion, p.urlImagenProducto,
    c.categoriaID, c.categoriaNombre,
    sp.sucursalProductoID, sp.precioUnitario, sp.stock, sp.ultimoPrecioCompra,
    sp.fechaUltimaCompra, sp.precioMayorista, sp.cantidadMinimaMayorista,
    sp.enPromocion, sp.porcentajeDescuento, sp.usuarioId, sp.ubicacionTienda,
    sp.alertaVencimientoActiva, sp.alertaStockBajoActiva, sp.stockMinimoAlerta,
    (SELECT MIN(il.fechaVencimiento) FROM inventario_lote il
        WHERE il.sucursalProductoID = sp.sucursalProductoID
        AND il.saldoCantidad > 0
        AND il.fechaVencimiento IS NOT NULL
        AND il.estado = 1) AS ProximoVencimiento
FROM producto p
INNER JOIN categoria c ON c.categoriaID = p.categoriaID
INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
WHERE p.estado = 1 AND sp.estado = 1 AND sp.sucursalID = @sucursal
ORDER BY p.productoID;

-- Q4 · Igual que Q2 pero SIN la subconsulta de vencimiento.
SELECT COUNT(*) AS filas, SUM(LENGTH(sub.NomProducto)) AS peso
FROM (
    SELECT p.nomProducto AS NomProducto
    FROM producto p
    INNER JOIN categoria c ON c.categoriaID = p.categoriaID
    INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
    WHERE p.estado = 1 AND sp.estado = 1 AND sp.sucursalID = @sucursal
) sub;

SET profiling = 0;

SELECT '===== TIEMPOS DENTRO DEL SERVIDOR =====' AS ' ';
SHOW PROFILES;

SELECT '===== VOLUMEN REAL DE LA RESPUESTA =====' AS ' ';
SELECT
    COUNT(*) AS filas,
    ROUND(SUM(
        LENGTH(COALESCE(p.nomProducto,'')) + LENGTH(COALESCE(p.urlImagenProducto,'')) +
        LENGTH(COALESCE(p.codigo,'')) + LENGTH(COALESCE(p.codigoBarras,'')) +
        LENGTH(COALESCE(p.codigoSunat,'')) + LENGTH(COALESCE(c.categoriaNombre,'')) +
        LENGTH(COALESCE(sp.ubicacionTienda,'')) + 120
    ) / 1024) AS kb_aprox
FROM producto p
INNER JOIN categoria c ON c.categoriaID = p.categoriaID
INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
WHERE p.estado = 1 AND sp.estado = 1 AND sp.sucursalID = @sucursal;

SELECT '===== PLAN DE EJECUCION =====' AS ' ';
EXPLAIN
SELECT p.productoID,
    (SELECT MIN(il.fechaVencimiento) FROM inventario_lote il
        WHERE il.sucursalProductoID = sp.sucursalProductoID
        AND il.saldoCantidad > 0
        AND il.fechaVencimiento IS NOT NULL
        AND il.estado = 1) AS ProximoVencimiento
FROM producto p
INNER JOIN categoria c ON c.categoriaID = p.categoriaID
INNER JOIN sucursalproducto sp ON sp.productoID = p.productoID
WHERE p.estado = 1 AND sp.estado = 1 AND sp.sucursalID = @sucursal
ORDER BY p.productoID;

SELECT '===== INDICES ACTUALES =====' AS ' ';
SHOW INDEX FROM inventario_lote;
SHOW INDEX FROM sucursalproducto;
