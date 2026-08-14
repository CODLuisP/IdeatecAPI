-- =====================================================================
-- Segundo logo de empresa: uno para comprobantes (logoBase64, ya existía)
-- y otro específico para los documentos PDF (logoPdfBase64). Si el segundo
-- no está configurado, el backend usa logoBase64 como respaldo (ver
-- Empresa.LogoPdfEfectivo). Aplicar manualmente contra la base de datos
-- MySQL (no hay EF Migrations en este proyecto).
-- =====================================================================

ALTER TABLE empresa
    ADD COLUMN logoPdfBase64 LONGTEXT NULL;
