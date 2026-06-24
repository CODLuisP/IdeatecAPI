namespace IdeatecAPI.Application.Features.Proveedor.DTOs;

public class ObtenerCompraProveedorDTO
{
    public int CompraProveedorId { get; set; }
    public int ProveedorId { get; set; }
    public string? RazonSocialProveedor { get; set; }
    public int SucursalId { get; set; }
    public string? NomSucursal { get; set; }
    public int ProductoId { get; set; }
    public string? NomProducto { get; set; }
    public decimal? PrecioCompra { get; set; }
    public int? Cantidad { get; set; }
    public string? UnidadMedida { get; set; }
    public string? DocReferencia { get; set; }
    public DateTime? FechaCreacion { get; set; }
}
