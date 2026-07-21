-- =====================================================================
-- Umbral de stock bajo personalizable - antes estaba fijo en 10 unidades
-- en el código. Aplicar manualmente contra la base de datos MySQL (no hay
-- EF Migrations en este proyecto).
-- =====================================================================

ALTER TABLE configuracion
    ADD COLUMN umbralstockbajo INT NULL;
