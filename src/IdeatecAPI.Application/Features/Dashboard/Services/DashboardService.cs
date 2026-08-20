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
        result.Ganancias = await CalcularGananciasAsync(ruc, codEstablecimiento: null, fecha);
        return result;
    }

    public async Task<DashboardResponseDto> GetDashboardPorSucursalAsync(
        int sucursalId,
        DateTime? fecha,
        int limite)
    {
        var result = await _unitOfWork.Dashboard.GetDashboardPorSucursalAsync(sucursalId, fecha, limite);
        result.VentasNetas = CalcularVentasNetas(result);

        var sucursal = await _unitOfWork.Sucursal.GetByIdSucursalAsync(sucursalId);
        result.Ganancias = await CalcularGananciasAsync(sucursal.EmpresaRuc!, sucursal.CodEstablecimiento, fecha);

        return result;
    }

    // ── VentasNetas solo considera notas que afectan documentos del mismo día ──
    private static decimal CalcularVentasNetas(DashboardResponseDto dto) =>
        dto.VentasDelDia
        + dto.TotalNotasDebitoDelDia
        - dto.TotalNotasCreditoDelDia;

    // ── Ganancia del día: mismo cálculo (ingreso - costo PEPS) usado en el reporte descargable ──
    private async Task<decimal> CalcularGananciasAsync(string ruc, string? codEstablecimiento, DateTime? fecha)
    {
        var dia = fecha ?? DateTime.Today;
        var ganancias = await _unitOfWork.Reportes.GetGananciasAsync(ruc, codEstablecimiento, dia, dia, usuarioCreacion: null);
        return ganancias.IngresoVentas - ganancias.CostoVentas;
    }
}