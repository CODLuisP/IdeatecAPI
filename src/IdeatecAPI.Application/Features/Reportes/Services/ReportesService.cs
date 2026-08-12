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
        int? limit = null,
        string filtroNV = "excluir");

    // ── Productos top ─────────────────────────────────────────────────────────
    Task<IEnumerable<ProductoTopDTO>> GetProductosTopAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null,
        string orderBy = "monto",
        string filtroNV = "excluir");

    // ── Excel ─────────────────────────────────────────────────────────────────
    Task<byte[]> ExportarListadoReportesExcelAsync(
        string titulo,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null,
        string filtroNV = "excluir");

    Task<byte[]> ExportarProductosTopExcelAsync(
        string titulo,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null,
        string orderBy = "monto",
        string filtroNV = "excluir");

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

    //usado actualemente en produccion en control de caja
    Task<byte[]> ExportarControlCajaExcelAsync(
        string titulo,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null,
        string filtroNV = "excluir");

    // ── PDF versions ──────────────────────────────────────────────────────────
    Task<byte[]> ExportarListadoPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null,
        string filtroNV = "excluir");

    Task<byte[]> ExportarProductosTopPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null, string orderBy = "monto",
        string filtroNV = "excluir");

    Task<byte[]> ExportarMediosPagoPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null);

    //usado actualemente en produccion en control de caja
    Task<byte[]> ExportarControlCajaPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null,
        string filtroNV = "excluir");

    // ── Ticket ────────────────────────────────────────────────────────────────
    Task<string> ExportarControlCajaTicketHtmlAsync(
        string titulo, string ruc,
        string nombreResponsable,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null,
        string? nombreUsuario = null,
        string filtroNV = "excluir");

    Task<byte[]> ExportarControlCajaTicketPdfAsync(
        string titulo, string ruc,
        string nombreResponsable,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null,
        string? nombreUsuario = null,
        string filtroNV = "excluir");
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
        int? limit = null,
        string filtroNV = "excluir")
    {
        DateTime? desde = fechaDesde?.Date;
        DateTime? hasta = fechaDesde.HasValue
            ? (fechaHasta.HasValue
                ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
                : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1))
            : null;

        var comprobantes = await _unitOfWork.Reportes.GetListadoParaReportesAsync(
            ruc, codEstablecimiento, desde, hasta, usuarioCreacion, clienteNumDoc, limit, filtroNV);

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
        string orderBy = "monto",
        string filtroNV = "excluir")
    {
        DateTime? desde = fechaDesde?.Date;
        DateTime? hasta = fechaDesde.HasValue
            ? (fechaHasta.HasValue
                ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
                : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1))
            : null;

        return await _unitOfWork.Reportes.GetProductosTopAsync(
            ruc, codEstablecimiento, desde, hasta,
            usuarioCreacion, clienteNumDoc, limit, orderBy, filtroNV);
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
        int? limit = null,
        string filtroNV = "excluir")
    {
        var datos = await GetListadoParaReportesAsync(
            ruc, codEstablecimiento, fechaDesde, fechaHasta,
            usuarioCreacion, clienteNumDoc, limit, filtroNV);

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
        string orderBy = "monto",
        string filtroNV = "excluir")
    {
        var datos = await GetProductosTopAsync(
            ruc, codEstablecimiento, fechaDesde, fechaHasta,
            usuarioCreacion, clienteNumDoc, limit, orderBy, filtroNV);

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
        int? limit = null,
        string filtroNV = "excluir")
    {
        DateTime? desde = fechaDesde?.Date;
        DateTime? hasta = fechaDesde.HasValue
            ? (fechaHasta.HasValue
                ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
                : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1))
            : null;

        var datos = await _unitOfWork.Reportes.GetListadoControlCajaAsync(
            ruc, codEstablecimiento, desde, hasta,
            usuarioCreacion, clienteNumDoc, limit, filtroNV);

        var dtos = PrepararDtosControlCaja(datos);

        return await _excelService.ExportarControlCajaAsync(
            titulo, dtos, ruc, codEstablecimiento,
            fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
    }

    // ── PDF Listado ───────────────────────────────────────────────────────────
    public async Task<byte[]> ExportarListadoPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null,
        string filtroNV = "excluir")
    {
        var datos = await GetListadoParaReportesAsync(
            ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc, limit, filtroNV);
        return await _pdfService.ExportarListadoPdfAsync(
            titulo, datos, ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
    }

    // ── PDF Productos Top ─────────────────────────────────────────────────────
    public async Task<byte[]> ExportarProductosTopPdfAsync(
        string titulo, string ruc,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null, string orderBy = "monto",
        string filtroNV = "excluir")
    {
        var datos = await GetProductosTopAsync(
            ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc, limit, orderBy, filtroNV);
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
        string? clienteNumDoc = null, int? limit = null,
        string filtroNV = "excluir")
    {
        DateTime? desde = fechaDesde?.Date;
        DateTime? hasta = fechaDesde.HasValue
            ? (fechaHasta.HasValue
                ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
                : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1))
            : null;

        var datos = await _unitOfWork.Reportes.GetListadoControlCajaAsync(
            ruc, codEstablecimiento, desde, hasta, usuarioCreacion, clienteNumDoc, limit, filtroNV);
        var dtos = PrepararDtosControlCaja(datos);

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
        string? nombreUsuario = null,
        string filtroNV = "excluir")
    {
        DateTime? desde = fechaDesde?.Date;
        DateTime? hasta = fechaDesde.HasValue
            ? (fechaHasta.HasValue
                ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
                : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1))
            : null;

        // 1. Comprobantes
        var comprobantes = (await _unitOfWork.Reportes.GetListadoControlCajaAsync(
            ruc, codEstablecimiento, desde, hasta, usuarioCreacion, clienteNumDoc, limit, filtroNV)).ToList();

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
        var itemsTicket = comprobantes.Select(c =>
        {
            var esRechazado = c.EstadoSunat == "RECHAZADO";
            return new ControlCajaTicketItemDto
            {
                ComprobanteId         = c.ComprobanteId,
                TipoComprobante       = c.TipoComprobante,
                Serie                 = c.Serie ?? "",
                Correlativo           = c.Correlativo,
                NumeroCompleto        = c.NumeroCompleto ?? "",
                FechaEmision          = c.FechaEmision,
                ImporteTotal          = esRechazado ? 0 : c.ImporteTotal ?? 0,
                ValorVenta            = esRechazado ? 0 : c.ValorVenta ?? 0,
                TotalIGV              = esRechazado ? 0 : c.TotalIGV ?? 0,
                TipoMoneda            = c.TipoMoneda ?? "PEN",
                TipoCambio            = esRechazado ? 0 : c.TipoCambio ?? 0,
                EstadoSunat           = c.EstadoSunat,
                ComprobanteAfectadoId = c.ComprobanteAfectadoId,
                NumDocAfectado        = c.NumDocAfectado,
                TotalComisionPagoTarjeta = esRechazado ? null : c.TotalComisionPagoTarjeta,
                Pagos                 = esRechazado ? new() : (pagosPorId.TryGetValue(c.ComprobanteId, out var p) ? p : new())
            };
        }).ToList();

        HeredarTipoCambioTicket(itemsTicket);

        return await _ticketHtmlService.GenerarHtmlAsync(
            titulo, itemsTicket, ruc, codEstablecimiento, fechaDesde, fechaHasta, nombreResponsable, nombreUsuario);
    }

    // ── Ticket PDF Control de Caja ────────────────────────────────────────────
    public async Task<byte[]> ExportarControlCajaTicketPdfAsync(
        string titulo, string ruc,
        string nombreResponsable,
        string? codEstablecimiento = null, DateTime? fechaDesde = null,
        DateTime? fechaHasta = null, int? usuarioCreacion = null,
        string? clienteNumDoc = null, int? limit = null,
        string? nombreUsuario = null,
        string filtroNV = "excluir")
    {
        DateTime? desde = fechaDesde?.Date;
        DateTime? hasta = fechaDesde.HasValue
            ? (fechaHasta.HasValue
                ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
                : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1))
            : null;

        var empresa = await _unitOfWork.Empresas.GetEmpresaByRucAsync(ruc);

        var comprobantes = (await _unitOfWork.Reportes.GetListadoControlCajaAsync(
            ruc, codEstablecimiento, desde, hasta, usuarioCreacion, clienteNumDoc, limit, filtroNV)).ToList();

        var items = Enumerable.Empty<ControlCajaTicketItemDto>();

        if (comprobantes.Any())
        {
            var ids   = comprobantes.Select(c => c.ComprobanteId);
            var pagos = (await _unitOfWork.Reportes.GetPagosByComprobanteIdsAsync(ids)).ToList();
            var pagosPorId = pagos
                .GroupBy(p => p.ComprobanteId)
                .ToDictionary(g => g.Key,
                    g => g.Select(p => new PagoResumenDto { MedioPago = p.MedioPago, Monto = p.Monto }).ToList());

            var itemsPdf = comprobantes.Select(c =>
            {
                var esRechazado = c.EstadoSunat == "RECHAZADO";
                return new ControlCajaTicketItemDto
                {
                    ComprobanteId         = c.ComprobanteId,
                    TipoComprobante       = c.TipoComprobante,
                    Serie                 = c.Serie ?? "",
                    Correlativo           = c.Correlativo,
                    NumeroCompleto        = c.NumeroCompleto ?? "",
                    FechaEmision          = c.FechaEmision,
                    ImporteTotal          = esRechazado ? 0 : c.ImporteTotal ?? 0,
                    ValorVenta            = esRechazado ? 0 : c.ValorVenta ?? 0,
                    TotalIGV              = esRechazado ? 0 : c.TotalIGV ?? 0,
                    TipoMoneda            = c.TipoMoneda ?? "PEN",
                    TipoCambio            = esRechazado ? 0 : c.TipoCambio ?? 0,
                    EstadoSunat           = c.EstadoSunat,
                    ComprobanteAfectadoId = c.ComprobanteAfectadoId,
                    NumDocAfectado        = c.NumDocAfectado,
                    TotalComisionPagoTarjeta = esRechazado ? null : c.TotalComisionPagoTarjeta,
                    Pagos                 = esRechazado ? new() : (pagosPorId.TryGetValue(c.ComprobanteId, out var p) ? p : new())
                };
            }).ToList();

            HeredarTipoCambioTicket(itemsPdf);
            items = itemsPdf;
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
        TipoCambio      = c.TipoCambio ?? 0,
        ValorVenta      = c.ValorVenta ?? 0,
        TotalIGV        = c.TotalIGV ?? 0,
        ImporteTotal    = c.ImporteTotal ?? 0,
        TotalComisionPagoTarjeta = c.TotalComisionPagoTarjeta,
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

    // Para Control de Caja: los RECHAZADOS se muestran (visibilidad de correlativo)
    // pero con todos sus montos en cero, ya que no tienen efecto contable.
    private static ListarComprobanteDTO ZerarMontosSiRechazado(ListarComprobanteDTO d)
    {
        if (d.EstadoSunat != "RECHAZADO") return d;

        d.TipoCambio   = 0;
        d.ValorVenta   = 0;
        d.TotalIGV     = 0;
        d.ImporteTotal = 0;
        d.MontoCredito = 0;
        d.TotalComisionPagoTarjeta = null;
        return d;
    }

    // Hereda TipoCambio del doc afectado para notas de ticket en USD sin TC propio.
    private static void HeredarTipoCambioTicket(List<ControlCajaTicketItemDto> lista)
    {
        var tcPorNumero = lista
            .Where(x => x.TipoMoneda == "USD" && x.TipoCambio > 0 && !string.IsNullOrEmpty(x.NumeroCompleto))
            .ToDictionary(x => x.NumeroCompleto.Trim().ToUpper(), x => x.TipoCambio);

        foreach (var dto in lista)
        {
            if ((dto.TipoComprobante == "07" || dto.TipoComprobante == "08")
                && dto.TipoMoneda == "USD"
                && dto.TipoCambio == 0
                && !string.IsNullOrEmpty(dto.NumDocAfectado)
                && tcPorNumero.TryGetValue(dto.NumDocAfectado.Trim().ToUpper(), out var tc))
            {
                dto.TipoCambio = tc;
            }
        }
    }

    // Mapea, zera rechazados y hereda TipoCambio del doc afectado en notas USD sin TC.
    private static List<ListarComprobanteDTO> PrepararDtosControlCaja(
        IEnumerable<Domain.Entities.Comprobante> datos)
    {
        var lista = datos.Select(MapToListarDto).Select(ZerarMontosSiRechazado).ToList();

        // Índice de TC por número de comprobante (solo documentos USD con TC válido)
        var tcPorNumero = lista
            .Where(x => x.TipoMoneda == "USD" && x.TipoCambio > 0 && !string.IsNullOrEmpty(x.NumeroCompleto))
            .ToDictionary(x => x.NumeroCompleto.Trim().ToUpper(), x => x.TipoCambio);

        // Notas en USD sin TC propio: hereda el TC del documento afectado
        foreach (var dto in lista)
        {
            if ((dto.TipoComprobante == "07" || dto.TipoComprobante == "08")
                && dto.TipoMoneda == "USD"
                && dto.TipoCambio == 0
                && !string.IsNullOrEmpty(dto.NumDocAfectado)
                && tcPorNumero.TryGetValue(dto.NumDocAfectado.Trim().ToUpper(), out var tc))
            {
                dto.TipoCambio = tc;
            }
        }

        return lista;
    }
}