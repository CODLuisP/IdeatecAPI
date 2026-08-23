using IdeatecAPI.Application.Features.Productos.DTO;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IProductoRepository : IRepository<Producto>
{
    Task<IEnumerable<Producto>> GetAllProductosAsync(int sucursalId); //Producto completo por sucursal
    Task<IEnumerable<Producto>> GetAllProductosBaseRucAsync(string empresaRuc);
    Task<IEnumerable<Producto>> GetAllProductosRucAsync(string empresaRuc);
    Task<IEnumerable<Producto>> GetProductosRucDisponiblesAsync(int sucursalId);
    Task<IEnumerable<Producto>> SearchProductosRucDisponiblesAsync(int sucursalId, string palabra);
    Task<IEnumerable<Producto>> SearchBySucursalAsync(int sucursalId, string palabra);
    Task<IEnumerable<Producto>> SearchByRucAsync(string empresaRuc, string palabra);
    Task<IEnumerable<ReporteProductoItemDTO>> GetReporteProductosAsync(ReporteProductoFiltroDTO filtro);
    Task<Producto?> GetProductoByIdAsync(int productoId, int sucursalId);
    Task<bool> ExisteProductoAsync(string codigo);
    Task<Producto> RegistrarProductoAsync(Producto producto);
    Task<SucursalProducto> RegistrarSucursalProductoAsync(SucursalProducto sucursalProducto);
    Task<bool> EditarProductoAsync(Producto producto);
    // Las cantidades son decimales: hay productos que se venden por peso o volumen.
    Task<bool> ActualizarStockAsync(int sucursalProductoId, decimal cantidad);
    // Lee y bloquea (FOR UPDATE) el stock de todos los productos de una venta, para poder
    // validar la disponibilidad de una vez en lugar de un UPDATE guardado por producto.
    Task<IEnumerable<StockBloqueadoDTO>> GetStockParaDescontarAsync(IEnumerable<int> sucursalProductoIds);
    // Aplica todos los descuentos de stock de una venta en un unico UPDATE. Devuelve
    // cuantas filas cumplieron la guardia de stock suficiente.
    Task<int> DescontarStockBatchAsync(IReadOnlyDictionary<int, decimal> descuentosPorSucursalProducto);
    Task<bool> DevolverStockAsync(int ProductoId, int SucursalId, decimal cantidad);
    Task<bool> EditarSucursalProductoAsync(SucursalProducto sucursalProducto);
    Task<bool> EliminarSucursalProductoAsync(int sucursalProductoId); 
    Task<Producto?> ObtenerProductoPorCodigoAsync(string codigo);
    Task<bool> ExisteEnSucursalAsync(int productoId, int sucursalId);
    Task<bool> RegistrarCompraStockAsync(int productoId, int sucursalId, decimal cantidad, decimal precioCompra);
    Task<bool> IncrementarStockSinCostoAsync(int productoId, int sucursalId, decimal cantidad);
    Task<bool> ActualizarCostoSinStockAsync(int productoId, int sucursalId, decimal precioCompra);
    Task<Producto?> GetInfoConversionBySucursalProductoIdAsync(int sucursalProductoId);
    // Version en bloque: resuelve la info de conversion de todos los productos de una venta
    // en una sola consulta, con el sucursalProductoID del producto base ya incluido.
    Task<IEnumerable<InfoConversionStockDTO>> GetInfoConversionBySucursalProductoIdsAsync(IEnumerable<int> sucursalProductoIds);
    Task<bool> DescontarStockBaseAsync(int productoBaseId, int sucursalId, decimal cantidad);
    Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, int sucursalId);
    Task<bool> ExisteCodigoBarrasEnEdicionAsync(string codigoBarras, int sucursalProductoId, int productoIdExcluir);
}