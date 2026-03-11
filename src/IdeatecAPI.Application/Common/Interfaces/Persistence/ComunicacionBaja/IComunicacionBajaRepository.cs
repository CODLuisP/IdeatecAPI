using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IComunicacionBajaRepository
{
    Task<IEnumerable<ComunicacionBaja>> GetAllAsync(int empresaId);
    Task<ComunicacionBaja?> GetByIdAsync(int bajaId);
    Task<int> CreateAsync(ComunicacionBaja baja);
    Task UpdateEstadoAsync(int bajaId, string estado, string? codigo, string? mensaje, string? ticket, string? xml, string? cdr);
}