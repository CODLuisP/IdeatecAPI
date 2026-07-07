namespace IdeatecAPI.Domain.Entities;
public class InventarioLote
{
    public int InventarioLoteId { get; set; }
    public int SucursalProductoId { get; set; }
    public int? CompraProveedorId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public DateTime FechaLote { get; set; }
    public decimal CantidadOriginal { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal SaldoCantidad { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime? FechaCreacion { get; set; }

    // Datos enriquecidos para reportes (no se persisten)
    public string? NomProducto { get; set; }
    public string? Codigo { get; set; }
}
