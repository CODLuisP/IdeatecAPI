using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Application.Features.Reportes.Services;

public interface IDashboardReportService
{
    Task<DashboardReportDto> GenerarReporteAsync(DashboardReportRequestDto request);
}
