namespace IdeatecAPI.Domain.Entities;

/// <summary>
/// Caja del día de una sucursal. Solo puede existir una con estado ABIERTA por
/// sucursal a la vez; dentro de ella se suceden los turnos de cada usuario.
/// </summary>
public class CajaApertura
{
    public int CajaAperturaId { get; set; }
    public string? EmpresaRuc { get; set; }
    public int SucursalId { get; set; }
    public string? CodEstablecimiento { get; set; }

    public decimal MontoInicial { get; set; }
    public DateTime FechaApertura { get; set; }
    public int UsuarioApertura { get; set; }
    public string? NombreUsuarioApertura { get; set; }

    public DateTime? FechaCierre { get; set; }
    public int? UsuarioCierre { get; set; }
    public string? NombreUsuarioCierre { get; set; }

    /// <summary>Efectivo que debía haber en el cajón al cerrar la caja.</summary>
    public decimal? EfectivoEsperado { get; set; }
    /// <summary>Efectivo físico contado al cerrar la caja.</summary>
    public decimal? EfectivoContado { get; set; }
    /// <summary>Contado - esperado. Negativo = faltante, positivo = sobrante.</summary>
    public decimal? Diferencia { get; set; }

    /// <summary>ABIERTA | CERRADA</summary>
    public string Estado { get; set; } = EstadoAbierta;
    public string? Observaciones { get; set; }

    public const string EstadoAbierta = "ABIERTA";
    public const string EstadoCerrada = "CERRADA";
}
