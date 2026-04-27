using IdeatecAPI.Application.Features.CuentasPorCobrar.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface ICuentasPorCobrarRepository
{
    Task<IEnumerable<ListaCuentasPorCobrarDto>> GetCuentasPorCobrarAsync(
        string empresaRuc,
        string? establecimientoAnexo,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? clienteNumDoc);

    Task<IEnumerable<CuotaDto>> GetCuotasByComprobanteIdAsync(int comprobanteId);

    Task<Cuota?> GetCuotaByIdAsync(int cuotaId);

    Task<bool> PagarCuotaAsync(PagarCuotaDto dto, string nuevoEstado);
}