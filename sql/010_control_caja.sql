-- =====================================================================
-- Módulo "Control de Caja" - apertura y cierre de caja por turnos de
-- usuario. Aplicar manualmente contra la base de datos MySQL (no hay
-- EF Migrations en este proyecto).
--
-- Modelo: una caja abierta a la vez por sucursal (caja_apertura); dentro
-- de ella se suceden los turnos de cada usuario (caja_turno). Al cuadrar
-- un turno se congela lo recaudado por medio de pago (caja_turno_detalle).
-- =====================================================================

-- Flag global que activa el módulo: barra en Nueva Venta, bloqueo de
-- emisión sin caja abierta y menú "Caja".
ALTER TABLE configuracion
    ADD COLUMN administracaja TINYINT(1) NULL;


-- ---------------------------------------------------------------------
-- Caja del día
-- ---------------------------------------------------------------------
CREATE TABLE caja_apertura (
    cajaAperturaID        INT           NOT NULL AUTO_INCREMENT,
    empresaRuc            VARCHAR(15)   NOT NULL,
    sucursalID            INT           NOT NULL,
    codEstablecimiento    VARCHAR(10)   NULL,

    montoInicial          DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    fechaApertura         DATETIME      NOT NULL,
    usuarioApertura       INT           NOT NULL,
    nombreUsuarioApertura VARCHAR(100)  NULL,

    fechaCierre           DATETIME      NULL,
    usuarioCierre         INT           NULL,
    nombreUsuarioCierre   VARCHAR(100)  NULL,

    -- Efectivo esperado vs. contado al cerrar la caja. diferencia negativa
    -- = faltante, positiva = sobrante.
    efectivoEsperado      DECIMAL(12,2) NULL,
    efectivoContado       DECIMAL(12,2) NULL,
    diferencia            DECIMAL(12,2) NULL,

    estado                VARCHAR(10)   NOT NULL DEFAULT 'ABIERTA',
    observaciones         VARCHAR(500)  NULL,

    PRIMARY KEY (cajaAperturaID),
    KEY idx_caja_apertura_sucursal_estado (sucursalID, estado),
    KEY idx_caja_apertura_ruc_fecha (empresaRuc, fechaApertura)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- ---------------------------------------------------------------------
-- Turno de un usuario dentro de una caja abierta
-- ---------------------------------------------------------------------
CREATE TABLE caja_turno (
    cajaTurnoID          INT           NOT NULL AUTO_INCREMENT,
    cajaAperturaID       INT           NOT NULL,

    usuarioID            INT           NOT NULL,
    nombreUsuario        VARCHAR(100)  NULL,

    fechaInicio          DATETIME      NOT NULL,
    fechaFin             DATETIME      NULL,

    -- Efectivo recibido al empezar el turno: el monto inicial de la caja
    -- para el primero, y lo contado por el turno previo para los demás.
    saldoInicial         DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    efectivoEsperado     DECIMAL(12,2) NULL,
    efectivoContado      DECIMAL(12,2) NULL,
    diferencia           DECIMAL(12,2) NULL,

    totalVentas          DECIMAL(12,2) NULL,
    cantidadComprobantes INT           NULL,

    estado               VARCHAR(10)   NOT NULL DEFAULT 'ABIERTO',
    -- Quién ejecutó el cuadre; difiere del dueño del turno cuando alguien
    -- olvidó cuadrar y otro usuario cierra su turno.
    cerradoPorUsuarioID  INT           NULL,
    -- True si este cuadre además cerró la caja del día.
    esCierreCaja         TINYINT(1)    NOT NULL DEFAULT 0,
    observaciones        VARCHAR(500)  NULL,

    PRIMARY KEY (cajaTurnoID),
    KEY idx_caja_turno_apertura_estado (cajaAperturaID, estado),
    KEY idx_caja_turno_usuario (usuarioID, fechaInicio),
    CONSTRAINT fk_caja_turno_apertura FOREIGN KEY (cajaAperturaID)
        REFERENCES caja_apertura (cajaAperturaID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


-- ---------------------------------------------------------------------
-- Recaudado por medio de pago en un turno, congelado al cuadrar para que
-- el histórico no cambie si luego se anula un comprobante.
-- ---------------------------------------------------------------------
CREATE TABLE caja_turno_detalle (
    cajaTurnoDetalleID INT           NOT NULL AUTO_INCREMENT,
    cajaTurnoID        INT           NOT NULL,

    medioPago          VARCHAR(30)   NOT NULL,
    montoEsperado      DECIMAL(12,2) NOT NULL DEFAULT 0.00,
    -- Solo se llena para Efectivo: el resto no se cuenta a mano.
    montoContado       DECIMAL(12,2) NULL,
    diferencia         DECIMAL(12,2) NULL,

    PRIMARY KEY (cajaTurnoDetalleID),
    KEY idx_caja_turno_detalle_turno (cajaTurnoID),
    CONSTRAINT fk_caja_turno_detalle_turno FOREIGN KEY (cajaTurnoID)
        REFERENCES caja_turno (cajaTurnoID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
