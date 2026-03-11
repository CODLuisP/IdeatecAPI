using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IGuiaRemisionDetalleRepository
{
    Task<IEnumerable<GuiaRemisionDetalle>> GetByGuiaIdAsync(int guiaId);
    Task CreateAsync(GuiaRemisionDetalle detalle);
}