using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface INoteLegendRepository : IRepository<NoteLegend>
{
    Task<IEnumerable<NoteLegend>> GetByComprobanteIdAsync(int comprobanteId);
    Task<int> CreateLegendAsync(NoteLegend legend);
    Task DeleteByComprobanteIdAsync(int comprobanteId);
}