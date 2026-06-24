using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IProveedorRepository : IRepository<Proveedor>
{
    Task<IEnumerable<Proveedor>> GetAllByRucEmpresaAsync(string rucEmpresa);
    Task<Proveedor?> GetByIdAsync(int proveedorId);
    Task<Proveedor?> GetByIdRucEmpresaAsync(string rucEmpresa, int proveedorId);
    Task<Proveedor?> GetByNumDocRucEmpresaAsync(string rucEmpresa, string numDocumento);
    Task<Proveedor> RegistrarAsync(Proveedor proveedor);
    Task<bool> EditarAsync(Proveedor proveedor);
    Task<bool> EliminarAsync(int proveedorId);
    Task<IEnumerable<Proveedor>> SearchByRucEmpresaAsync(string rucEmpresa, string palabra);
}
