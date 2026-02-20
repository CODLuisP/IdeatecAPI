using IdeatecAPI.Domain.Entities.Cliente;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence
{
    public interface IClienteRepository : IRepository<Cliente>
    {
        Task<IEnumerable<Cliente>> GetAllClientesAsync();
        Task<int> RegistrarClienteAsync(Cliente cliente);
        Task<Cliente?> GetByNumDocAsync(string numeroDocumento);

    }
}