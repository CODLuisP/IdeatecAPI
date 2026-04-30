using IdeatecAPI.Application.Features.Dashboard.DTOs;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IDashboardRepository
{
    Task<DashboardResponseDto> GetDashboardPorEmpresaAsync(
        string ruc,
        DateTime? fecha,
        int limite);

    Task<DashboardResponseDto> GetDashboardPorSucursalAsync(
        int sucursalId,
        DateTime? fecha,
        int limite);
}