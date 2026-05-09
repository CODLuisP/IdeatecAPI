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
        var result = await _unitOfWork.Dashboard.GetDashboardPorEmpresaAsync(ruc, fecha, limite);
        result.VentasNetas = CalcularVentasNetas(result);
        return result;
    }

    public async Task<DashboardResponseDto> GetDashboardPorSucursalAsync(
        int sucursalId,
        DateTime? fecha,
        int limite)
    {
        var result = await _unitOfWork.Dashboard.GetDashboardPorSucursalAsync(sucursalId, fecha, limite);
        result.VentasNetas = CalcularVentasNetas(result);
        return result;
    }

    // ── VentasNetas solo considera notas que afectan documentos del mismo día ──
    private static decimal CalcularVentasNetas(DashboardResponseDto dto) =>
        dto.VentasDelDia
        + dto.TotalNotasDebitoDelDia
        - dto.TotalNotasCreditoDelDia;
}