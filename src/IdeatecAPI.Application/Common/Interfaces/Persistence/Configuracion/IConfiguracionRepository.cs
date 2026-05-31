using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IConfiguracionRepository : IRepository<Configuracion>
{
    Task<Configuracion?> GetByRucAsync(string ruc);
    Task<bool> RegistrarConfiguracionAsync(Configuracion configuracion);
    Task<bool> EditarConfiguracionAsync(string ruc, Configuracion configuracion);
}
