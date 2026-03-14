using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IProductoRepository : IRepository<Producto>
{
    Task<IEnumerable<Producto>> GetAllProductosAsync();
    Task<Producto?> GetProductoByIdAsync(int id);
    Task<bool> ExisteProductoAsync(string codigo);
    Task<Producto> RegistrarProductoAsync(Producto producto);
    Task<bool> EditarProductoAsync(Producto producto);
    Task<bool> EliminarProductoAsync(int productoId);
}