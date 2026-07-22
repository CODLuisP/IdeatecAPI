namespace IdeatecAPI.Application.Features.Inventario.DTOs;

public class LoteVencidoDTO
{
    public int InventarioLoteId { get; set; }
    public int SucursalProductoId { get; set; }
    public string? NomProducto { get; set; }
    public string? Codigo { get; set; }
    public string Origen { get; set; } = string.Empty;
    public DateTime FechaLote { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal SaldoCantidad { get; set; }
    public decimal CostoUnitario { get; set; }
}
