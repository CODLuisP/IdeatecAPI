namespace IdeatecAPI.Domain.Entities;
public class CompraProveedor
{
    public int CompraProveedorId { get; set; }
    public int ProveedorId { get; set; }
    public int SucursalId { get; set; }
    public int ProductoId { get; set; }
    public decimal? PrecioCompra { get; set; }
    // Decimal: se pueden comprar 30.5 kg, 12.75 L, etc.
    public decimal? Cantidad { get; set; }
    public string? UnidadMedida { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public string? DocReferencia { get; set; }
    public int? IdUsuario { get; set; }

    // Datos enriquecidos para listados (no se persisten)
    public string? RazonSocialProveedor { get; set; }
    public string? NomProducto { get; set; }
    public string? NomSucursal { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public decimal? CantidadOriginalLote { get; set; }
    public decimal? SaldoCantidadLote { get; set; }
}
