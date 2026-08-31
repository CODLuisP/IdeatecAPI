-- Agrega la referencia a la línea de venta (comprobantedetalle) que originó cada
-- movimiento de kardex. Permite distinguir, en el reporte de rentabilidad, la venta
-- de un producto "paquete" (sixpack, caja, etc.) de la venta de su producto base,
-- en vez de que ambas queden fusionadas contra el producto base como hasta ahora.
--
-- Nullable a propósito: los movimientos existentes no tienen forma de reconstruir
-- a qué línea pertenecían, así que quedan en NULL y el reporte los sigue tratando
-- con la lógica anterior (ver InventarioLoteRepository.GetRentabilidadPorProductoAsync).
ALTER TABLE kardex_movimiento
  ADD COLUMN comprobanteDetalleID INT NULL AFTER referenciaID,
  ADD INDEX idx_kardex_comprobante_detalle (comprobanteDetalleID);
