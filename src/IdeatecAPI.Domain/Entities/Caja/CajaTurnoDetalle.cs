namespace IdeatecAPI.Domain.Entities;

/// <summary>
/// Foto de lo recaudado por medio de pago en un turno, congelada al cuadrar.
/// Se guarda aunque solo el efectivo se cuente físicamente, para que el
/// histórico no dependa de recalcular sobre comprobantes que pueden anularse.
/// </summary>
public class CajaTurnoDetalle
{
    public int CajaTurnoDetalleId { get; set; }
    public int CajaTurnoId { get; set; }

    /// <summary>Efectivo | Tarjeta | Yape | Plin | Transferencia | Otro</summary>
    public string? MedioPago { get; set; }
    public decimal MontoEsperado { get; set; }
    /// <summary>Solo se llena para Efectivo; el resto no se cuenta a mano.</summary>
    public decimal? MontoContado { get; set; }
    public decimal? Diferencia { get; set; }
}
