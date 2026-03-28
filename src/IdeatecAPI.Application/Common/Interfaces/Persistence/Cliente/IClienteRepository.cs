
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<IEnumerable<Cliente>> GetAllClientesRucAsync(string empresaRuc);
    Task<IEnumerable<Cliente>> GetAllClientesSucursalAsync(int sucursalId);
    Task<Cliente?> GetClienteByIdAsync(int clienteId);
    Task<Cliente?> GetClienteByIdEmpresaAsync(string empresaRuc, int clienteId);
    Task<Cliente> RegistrarClienteAsync(Cliente cliente);
    Task<bool> EditarClienteAsync(Cliente cliente);
    Task<bool> EliminarClienteAsync(int clienteId);
    Task<Cliente?> GetByNumDocAsync(string numeroDocumento); // interno solo tabla cliente, no va al controller
    Task<Cliente?> GetByClienteRepetidoEmpresaAsync(string numeroDocumento, int sucursalId);
    Task<IEnumerable<Cliente>> SearchByRucAsync(string empresaRuc, string palabra);
}
