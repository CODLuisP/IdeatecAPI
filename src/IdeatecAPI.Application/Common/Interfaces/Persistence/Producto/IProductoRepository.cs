using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IProductoRepository : IRepository<Producto>
{
    Task<IEnumerable<Producto>> GetAllProductosAsync(int sucursalId);
    Task<Producto?> GetProductoByIdAsync(int productoId, int sucursalId);
    Task<bool> ExisteProductoAsync(string codigo);
    Task<Producto> RegistrarProductoAsync(Producto producto);
    Task<SucursalProducto> RegistrarSucursalProductoAsync(SucursalProducto sucursalProducto);
    Task<bool> EditarProductoAsync(Producto producto);
    Task<bool> EditarSucursalProductoAsync(SucursalProducto sucursalProducto);
    Task<bool> EliminarSucursalProductoAsync(int sucursalProductoId);    
}