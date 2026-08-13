-- =====================================================================
-- Alertas configurables por producto (stock bajo y vencimiento), en vez de
-- los umbrales fijos que estaban quemados en el frontend. Aplicar
-- manualmente contra la base de datos MySQL (no hay EF Migrations en este
-- proyecto).
-- =====================================================================

ALTER TABLE sucursalproducto
    ADD COLUMN alertavencimientoactiva TINYINT(1) NULL,
    ADD COLUMN alertastockbajoactiva   TINYINT(1) NULL,
    ADD COLUMN stockminimoalerta       DECIMAL(10,2) NULL;

-- Días de anticipación (antes de la fecha de vencimiento) para marcar un
-- lote como "próximo a vencer". Configurable por empresa (antes fijo en 30).
ALTER TABLE configuracion
    ADD COLUMN diasalertavencimiento INT NULL;
