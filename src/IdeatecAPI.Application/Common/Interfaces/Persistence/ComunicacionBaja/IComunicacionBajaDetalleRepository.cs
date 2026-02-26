using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IComunicacionBajaDetalleRepository
{
    Task<IEnumerable<ComunicacionBajaDetalle>> GetByBajaIdAsync(int bajaId);
    Task CreateAsync(ComunicacionBajaDetalle detalle);
}