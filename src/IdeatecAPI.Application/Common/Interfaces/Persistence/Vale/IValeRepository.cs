using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IValeRepository : IRepository<Vale>
{
    Task<IEnumerable<Vale>> GetAllValesAsync();
    Task<bool> RegistrarValeAsync(Vale vale);
    Task<bool> EditarValeAsync(int idVale, Vale vale);
    Task<bool> EliminarValeAsync(int idVale);
}
