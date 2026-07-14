-- =====================================================================
-- Alerta de stock bajo por WhatsApp - agrega el número de aviso a la
-- configuración de empresa. Aplicar manualmente contra la base de datos
-- MySQL (no hay EF Migrations en este proyecto).
-- =====================================================================

ALTER TABLE configuracion
    ADD COLUMN numerostockbajo VARCHAR(20) NULL;
