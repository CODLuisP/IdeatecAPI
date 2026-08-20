using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence.Reportes;

public interface IDashboardReportPdfService
{
    Task<byte[]> ExportarDashboardReportPdfAsync(DashboardReportDto data);
}
