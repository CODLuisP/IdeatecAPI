using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Reportes.DTOs;
namespace IdeatecAPI.Application.Features.Reportes.Services;

public interface IReportesService
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

public class ReportesService : IReportesService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportesService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReporteResponseDto> GetReportesPorEmpresaAsync(
        string ruc,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int limite,
        int? usuarioId)
    {
        return await _unitOfWork.Reportes.GetReportesPorEmpresaAsync(
            ruc, periodo, desde, hasta, limite, usuarioId);
    }

    public async Task<ReporteResponseDto> GetReportesPorSucursalAsync(
        int sucursalId,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int limite,
        int? usuarioId)
    {
        return await _unitOfWork.Reportes.GetReportesPorSucursalAsync(
            sucursalId, periodo, desde, hasta, limite, usuarioId);
    }

    public async Task<List<ClienteExportDto>> GetClientesExportPorEmpresaAsync(
        string ruc,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int? usuarioId)
    {
        return await _unitOfWork.Reportes.GetClientesExportPorEmpresaAsync(
            ruc, periodo, desde, hasta, usuarioId);
    }

    public async Task<List<ClienteExportDto>> GetClientesExportPorSucursalAsync(
        int sucursalId,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int? usuarioId)
    {
        return await _unitOfWork.Reportes.GetClientesExportPorSucursalAsync(
            sucursalId, periodo, desde, hasta, usuarioId);
    }
}