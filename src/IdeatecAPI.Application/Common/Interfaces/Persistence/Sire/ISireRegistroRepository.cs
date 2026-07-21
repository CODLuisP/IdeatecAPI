using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface ISireRegistroRepository : IRepository<SireRegistro>
{
    Task<SireRegistro?> GetByRucAndPeriodoAsync(string rucEmpresa, string perTributario);
    Task<IEnumerable<SireRegistro>> GetHistorialByRucAsync(string rucEmpresa);
    Task<int> CreateSireRegistroAsync(SireRegistro registro);
    Task UpdateSireRegistroAsync(SireRegistro registro);
}
