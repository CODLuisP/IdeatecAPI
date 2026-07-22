namespace IdeatecAPI.Application.Features.Inventario.DTOs;

public class LoteReporteDTO
{
    public int InventarioLoteId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public DateTime FechaLote { get; set; }
    public decimal CantidadOriginal { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal SaldoCantidad { get; set; }
    public decimal SaldoValor => SaldoCantidad * CostoUnitario;
    public DateTime? FechaVencimiento { get; set; }
}
