using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Inventario.DTOs;

// Un movimiento de kardex junto con la resolución de qué se vendió realmente en esa
// línea (producto base o paquete), igual que RentabilidadFilasSql en InventarioLoteRepository.
public class KardexMovimientoResuelto
{
    public required KardexMovimiento Movimiento { get; set; }
    public int? ProductoId { get; set; }
    public string? NomProducto { get; set; }
    public string? Codigo { get; set; }
    public bool EsPaquete { get; set; }
    public decimal CantidadVenta { get; set; }
    public decimal? CostoVenta { get; set; }
}
