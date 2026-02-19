using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Domain.Entities.Cliente;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence
{
    public interface IDireccionRepository : IRepository<Direccion>
    {
        Task CrearDireccionAsync(Direccion direccion);
    }
}