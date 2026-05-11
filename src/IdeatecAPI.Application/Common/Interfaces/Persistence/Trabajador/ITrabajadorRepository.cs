using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface ITrabajadorRepository : IRepository<Trabajador>
{
    Task<IEnumerable<Trabajador>> GetAllBySucursalAsync(int sucursalId);
    Task<Trabajador?> GetByDniAsync(string dni);
    Task<Trabajador?> GetByDniEnSucursalAsync(string dni, int sucursalId); // validación duplicado
    Task<Trabajador> RegistrarAsync(Trabajador trabajador);
    Task<bool> EditarAsync(Trabajador trabajador);
    Task<bool> EliminarAsync(int id);
    Task<IEnumerable<Trabajador>> SearchBySucursalAsync(int sucursalId, string palabra);
}