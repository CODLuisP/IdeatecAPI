namespace IdeatecAPI.Application.Features.Caja.DTOs;

/// <summary>Recaudado por un medio de pago dentro de un turno.</summary>
public class MedioPagoResumenDto
{
    public string MedioPago { get; set; } = string.Empty;
    public decimal MontoEsperado { get; set; }
    public decimal? MontoContado { get; set; }
    public decimal? Diferencia { get; set; }
}

public class CajaAperturaDto
{
    public int CajaAperturaId { get; set; }
    public string? EmpresaRuc { get; set; }
    public int SucursalId { get; set; }
    public string? NombreSucursal { get; set; }
    public decimal MontoInicial { get; set; }
    public DateTime FechaApertura { get; set; }
    public int UsuarioApertura { get; set; }
    public string? NombreUsuarioApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public int? UsuarioCierre { get; set; }
    public string? NombreUsuarioCierre { get; set; }
    public decimal? EfectivoEsperado { get; set; }
    public decimal? EfectivoContado { get; set; }
    public decimal? Diferencia { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

public class CajaTurnoDto
{
    public int CajaTurnoId { get; set; }
    public int CajaAperturaId { get; set; }
    public int UsuarioId { get; set; }
    public string? NombreUsuario { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal? EfectivoEsperado { get; set; }
    public decimal? EfectivoContado { get; set; }
    public decimal? Diferencia { get; set; }
    public decimal? TotalVentas { get; set; }
    public int? CantidadComprobantes { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool EsCierreCaja { get; set; }
    public string? Observaciones { get; set; }
    public List<MedioPagoResumenDto> Medios { get; set; } = new();
}

/// <summary>
/// Lo que el frontend necesita para decidir qué mostrar en Nueva Venta:
/// el panel de "Abrir caja", la barra con el saldo, o el aviso de turno ajeno.
/// </summary>
public class CajaEstadoDto
{
    public bool CajaAbierta { get; set; }
    public CajaAperturaDto? Caja { get; set; }

    /// <summary>Turno abierto del usuario que consulta, si lo tiene.</summary>
    public CajaTurnoDto? TurnoActual { get; set; }

    /// <summary>
    /// Turno abierto que pertenece a OTRO usuario (alguien olvidó cuadrar).
    /// Mientras exista, el usuario actual no puede iniciar el suyo.
    /// </summary>
    public CajaTurnoDto? TurnoDeOtroUsuario { get; set; }

    /// <summary>Efectivo que debería haber ahora mismo en el cajón.</summary>
    public decimal SaldoEfectivo { get; set; }

    /// <summary>
    /// El usuario ya cerró un turno en esta caja. Evita que al volver a Nueva
    /// Venta (tras cuadrar y seguir navegando) se le abra otro turno solo.
    /// </summary>
    public bool UsuarioYaCuadro { get; set; }

    /// <summary>
    /// Con la caja cerrada, el efectivo con el que cerró la última caja de la
    /// sucursal: se propone como fondo de la nueva apertura. Null si nunca se
    /// cerró una o si se cerró sin contar el efectivo.
    /// </summary>
    public decimal? SugerenciaMontoInicial { get; set; }

    /// <summary>
    /// La caja abierta se aperturó un día anterior al de hoy: nadie la cerró al
    /// terminar la jornada. Es una sugerencia para el frontend, no un bloqueo:
    /// se puede seguir vendiendo sobre ella con un turno nuevo.
    /// </summary>
    public bool CajaDeDiaAnterior { get; set; }
}

public class AbrirCajaDto
{
    public int SucursalId { get; set; }
    public decimal MontoInicial { get; set; }
    public string? Observaciones { get; set; }
}

public class IniciarTurnoDto
{
    public int SucursalId { get; set; }
}

/// <summary>Vista previa del cuadre: lo que el modal muestra antes de confirmar.</summary>
public class CuadreTurnoDto
{
    public int CajaTurnoId { get; set; }
    public int CajaAperturaId { get; set; }
    public string? NombreUsuario { get; set; }
    public DateTime FechaInicio { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal VentasEfectivo { get; set; }
    public decimal TotalRetiros { get; set; }
    public decimal EfectivoEsperado { get; set; }
    public decimal TotalVentas { get; set; }
    public int CantidadComprobantes { get; set; }
    public List<MedioPagoResumenDto> Medios { get; set; } = new();
    public List<CajaRetiroDto> Retiros { get; set; } = new();
}

/// <summary>Salida de efectivo del cajón dentro de un turno, con su motivo.</summary>
public class CajaRetiroDto
{
    public int CajaRetiroId { get; set; }
    public int CajaTurnoId { get; set; }
    public decimal Monto { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime FechaRetiro { get; set; }
    public string? NombreUsuario { get; set; }
}

public class RegistrarRetiroDto
{
    public int CajaTurnoId { get; set; }
    public decimal Monto { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

/// <summary>Cajero que tuvo movimiento en el día del corte, para el selector.</summary>
public class CajeroResumenDto
{
    public int UsuarioId { get; set; }
    public string? NombreUsuario { get; set; }
}

public class VentaCategoriaDto
{
    public string Categoria { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}

public class ObservacionTurnoDto
{
    public string? NombreUsuario { get; set; }
    public string Texto { get; set; } = string.Empty;
}

/// <summary>
/// Corte de caja de un día (y opcionalmente de un cajero puntual), en el
/// formato que el módulo Caja muestra: entradas de efectivo, medios de pago,
/// ventas por categoría, retiros y ganancia del día.
/// </summary>
public class CorteDiarioDto
{
    public DateTime Fecha { get; set; }
    public int SucursalId { get; set; }
    public string? NombreSucursal { get; set; }
    public int? UsuarioId { get; set; }
    public string? NombreUsuario { get; set; }

    public decimal DineroInicial { get; set; }
    public decimal VentasEfectivo { get; set; }
    public decimal TotalRetiros { get; set; }
    public decimal EfectivoEsperado { get; set; }

    public decimal VentasTotales { get; set; }
    public int CantidadComprobantes { get; set; }
    public decimal? GananciaDia { get; set; }

    public List<MedioPagoResumenDto> OtrosMediosPago { get; set; } = new();
    public List<VentaCategoriaDto> VentasPorCategoria { get; set; } = new();
    public List<CajaRetiroDto> Retiros { get; set; } = new();
    public List<ObservacionTurnoDto> Observaciones { get; set; } = new();
    public List<CajeroResumenDto> CajerosDelDia { get; set; } = new();
}

public class CuadrarCajaDto
{
    public int CajaTurnoId { get; set; }
    public decimal EfectivoContado { get; set; }
    public string? Observaciones { get; set; }
    /// <summary>True = además de cuadrar el turno, cierra la caja del día.</summary>
    public bool CerrarCaja { get; set; }
}

public class CajaHistorialItemDto : CajaAperturaDto
{
    public int CantidadTurnos { get; set; }
    public decimal TotalVentas { get; set; }
}

public class CajaHistorialDto
{
    public List<CajaHistorialItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CajaDetalleDto
{
    public CajaAperturaDto Caja { get; set; } = new();
    public List<CajaTurnoDto> Turnos { get; set; } = new();
}
