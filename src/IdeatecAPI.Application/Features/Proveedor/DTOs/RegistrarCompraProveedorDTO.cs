namespace IdeatecAPI.Application.Features.Proveedor.DTOs;

public class RegistrarCompraProveedorDTO
{
    public int? ProveedorId { get; set; }
    public int? SucursalId { get; set; }
    public int? ProductoId { get; set; }
    public decimal? PrecioCompra { get; set; }
    // Decimal: se pueden comprar 30.5 kg, 12.75 L, etc.
    public decimal? Cantidad { get; set; }
    public string? UnidadMedida { get; set; }
    public string? DocReferencia { get; set; }
    public int? IdUsuario { get; set; }
    public DateTime? FechaVencimiento { get; set; }
}
