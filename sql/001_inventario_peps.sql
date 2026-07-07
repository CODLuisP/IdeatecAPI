-- =====================================================================
-- PEPS/FIFO - Fase A: infraestructura de lotes e historial (Kardex)
-- Aplicar manualmente contra la base de datos MySQL (no hay EF Migrations
-- en este proyecto). No modifica ninguna tabla existente.
-- =====================================================================

-- 1) Lote de inventario: saldo restante por compra (o saldo inicial / devolución)
CREATE TABLE IF NOT EXISTS inventario_lote (
    inventarioLoteID   INT AUTO_INCREMENT PRIMARY KEY,
    sucursalProductoID INT NOT NULL,
    compraProveedorID  INT NULL,
    origen             VARCHAR(20) NOT NULL,   -- 'COMPRA' | 'SALDO_INICIAL' | 'DEVOLUCION_VENTA' | 'AJUSTE'
    fechaLote          DATETIME NOT NULL,
    cantidadOriginal   DECIMAL(12,3) NOT NULL,
    costoUnitario      DECIMAL(12,4) NOT NULL,
    saldoCantidad      DECIMAL(12,3) NOT NULL,
    estado             TINYINT(1) NOT NULL DEFAULT 1,
    fechaCreacion      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_lote_sucursalproducto FOREIGN KEY (sucursalProductoID) REFERENCES sucursalproducto(sucursalProductoID),
    CONSTRAINT fk_lote_compraproveedor  FOREIGN KEY (compraProveedorID)  REFERENCES compraproveedor(idCompraProveedor),
    INDEX idx_lote_fifo (sucursalProductoID, saldoCantidad, fechaLote, inventarioLoteID)
);

-- 2) Movimiento de Kardex: un registro por evento que afecta stock
CREATE TABLE IF NOT EXISTS kardex_movimiento (
    kardexMovimientoID    INT AUTO_INCREMENT PRIMARY KEY,
    sucursalProductoID    INT NOT NULL,
    tipoMovimiento        VARCHAR(30) NOT NULL,   -- 'ENTRADA_COMPRA' | 'ENTRADA_SALDO_INICIAL' | 'SALIDA_VENTA' | 'SALIDA_NOTA' | 'ENTRADA_DEVOLUCION' | 'AJUSTE'
    referenciaTipo        VARCHAR(20) NULL,       -- 'COMPROBANTE' | 'COMPRAPROVEEDOR' | 'NOTA'
    referenciaID           INT NULL,
    cantidad               DECIMAL(12,3) NOT NULL,
    costoUnitarioPromedio  DECIMAL(12,4) NULL,
    costoTotal             DECIMAL(12,4) NULL,
    saldoCantidadPost      DECIMAL(12,3) NOT NULL,
    saldoValorPost         DECIMAL(14,4) NOT NULL,
    fechaMovimiento        DATETIME NOT NULL,
    idUsuario              INT NULL,
    CONSTRAINT fk_kardex_sucursalproducto FOREIGN KEY (sucursalProductoID) REFERENCES sucursalproducto(sucursalProductoID),
    INDEX idx_kardex_sp_fecha (sucursalProductoID, fechaMovimiento)
);

-- 3) Detalle: qué lote(s) aportó/consumió cada movimiento
CREATE TABLE IF NOT EXISTS kardex_movimiento_lote (
    kardexMovimientoLoteID INT AUTO_INCREMENT PRIMARY KEY,
    kardexMovimientoID     INT NOT NULL,
    inventarioLoteID       INT NOT NULL,
    cantidad                DECIMAL(12,3) NOT NULL,
    costoUnitario           DECIMAL(12,4) NOT NULL,
    CONSTRAINT fk_kml_movimiento FOREIGN KEY (kardexMovimientoID) REFERENCES kardex_movimiento(kardexMovimientoID),
    CONSTRAINT fk_kml_lote       FOREIGN KEY (inventarioLoteID)   REFERENCES inventario_lote(inventarioLoteID),
    INDEX idx_kml_lote (inventarioLoteID)
);
