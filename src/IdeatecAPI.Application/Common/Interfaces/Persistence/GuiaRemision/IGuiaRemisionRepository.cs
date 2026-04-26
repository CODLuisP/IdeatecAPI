using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IGuiaRemisionRepository
{
    // ── Antes recibía empresaId (int), ahora queda como fallback interno ──
    Task<IEnumerable<GuiaRemision>> GetAllAsync(int empresaId);

    // ── Nuevo: listado por RUC + tipoDoc, con sucursalId opcional ──
    Task<IEnumerable<GuiaRemision>> GetAllByRucAsync(string empresaRuc, string tipoDoc, int? sucursalId);

    Task<GuiaRemision?> GetByIdAsync(int guiaId);
    Task<GuiaRemision?> GetBySerieCorrelativoAsync(string empresaRuc, string serie, int correlativo);
    Task<int> CreateAsync(GuiaRemision guia);
    Task UpdateEstadoAsync(int guiaId, string estado, string? codigo, string? mensaje, string? ticket, string? cdr, DateTime? fechaEnvio);
    Task<bool> ExisteAsync(int empresaId, string tipoDoc, string serie, int correlativo);
}