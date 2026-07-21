-- =====================================================================
-- Módulo "Caja Autopago" - agrega el flag de activación a la
-- configuración de empresa. Aplicar manualmente contra la base de datos
-- MySQL (no hay EF Migrations en este proyecto).
-- =====================================================================

ALTER TABLE configuracion
    ADD COLUMN iscajaautopago TINYINT(1) NULL;
