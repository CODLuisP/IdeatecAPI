-- =====================================================================
-- PEPS/FIFO - Backfill de saldo inicial (correr UNA sola vez, tras aplicar
-- 001_inventario_peps.sql, y antes de activar el consumo FIFO en ventas).
--
-- Crea un lote 'SALDO_INICIAL' por cada SucursalProducto con stock > 0 que
-- todavía no tenga uno, usando ultimoPrecioCompra como costo de referencia.
-- Es idempotente: si se corre de nuevo, no duplica filas ya creadas.
-- =====================================================================

-- 1) Revisar ANTES de correr el backfill: productos con stock pero sin costo
--    conocido (ultimoPrecioCompra IS NULL). Para estos, el backfill los deja
--    con costoUnitario = 0, lo que subestima el COGS. Revisar y corregir el
--    costo manualmente (vía el endpoint POST api/inventario/saldo-inicial,
--    indicando el costo correcto) antes de asumir el backfill masivo como
--    definitivo para esos casos.
SELECT sp.sucursalProductoID, p.nomProducto, sp.stock, sp.ultimoPrecioCompra
FROM sucursalproducto sp
INNER JOIN producto p ON p.productoID = sp.productoID
WHERE sp.estado = 1
AND sp.stock > 0
AND sp.ultimoPrecioCompra IS NULL
AND NOT EXISTS (
    SELECT 1 FROM inventario_lote il
    WHERE il.sucursalProductoID = sp.sucursalProductoID
    AND il.origen = 'SALDO_INICIAL'
);

-- 2) Backfill masivo.
INSERT INTO inventario_lote
    (sucursalProductoID, compraProveedorID, origen, fechaLote, cantidadOriginal, costoUnitario, saldoCantidad, estado, fechaCreacion)
SELECT
    sp.sucursalProductoID,
    NULL,
    'SALDO_INICIAL',
    NOW(),
    sp.stock,
    COALESCE(sp.ultimoPrecioCompra, 0),
    sp.stock,
    1,
    NOW()
FROM sucursalproducto sp
WHERE sp.estado = 1
AND sp.stock > 0
AND NOT EXISTS (
    SELECT 1 FROM inventario_lote il
    WHERE il.sucursalProductoID = sp.sucursalProductoID
    AND il.origen = 'SALDO_INICIAL'
);

-- 3) Registrar el movimiento de Kardex correspondiente a cada lote recién creado.
INSERT INTO kardex_movimiento
    (sucursalProductoID, tipoMovimiento, referenciaTipo, referenciaID, cantidad,
     costoUnitarioPromedio, costoTotal, saldoCantidadPost, saldoValorPost, fechaMovimiento, idUsuario)
SELECT
    il.sucursalProductoID,
    'ENTRADA_SALDO_INICIAL',
    NULL,
    NULL,
    il.cantidadOriginal,
    il.costoUnitario,
    il.cantidadOriginal * il.costoUnitario,
    il.saldoCantidad,
    il.saldoCantidad * il.costoUnitario,
    il.fechaLote,
    NULL
FROM inventario_lote il
WHERE il.origen = 'SALDO_INICIAL'
AND NOT EXISTS (
    SELECT 1 FROM kardex_movimiento_lote kml
    WHERE kml.inventarioLoteID = il.inventarioLoteID
);

-- 4) Enlazar cada movimiento de saldo inicial recién insertado con su lote.
INSERT INTO kardex_movimiento_lote (kardexMovimientoID, inventarioLoteID, cantidad, costoUnitario)
SELECT km.kardexMovimientoID, il.inventarioLoteID, il.cantidadOriginal, il.costoUnitario
FROM inventario_lote il
INNER JOIN kardex_movimiento km
    ON km.sucursalProductoID = il.sucursalProductoID
    AND km.tipoMovimiento = 'ENTRADA_SALDO_INICIAL'
    AND km.cantidad = il.cantidadOriginal
WHERE il.origen = 'SALDO_INICIAL'
AND NOT EXISTS (
    SELECT 1 FROM kardex_movimiento_lote kml WHERE kml.inventarioLoteID = il.inventarioLoteID
);

-- 5) Verificación de reconciliación: debe devolver 0 filas si todo cuadra.
SELECT sp.sucursalProductoID, sp.stock AS stockCounter,
       COALESCE(SUM(il.saldoCantidad), 0) AS stockLotes
FROM sucursalproducto sp
LEFT JOIN inventario_lote il ON il.sucursalProductoID = sp.sucursalProductoID AND il.estado = 1
WHERE sp.estado = 1
GROUP BY sp.sucursalProductoID
HAVING stockCounter <> stockLotes;
