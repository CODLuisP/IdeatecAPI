namespace IdeatecAPI.Application.Features.Productos.DTO;

// Datos que necesita el descuento de stock para decidir si un producto vendido es un
// paquete y, en ese caso, a qué sucursalProducto base se redirige el descuento.
// Trae ya resuelto el sucursalProductoID del producto base para evitar una segunda
// consulta por cada paquete de la venta.
public class InfoConversionStockDTO
{
    public int SucursalProductoId { get; set; }
    public int ProductoId { get; set; }
    public int SucursalId { get; set; }
    public bool? EsPaquete { get; set; }
    public int? ProductoBaseId { get; set; }
    public decimal? FactorConversion { get; set; }
    public int? BaseSucursalProductoId { get; set; }
}

// Stock actual de un sucursalProducto, leido y bloqueado antes de descontarlo.
public class StockBloqueadoDTO
{
    public int SucursalProductoId { get; set; }
    public decimal? Stock { get; set; }
}
