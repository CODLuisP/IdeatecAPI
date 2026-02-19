using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IEmpresaRepository : IRepository<Empresa>
{
    Task<IEnumerable<Empresa>> GetAllEmpresasAsync();
    Task<Empresa?> GetEmpresaByIdAsync(int id);
    Task<bool> ExisteRucAsync(string ruc);
    Task<int> CreateEmpresaAsync(Empresa empresa);
    Task UpdateEmpresaAsync(Empresa empresa);
    Task DeleteEmpresaAsync(int id);

    
}