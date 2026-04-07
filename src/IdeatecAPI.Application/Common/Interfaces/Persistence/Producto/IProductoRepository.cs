using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IProductoRepository : IRepository<Producto>
{
    Task<IEnumerable<Producto>> GetAllProductosAsync(int sucursalId); //Producto completo por sucursal
    Task<IEnumerable<Producto>> GetAllProductosBaseRucAsync(string empresaRuc);
    Task<IEnumerable<Producto>> GetAllProductosRucAsync(string empresaRuc);
    Task<IEnumerable<Producto>> GetProductosRucDisponiblesAsync(int sucursalId);
    Task<IEnumerable<Producto>> SearchBySucursalAsync(int sucursalId, string palabra);
    Task<IEnumerable<Producto>> SearchByRucAsync(string empresaRuc, string palabra);
    Task<Producto?> GetProductoByIdAsync(int productoId, int sucursalId);
    Task<bool> ExisteProductoAsync(string codigo);
    Task<Producto> RegistrarProductoAsync(Producto producto);
    Task<SucursalProducto> RegistrarSucursalProductoAsync(SucursalProducto sucursalProducto);
    Task<bool> EditarProductoAsync(Producto producto);
    Task<bool> ActualizarStockAsync(int sucursalProductoId, int cantidad);
    Task<bool> EditarSucursalProductoAsync(SucursalProducto sucursalProducto);
    Task<bool> EliminarSucursalProductoAsync(int sucursalProductoId); 
    Task<Producto?> ObtenerProductoPorCodigoAsync(string codigo);
    Task<bool> ExisteEnSucursalAsync(int productoId, int sucursalId);   
}