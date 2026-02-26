using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IComprobanteRepository : IRepository<Comprobante>
{
    Task<int> GenerarComprobanteAsync(Comprobante dto);
}
