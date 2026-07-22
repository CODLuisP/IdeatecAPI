-- =====================================================================
-- El número de WhatsApp para aviso de stock bajo pasa de ser una
-- configuración por empresa a ser una configuración por sucursal (cada
-- sucursal puede avisar a un número distinto). El umbral de unidades
-- se mantiene a nivel de empresa (configuracion.umbralstockbajo).
--
-- Aplicar manualmente contra la base de datos MySQL (no hay EF
-- Migrations en este proyecto).
--
-- Nota: la columna configuracion.numerostockbajo NO se elimina (para
-- evitar una operación destructiva); simplemente el backend deja de
-- leerla/escribirla. Si quieres migrar el dato existente al nuevo
-- campo por sucursal, hazlo manualmente según tu caso (ej. copiarlo a
-- todas las sucursales de esa empresa) antes de que los usuarios lo
-- reconfiguren.
-- =====================================================================

ALTER TABLE sucursal
    ADD COLUMN numerostockbajo VARCHAR(20) NULL;
