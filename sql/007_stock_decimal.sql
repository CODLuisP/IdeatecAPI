-- =====================================================================
-- Venta y compra por peso / volumen (granel).
--
-- Hasta ahora el stock y las cantidades de compra eran ENTEROS, así que
-- era imposible vender 1.3 kg de arroz o 0.5 L de aceite: el endpoint
-- PUT /api/Comprobantes/{id}/descontar-stock devolvía 400 porque el
-- valor decimal no encajaba en `int Cantidad`, y la venta quedaba
-- registrada SIN descontar el inventario.
--
-- El núcleo del inventario (kardex_movimiento.cantidad,
-- inventario_lote.saldocantidad) YA era DECIMAL, así que este cambio
-- solo alinea las dos columnas que faltaban.
--
-- DECIMAL(18,3) admite hasta 3 decimales: suficiente para gramos
-- (0.001 kg) y mililitros (0.001 L).
--
-- Aplicar manualmente contra la base de datos MySQL (no hay EF
-- Migrations en este proyecto).
--
-- SEGURIDAD DEL CAMBIO: INT -> DECIMAL es una AMPLIACIÓN de tipo. Los
-- valores existentes se conservan (un stock de 9 pasa a 9.000); no hay
-- truncamiento ni pérdida de datos. Aun así, respalda la base antes de
-- ejecutarlo:
--     mysqldump -u USUARIO -p NOMBRE_BD > respaldo_antes_007.sql
-- =====================================================================

-- Stock por sucursal: permite saldos fraccionados (7.700 kg).
ALTER TABLE sucursalproducto
    MODIFY COLUMN stock DECIMAL(18,3) NULL;

-- Cantidad comprada al proveedor: permite comprar 30.500 kg.
ALTER TABLE compraproveedor
    MODIFY COLUMN cantidad DECIMAL(18,3) NULL;
