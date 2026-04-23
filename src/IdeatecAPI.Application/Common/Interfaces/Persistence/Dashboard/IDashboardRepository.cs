using IdeatecAPI.Application.Features.Dashboard.DTOs;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IDashboardRepository
{
    Task<DashboardResponseDto> GetDashboardPorEmpresaAsync(
        string ruc,
        DateTime? desde,
        DateTime? hasta,
        int limite);

    Task<DashboardResponseDto> GetDashboardPorSucursalAsync(
        int sucursalId,
        DateTime? desde,
        DateTime? hasta,
        int limite);
}