using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface INoteDetailRepository : IRepository<NoteDetail>
{
    Task<IEnumerable<NoteDetail>> GetByComprobanteIdAsync(int comprobanteId);
    Task<int> CreateDetailAsync(NoteDetail detail);
    Task DeleteByComprobanteIdAsync(int comprobanteId);
}