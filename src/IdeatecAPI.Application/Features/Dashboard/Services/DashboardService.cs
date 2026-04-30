using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Dashboard.DTOs;

namespace IdeatecAPI.Application.Features.Dashboard.Services;

public interface IDashboardService
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

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardResponseDto> GetDashboardPorEmpresaAsync(
        string ruc,
        DateTime? fecha,
        int limite)
    {
        return await _unitOfWork.Dashboard.GetDashboardPorEmpresaAsync(ruc, fecha, limite);
    }

    public async Task<DashboardResponseDto> GetDashboardPorSucursalAsync(
        int sucursalId,
        DateTime? fecha,
        int limite)
    {
        return await _unitOfWork.Dashboard.GetDashboardPorSucursalAsync(sucursalId, fecha, limite);
    }
}