
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<IEnumerable<Cliente>> GetAllClientesAsync();
    Task<Cliente?> GetClienteByIdAsync(int clienteId);
    Task<Cliente> RegistrarClienteAsync(Cliente cliente);
    Task<bool> EditarClienteAsync(Cliente cliente);
    Task<bool> EliminarClienteAsync(int clienteId);
    Task<Cliente?> GetByNumDocAsync(string numeroDocumento); // interno solo tabla cliente, no va al controller
}
