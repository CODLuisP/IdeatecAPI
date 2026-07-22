namespace IdeatecAPI.Domain.Entities;
public class SucursalProducto
{
    public int SucursalProductoId { get; set; }
    public int ProductoId { get; set; }
    public int SucursalId { get; set; }
    public string? NomSucursal { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public int? Stock { get; set; }
    public decimal? UltimoPrecioCompra { get; set; }
    public DateTime? FechaUltimaCompra { get; set; }
    public decimal? PrecioMayorista { get; set; }
    public int? CantidadMinimaMayorista { get; set; }
    public bool? EnPromocion { get; set; }
    public decimal? PorcentajeDescuento { get; set; }
    public int? UsuarioId { get; set; }
    public string? UbicacionTienda { get; set; }
    public bool? Estado { get; set; }
    public DateTime? FechaCreacion { get; set; }

    // Dato enriquecido para listados: fecha de vencimiento más próxima entre los
    // lotes de inventario con saldo (no se persiste en esta tabla, ver inventario_lote).
    public DateTime? ProximoVencimiento { get; set; }
}