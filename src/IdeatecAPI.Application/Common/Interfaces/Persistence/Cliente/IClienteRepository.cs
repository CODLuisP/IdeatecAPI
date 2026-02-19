using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Features.Clientes.DTOs;
using IdeatecAPI.Domain.Entities.Cliente;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence
{
    public interface IClienteRepository : IRepository<Cliente>
    {
        Task<IEnumerable<Cliente>> GetAllClientesAsync();
        Task<int> RegistrarClienteAsync(Cliente cliente);
    }

}