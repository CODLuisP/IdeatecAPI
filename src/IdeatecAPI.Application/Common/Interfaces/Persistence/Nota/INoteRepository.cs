using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface INoteRepository : IRepository<Note>
{
    Task<IEnumerable<Note>> GetAllNotesAsync(int empresaId);
    Task<Note?> GetNoteByIdAsync(int comprobanteId);
    Task<Note?> GetNoteByNumeroAsync(int empresaId, string tipoDoc, string serie, int correlativo);
    Task<bool> ExisteNoteAsync(int empresaId, string tipoDoc, string serie, int correlativo);
    Task<int> CreateNoteAsync(Note note);
    Task UpdateNoteAsync(Note note);
    Task UpdateEstadoSunatAsync(int comprobanteId, string estado, string? codigo, string? mensaje, string? xml, string? cdr);
}