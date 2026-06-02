using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Common.Interfaces.Persistence.Reportes;
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

    Task<byte[]> ExportarControlCajaExcelAsync(
        string titulo,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null);

    // ── PDF versions ──────────────────────────────────────────────────────────
    Task<byte[]> ExportarListadoPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null);

    Task<byte[]> ExportarProductosTopPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null, string orderBy = "monto");

    Task<byte[]> ExportarMediosPagoPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null);

    Task<byte[]> ExportarControlCajaPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null);

    // ── Ticket ────────────────────────────────────────────────────────────────
    Task<string> ExportarControlCajaTicketHtmlAsync(
        string titulo, string ruc,
        string nombreResponsable,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null,
        string? nombreUsuario = null);

    Task<byte[]> ExportarControlCajaTicketPdfAsync(
        string titulo, string ruc,
        string nombreResponsable,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null,
        string? nombreUsuario = null);
}

public class ReportesService : IReportesService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IComprobanteExcelService _excelService;
    private readonly IReportesPdfService _pdfService;
    private readonly IControlCajaTicketHtmlService _ticketHtmlService;

    public ReportesService(
        IUnitOfWork unitOfWork,
        IComprobanteExcelService excelService,
        IReportesPdfService pdfService,
        IControlCajaTicketHtmlService ticketHtmlService)
    {
        _unitOfWork         = unitOfWork;
        _excelService       = excelService;
        _pdfService         = pdfService;
        _ticketHtmlService  = ticketHtmlService;
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

    public async Task<byte[]> ExportarControlCajaExcelAsync(
        string titulo,
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

        var datos = await _unitOfWork.Reportes.GetListadoControlCajaAsync(
            ruc, codEstablecimiento, desde, hasta,
            usuarioCreacion, clienteNumDoc, limit);

        var dtos = datos.Select(MapToListarDto);

        return await _excelService.ExportarControlCajaAsync(
            titulo, dtos, ruc, codEstablecimiento,
            fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
    }

    // ── PDF Listado ───────────────────────────────────────────────────────────
    public async Task<byte[]> ExportarListadoPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null)
    {
        var datos = await GetListadoParaReportesAsync(
            ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc, limit);
        return await _pdfService.ExportarListadoPdfAsync(
            titulo, datos, ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
    }

    // ── PDF Productos Top ─────────────────────────────────────────────────────
    public async Task<byte[]> ExportarProductosTopPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null, string orderBy = "monto")
    {
        var datos = await GetProductosTopAsync(
            ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc, limit, orderBy);
        return await _pdfService.ExportarProductosTopPdfAsync(
            titulo, datos, ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
    }

    // ── PDF Medios de Pago ────────────────────────────────────────────────────
    public async Task<byte[]> ExportarMediosPagoPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null)
    {
        var datos = await GetMediosPagoTopAsync(
            ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc, limit);
        return await _pdfService.ExportarMediosPagoPdfAsync(
            titulo, datos, ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
    }

    // ── PDF Control de Caja ───────────────────────────────────────────────────
    public async Task<byte[]> ExportarControlCajaPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null)
    {
        DateTime? desde = fechaDesde?.Date;
        DateTime? hasta = fechaDesde.HasValue
            ? (fechaHasta.HasValue
                ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
                : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1))
            : null;

        var datos = await _unitOfWork.Reportes.GetListadoControlCajaAsync(
            ruc, codEstablecimiento, desde, hasta, usuarioCreacion, clienteNumDoc, limit);
        var dtos = datos.Select(MapToListarDto);

        return await _pdfService.ExportarControlCajaPdfAsync(
            titulo, dtos, ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
    }

    // ── Ticket HTML Control de Caja ───────────────────────────────────────────
    public async Task<string> ExportarControlCajaTicketHtmlAsync(
        string titulo, string ruc,
        string nombreResponsable,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null,
        string? nombreUsuario = null)
    {
        DateTime? desde = fechaDesde?.Date;
        DateTime? hasta = fechaDesde.HasValue
            ? (fechaHasta.HasValue
                ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
                : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1))
            : null;

        // 1. Comprobantes
        var comprobantes = (await _unitOfWork.Reportes.GetListadoControlCajaAsync(
            ruc, codEstablecimiento, desde, hasta, usuarioCreacion, clienteNumDoc, limit)).ToList();

        if (!comprobantes.Any())
            return await _ticketHtmlService.GenerarHtmlAsync(
                titulo, Enumerable.Empty<ControlCajaTicketItemDto>(),
                ruc, codEstablecimiento, fechaDesde, fechaHasta, nombreResponsable);

        // 2. Pagos
        var ids = comprobantes.Select(c => c.ComprobanteId);
        var pagos = (await _unitOfWork.Reportes.GetPagosByComprobanteIdsAsync(ids)).ToList();
        var pagosPorId = pagos
            .GroupBy(p => p.ComprobanteId)
            .ToDictionary(g => g.Key,
                g => g.Select(p => new PagoResumenDto { MedioPago = p.MedioPago, Monto = p.Monto }).ToList());

        // 3. Unir
        var items = comprobantes.Select(c => new ControlCajaTicketItemDto
        {
            ComprobanteId         = c.ComprobanteId,
            TipoComprobante       = c.TipoComprobante,
            Serie                 = c.Serie ?? "",
            Correlativo           = c.Correlativo,
            NumeroCompleto        = c.NumeroCompleto ?? "",
            FechaEmision          = c.FechaEmision,
            ImporteTotal          = c.ImporteTotal ?? 0,
            ValorVenta            = c.ValorVenta ?? 0,
            TotalIGV              = c.TotalIGV ?? 0,
            TipoMoneda            = c.TipoMoneda ?? "PEN",
            EstadoSunat           = c.EstadoSunat,
            ComprobanteAfectadoId = c.ComprobanteAfectadoId,
            NumDocAfectado        = c.NumDocAfectado,
            Pagos                 = pagosPorId.TryGetValue(c.ComprobanteId, out var p) ? p : new()
        });

        return await _ticketHtmlService.GenerarHtmlAsync(
            titulo, items, ruc, codEstablecimiento, fechaDesde, fechaHasta, nombreResponsable, nombreUsuario);
    }

    // ── Ticket PDF Control de Caja ────────────────────────────────────────────
    public async Task<byte[]> ExportarControlCajaTicketPdfAsync(
        string titulo, string ruc,
        string nombreResponsable,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null,
        string? nombreUsuario = null)
    {
        DateTime? desde = fechaDesde?.Date;
        DateTime? hasta = fechaDesde.HasValue
            ? (fechaHasta.HasValue
                ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
                : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1))
            : null;

        var empresa = await _unitOfWork.Empresas.GetEmpresaByRucAsync(ruc);

        var comprobantes = (await _unitOfWork.Reportes.GetListadoControlCajaAsync(
            ruc, codEstablecimiento, desde, hasta, usuarioCreacion, clienteNumDoc, limit)).ToList();

        var items = Enumerable.Empty<ControlCajaTicketItemDto>();

        if (comprobantes.Any())
        {
            var ids   = comprobantes.Select(c => c.ComprobanteId);
            var pagos = (await _unitOfWork.Reportes.GetPagosByComprobanteIdsAsync(ids)).ToList();
            var pagosPorId = pagos
                .GroupBy(p => p.ComprobanteId)
                .ToDictionary(g => g.Key,
                    g => g.Select(p => new PagoResumenDto { MedioPago = p.MedioPago, Monto = p.Monto }).ToList());

            items = comprobantes.Select(c => new ControlCajaTicketItemDto
            {
                ComprobanteId         = c.ComprobanteId,
                TipoComprobante       = c.TipoComprobante,
                Serie                 = c.Serie ?? "",
                Correlativo           = c.Correlativo,
                NumeroCompleto        = c.NumeroCompleto ?? "",
                FechaEmision          = c.FechaEmision,
                ImporteTotal          = c.ImporteTotal ?? 0,
                ValorVenta            = c.ValorVenta ?? 0,
                TotalIGV              = c.TotalIGV ?? 0,
                TipoMoneda            = c.TipoMoneda ?? "PEN",
                EstadoSunat           = c.EstadoSunat,
                ComprobanteAfectadoId = c.ComprobanteAfectadoId,
                NumDocAfectado        = c.NumDocAfectado,
                Pagos                 = pagosPorId.TryGetValue(c.ComprobanteId, out var p) ? p : new()
            });
        }

        return await _pdfService.ExportarControlCajaTicketPdfAsync(
            titulo, items, ruc,
            codEstablecimiento, fechaDesde, fechaHasta,
            nombreResponsable,
            empresa?.NombreComercial ?? empresa?.RazonSocial,
            empresa?.Direccion,
            empresa?.LogoBase64,
            nombreUsuario);
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
        EstadoSunat          = c.EstadoSunat,
        NumDocAfectado       = c.NumDocAfectado,
        ComprobanteAfectadoId = c.ComprobanteAfectadoId,
        TipoPago             = c.TipoPago,
        Cliente = new ClienteDTO
        {
            RazonSocial     = c.ClienteRazonSocial,
            NumeroDocumento = c.ClienteNumDoc
        }
    };
}