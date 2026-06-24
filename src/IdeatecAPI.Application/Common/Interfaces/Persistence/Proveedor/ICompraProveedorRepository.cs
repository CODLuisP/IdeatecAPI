using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface ICompraProveedorRepository : IRepository<CompraProveedor>
{
    Task<IEnumerable<CompraProveedor>> GetAllBySucursalAsync(int sucursalId);
    Task<IEnumerable<CompraProveedor>> GetAllByProveedorAsync(int proveedorId);
    Task<IEnumerable<CompraProveedor>> GetByDocReferenciaAsync(string docReferencia, int sucursalId);
    Task<CompraProveedor?> GetByIdAsync(int compraProveedorId);
    Task<CompraProveedor> RegistrarAsync(CompraProveedor compra);
    Task<bool> EliminarAsync(int compraProveedorId);
}
