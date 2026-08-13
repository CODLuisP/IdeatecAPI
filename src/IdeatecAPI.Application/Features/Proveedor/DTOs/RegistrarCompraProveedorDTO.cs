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

    // Solo aplica cuando el único campo que cambia es FechaVencimiento y el lote tiene ventas
    // parciales: confirma que se acepta el aviso de que el cambio también afecta el historial
    // de lo ya vendido. Ver CompraProveedorService.EditarAsync.
    public bool Confirmar { get; set; } = false;
}
