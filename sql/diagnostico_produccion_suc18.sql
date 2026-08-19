-- =====================================================================
-- Diagnostico contra PRODUCCION (ideafactu), sucursal 18.
--
-- El profiling anterior se hizo contra beta (ideafactudemo, sucursal 11) y
-- dio 13 ms. Pero /api/productos/18 tarda ~1 s mientras /api/productos/3
-- tarda 200 ms: el tiempo escala con la cantidad de productos, asi que hay
-- que medir la base y la sucursal reales.
--
-- Todas las consultas son de solo lectura.
--
--   mysql -h ... -D ideafactu < sql/diagnostico_produccion_suc18.sql
--
-- En SHOW PROFILES:
--   Q1  SELECT 1                  -> piso.
--   Q2  agregada CON subconsulta  -> ejecucion completa, sin transferir filas.
--   Q3  agregada SIN subconsulta  -> lo mismo sin la subconsulta de vencimiento.
--
--   Q2 - Q3 = lo que cuesta la subconsulta de vencimiento EN PRODUCCION.
-- =====================================================================

SET @sucursal := 18;
SET profiling = 1;

SELECT 1 AS ping;

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

SELECT '===== TAMANO DE LAS TABLAS =====' AS ' ';
SELECT
    (SELECT COUNT(*) FROM inventario_lote)                          AS lotes_total,
    (SELECT COUNT(*) FROM producto)                                 AS productos_total,
    (SELECT COUNT(*) FROM sucursalproducto)                         AS sucursalproducto_total,
    (SELECT COUNT(*) FROM sucursalproducto WHERE sucursalID = @sucursal) AS de_esta_sucursal;

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

SELECT '===== INDICES EN PRODUCCION =====' AS ' ';
SHOW INDEX FROM inventario_lote;
SHOW INDEX FROM sucursalproducto;
