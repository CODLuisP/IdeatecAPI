-- =====================================================================
-- Retiros de efectivo dentro de un turno de caja (ej. pago a un
-- proveedor en efectivo, préstamo, etc.). Se descuentan del efectivo
-- esperado del turno igual que las ventas lo suman. Aplicar manualmente
-- contra la base de datos MySQL (no hay EF Migrations en este proyecto).
-- =====================================================================

CREATE TABLE caja_retiro (
    cajaRetiroID   INT           NOT NULL AUTO_INCREMENT,
    cajaTurnoID    INT           NOT NULL,

    monto          DECIMAL(12,2) NOT NULL,
    motivo         VARCHAR(255)  NOT NULL,
    fechaRetiro    DATETIME      NOT NULL,

    usuarioID      INT           NOT NULL,
    nombreUsuario  VARCHAR(100)  NULL,

    PRIMARY KEY (cajaRetiroID),
    KEY idx_caja_retiro_turno (cajaTurnoID),
    CONSTRAINT fk_caja_retiro_turno FOREIGN KEY (cajaTurnoID)
        REFERENCES caja_turno (cajaTurnoID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
