-- =====================================================================
-- Fecha de vencimiento por lote de compra. Se agrega a inventario_lote
-- (no a sucursalproducto) porque cada compra de un mismo producto puede
-- traer un vencimiento distinto — el dato pertenece al lote, no al
-- producto-sucursal agregado. Nullable, no afecta lotes existentes ni
-- requiere que el frontend la envíe. Aplicar manualmente contra la base
-- de datos MySQL (no hay EF Migrations en este proyecto).
-- =====================================================================

ALTER TABLE inventario_lote
    ADD COLUMN fechaVencimiento DATE NULL;
