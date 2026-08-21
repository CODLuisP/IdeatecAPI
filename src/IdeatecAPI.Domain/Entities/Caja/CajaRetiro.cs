namespace IdeatecAPI.Domain.Entities;

/// <summary>
/// Salida de efectivo del cajón dentro de un turno (no es una venta ni un
/// pago a cliente/proveedor por comprobante). Reduce el efectivo esperado
/// del turno al cuadrar.
/// </summary>
public class CajaRetiro
{
    public int CajaRetiroId { get; set; }
    public int CajaTurnoId { get; set; }

    public decimal Monto { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime FechaRetiro { get; set; }

    public int UsuarioId { get; set; }
    public string? NombreUsuario { get; set; }
}
