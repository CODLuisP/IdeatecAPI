
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

    public interface IDireccionRepository : IRepository<Direccion>
    {
        Task CrearDireccionAsync(Direccion direccion);
        Task<bool> EditarDireccionAsync(Direccion direccion);
        Task<bool> EliminarDireccionAsync(int direccionId);
    }
