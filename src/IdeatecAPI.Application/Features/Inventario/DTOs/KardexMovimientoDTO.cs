namespace IdeatecAPI.Application.Features.Inventario.DTOs;

public class KardexMovimientoDTO
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
    public int LotesConsumidos { get; set; }
}
