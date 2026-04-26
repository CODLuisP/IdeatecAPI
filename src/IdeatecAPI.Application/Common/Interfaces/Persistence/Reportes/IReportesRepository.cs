using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IReportesRepository
{
    Task<ReporteResponseDto> GetReportesPorEmpresaAsync(
        string ruc,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int limite,
        int? usuarioId);

    Task<ReporteResponseDto> GetReportesPorSucursalAsync(
        int sucursalId,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int limite,
        int? usuarioId);

    // Endpoint separado para Excel sin límite
    Task<List<ClienteExportDto>> GetClientesExportPorEmpresaAsync(
        string ruc,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int? usuarioId);

    Task<List<ClienteExportDto>> GetClientesExportPorSucursalAsync(
        int sucursalId,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int? usuarioId);
}