namespace IdeatecAPI.Domain.Entities;

/// <summary>
/// Turno de un usuario dentro de una caja abierta. Solo puede haber un turno
/// ABIERTO por caja: al cuadrar se cierra el turno y el siguiente usuario
/// arranca uno nuevo con el efectivo contado que quedó en el cajón.
/// </summary>
public class CajaTurno
{
    public int CajaTurnoId { get; set; }
    public int CajaAperturaId { get; set; }

    public int UsuarioId { get; set; }
    public string? NombreUsuario { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    /// <summary>Efectivo en el cajón al empezar el turno.</summary>
    public decimal SaldoInicial { get; set; }
    /// <summary>SaldoInicial + ventas en efectivo del turno.</summary>
    public decimal? EfectivoEsperado { get; set; }
    public decimal? EfectivoContado { get; set; }
    public decimal? Diferencia { get; set; }

    public decimal? TotalVentas { get; set; }
    public int? CantidadComprobantes { get; set; }

    /// <summary>ABIERTO | CERRADO</summary>
    public string Estado { get; set; } = EstadoAbierto;

    /// <summary>
    /// Usuario que ejecutó el cierre. Normalmente el mismo dueño del turno, pero
    /// difiere cuando alguien olvidó cuadrar y otro usuario cierra su turno.
    /// </summary>
    public int? CerradoPorUsuarioId { get; set; }
    /// <summary>True si este cuadre además cerró la caja del día.</summary>
    public bool EsCierreCaja { get; set; }
    public string? Observaciones { get; set; }

    public const string EstadoAbierto = "ABIERTO";
    public const string EstadoCerrado = "CERRADO";
}
