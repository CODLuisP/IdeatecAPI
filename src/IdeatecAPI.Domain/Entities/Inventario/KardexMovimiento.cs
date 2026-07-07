namespace IdeatecAPI.Domain.Entities;
public class KardexMovimiento
{
    public int KardexMovimientoId { get; set; }
    public int SucursalProductoId { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;
    public string? ReferenciaTipo { get; set; }
    public int? ReferenciaId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal? CostoUnitarioPromedio { get; set; }
    public decimal? CostoTotal { get; set; }
    public decimal SaldoCantidadPost { get; set; }
    public decimal SaldoValorPost { get; set; }
    public DateTime FechaMovimiento { get; set; }
    public int? IdUsuario { get; set; }

    // Cantidad de lotes distintos que aportaron/consumieron este movimiento (no se persiste).
    public int LotesConsumidos { get; set; }
}
