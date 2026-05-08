using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Comprobante.Services;
using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Application.Features.Reportes.Services;

public interface IReportesService
{
    Task<ReporteResponseDto> GetReportesPorEmpresaAsync(
        string ruc, string periodo, DateTime? desde,
        DateTime? hasta, int limite, int? usuarioId);

    Task<ReporteResponseDto> GetReportesPorSucursalAsync(
        int sucursalId, string periodo, DateTime? desde,
        DateTime? hasta, int limite, int? usuarioId);

    Task<List<ClienteExportDto>> GetClientesExportPorEmpresaAsync(
        string ruc, string periodo, DateTime? desde,
        DateTime? hasta, int? usuarioId);

    Task<List<ClienteExportDto>> GetClientesExportPorSucursalAsync(
        int sucursalId, string periodo, DateTime? desde,
        DateTime? hasta, int? usuarioId);

    // ── Listado comprobantes para reportes ────────────────────────────────────
    Task<IEnumerable<ListarComprobanteDTO>> GetListadoParaReportesAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null);

    // ── Productos top ─────────────────────────────────────────────────────────
    Task<IEnumerable<ProductoTopDTO>> GetProductosTopAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null,
        string orderBy = "monto");

    // ── Excel ─────────────────────────────────────────────────────────────────
    Task<byte[]> ExportarListadoReportesExcelAsync(
        string titulo,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null);

    Task<byte[]> ExportarProductosTopExcelAsync(
        string titulo,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null,
        string orderBy = "monto");

    Task<IEnumerable<MedioPagoTopDTO>> GetMediosPagoTopAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null);

    Task<byte[]> ExportarMediosPagoTopExcelAsync(
        string titulo,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null);
    }

public class ReportesService : IReportesService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IComprobanteExcelService _excelService;

    public ReportesService(IUnitOfWork unitOfWork, IComprobanteExcelService excelService)
    {
        _unitOfWork   = unitOfWork;
        _excelService = excelService;
    }

    public async Task<ReporteResponseDto> GetReportesPorEmpresaAsync(
        string ruc, string periodo, DateTime? desde,
        DateTime? hasta, int limite, int? usuarioId)
    {
        return await _unitOfWork.Reportes.GetReportesPorEmpresaAsync(
            ruc, periodo, desde, hasta, limite, usuarioId);
    }

    public async Task<ReporteResponseDto> GetReportesPorSucursalAsync(
        int sucursalId, string periodo, DateTime? desde,
        DateTime? hasta, int limite, int? usuarioId)
    {
        return await _unitOfWork.Reportes.GetReportesPorSucursalAsync(
            sucursalId, periodo, desde, hasta, limite, usuarioId);
    }

    public async Task<List<ClienteExportDto>> GetClientesExportPorEmpresaAsync(
        string ruc, string periodo, DateTime? desde,
        DateTime? hasta, int? usuarioId)
    {
        return await _unitOfWork.Reportes.GetClientesExportPorEmpresaAsync(
            ruc, periodo, desde, hasta, usuarioId);
    }

    public async Task<List<ClienteExportDto>> GetClientesExportPorSucursalAsync(
        int sucursalId, string periodo, DateTime? desde,
        DateTime? hasta, int? usuarioId)
    {
        return await _unitOfWork.Reportes.GetClientesExportPorSucursalAsync(
            sucursalId, periodo, desde, hasta, usuarioId);
    }

    // ── Listado comprobantes para reportes ────────────────────────────────────
    public async Task<IEnumerable<ListarComprobanteDTO>> GetListadoParaReportesAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null)
    {
        DateTime? desde = fechaDesde?.Date;
        DateTime? hasta = fechaDesde.HasValue
            ? (fechaHasta.HasValue
                ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
                : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1))
            : null;

        var comprobantes = await _unitOfWork.Reportes.GetListadoParaReportesAsync(
            ruc, codEstablecimiento, desde, hasta, usuarioCreacion, clienteNumDoc, limit);

        return comprobantes.Select(MapToListarDto);
    }

    // ── Productos top ─────────────────────────────────────────────────────────
    public async Task<IEnumerable<ProductoTopDTO>> GetProductosTopAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null,
        string orderBy = "monto")
    {
        DateTime? desde = fechaDesde?.Date;
        DateTime? hasta = fechaDesde.HasValue
            ? (fechaHasta.HasValue
                ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
                : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1))
            : null;

        return await _unitOfWork.Reportes.GetProductosTopAsync(
            ruc, codEstablecimiento, desde, hasta,
            usuarioCreacion, clienteNumDoc, limit, orderBy);
    }

    // ── Excel Listado ─────────────────────────────────────────────────────────
    public async Task<byte[]> ExportarListadoReportesExcelAsync(
        string titulo,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null)
    {
        var datos = await GetListadoParaReportesAsync(
            ruc, codEstablecimiento, fechaDesde, fechaHasta,
            usuarioCreacion, clienteNumDoc, limit);

        return await _excelService.ExportarListadoReportesAsync(
            titulo, datos, ruc, codEstablecimiento,
            fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
    }

    // ── Excel Productos Top ───────────────────────────────────────────────────
    public async Task<byte[]> ExportarProductosTopExcelAsync(
        string titulo,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null,
        string orderBy = "monto")
    {
        var datos = await GetProductosTopAsync(
            ruc, codEstablecimiento, fechaDesde, fechaHasta,
            usuarioCreacion, clienteNumDoc, limit, orderBy);

        return await _excelService.ExportarProductosTopAsync(
            titulo, datos, ruc, codEstablecimiento,
            fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
    }

    public async Task<byte[]> ExportarMediosPagoTopExcelAsync(
        string titulo,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null)
    {
        var datos = await GetMediosPagoTopAsync(
            ruc, codEstablecimiento, fechaDesde, fechaHasta,
            usuarioCreacion, clienteNumDoc, limit);

        return await _excelService.ExportarMediosPagoTopAsync(
            titulo, datos, ruc, codEstablecimiento,
            fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
    }

    public async Task<IEnumerable<MedioPagoTopDTO>> GetMediosPagoTopAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null)
    {
        DateTime? desde = fechaDesde?.Date;
        DateTime? hasta = fechaDesde.HasValue
            ? (fechaHasta.HasValue
                ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
                : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1))
            : null;

        return await _unitOfWork.Reportes.GetMediosPagoTopAsync(
            ruc, codEstablecimiento, desde, hasta, usuarioCreacion, clienteNumDoc, limit);
    }

    // ── Mapper ────────────────────────────────────────────────────────────────
    private static ListarComprobanteDTO MapToListarDto(Domain.Entities.Comprobante c) => new()
    {
        ComprobanteId   = c.ComprobanteId,
        TipoComprobante = c.TipoComprobante,
        NumeroCompleto  = c.NumeroCompleto ?? "",
        Serie           = c.Serie ?? "",
        Correlativo     = c.Correlativo?.ToString() ?? "",
        FechaEmision    = c.FechaEmision,
        TipoMoneda      = c.TipoMoneda ?? "PEN",
        ValorVenta      = c.ValorVenta ?? 0,
        TotalIGV        = c.TotalIGV ?? 0,
        ImporteTotal    = c.ImporteTotal ?? 0,
        EstadoSunat     = c.EstadoSunat,
        NumDocAfectado  = c.NumDocAfectado,
        TipoPago        = c.TipoPago,   
        Cliente = new ClienteDTO
        {
            RazonSocial     = c.ClienteRazonSocial,
            NumeroDocumento = c.ClienteNumDoc
        }
    };
}