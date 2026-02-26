using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface ICategoriaRepository : IRepository<Categoria>
{
    Task<IEnumerable<Categoria>> GetAllCategoriasAsync();
    Task<Categoria?> GetCategoriaByIdAsync(int id);
    Task<bool> RegistrarCategoriaAsync(Categoria categoria);
    Task<bool> EditarCategoriaAsync(Categoria categoria);
    Task<bool> EliminarCategoriaAsync(int CategoriaId);
}