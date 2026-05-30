using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Reportes.DTOs;
using IdeatecAPI.Application.Features.Reportes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly IReportesService _reportesService;
    private readonly ILogger<ReportesController> _logger;

    public ReportesController(IReportesService reportesService, ILogger<ReportesController> logger)
    {
        _reportesService = reportesService;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POR EMPRESA (RUC)
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("empresa/{ruc}")]
    [ProducesResponseType(typeof(ReporteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetReportesPorEmpresa(
        string ruc,
        [FromQuery] string periodo = "hoy",
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] int limite = 10,
        [FromQuery] int? usuarioId = null)
    {
        if (!PeriodoValido(periodo, desde, hasta, out var error))
            return BadRequest(new { mensaje = error });

        try
        {
            var result = await _reportesService.GetReportesPorEmpresaAsync(
                ruc, periodo, desde, hasta, limite, usuarioId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reportes para RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "Ocurrió un error al obtener los reportes.", detalle = ex.Message });
        }
    }

    [HttpGet("empresa/{ruc}/export")]
    [ProducesResponseType(typeof(List<ClienteExportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetExportPorEmpresa(
        string ruc,
        [FromQuery] string periodo = "hoy",
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] int? usuarioId = null)
    {
        if (!PeriodoValido(periodo, desde, hasta, out var error))
            return BadRequest(new { mensaje = error });

        try
        {
            var result = await _reportesService.GetClientesExportPorEmpresaAsync(
                ruc, periodo, desde, hasta, usuarioId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar clientes para RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "Ocurrió un error al exportar los datos.", detalle = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POR SUCURSAL
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("sucursal/{sucursalId:int}")]
    [ProducesResponseType(typeof(ReporteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetReportesPorSucursal(
        int sucursalId,
        [FromQuery] string periodo = "hoy",
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] int limite = 10,
        [FromQuery] int? usuarioId = null)
    {
        if (!PeriodoValido(periodo, desde, hasta, out var error))
            return BadRequest(new { mensaje = error });

        try
        {
            var result = await _reportesService.GetReportesPorSucursalAsync(
                sucursalId, periodo, desde, hasta, limite, usuarioId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reportes para sucursal {SucursalId}", sucursalId);
            return StatusCode(500, new { mensaje = "Ocurrió un error al obtener los reportes.", detalle = ex.Message });
        }
    }

    [HttpGet("sucursal/{sucursalId:int}/export")]
    [ProducesResponseType(typeof(List<ClienteExportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetExportPorSucursal(
        int sucursalId,
        [FromQuery] string periodo = "hoy",
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] int? usuarioId = null)
    {
        if (!PeriodoValido(periodo, desde, hasta, out var error))
            return BadRequest(new { mensaje = error });

        try
        {
            var result = await _reportesService.GetClientesExportPorSucursalAsync(
                sucursalId, periodo, desde, hasta, usuarioId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar clientes para sucursal {SucursalId}", sucursalId);
            return StatusCode(500, new { mensaje = "Ocurrió un error al exportar los datos.", detalle = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LISTADO COMPROBANTES
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("listado/{ruc}")]
    [ProducesResponseType(typeof(IEnumerable<ListarComprobanteDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetListado(
        string ruc,
        [FromQuery] string? codEstablecimiento = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] int? usuarioCreacion = null,
        [FromQuery] string? clienteNumDoc = null,
        [FromQuery] int? limit = null)
    {
        try
        {
            var result = await _reportesService.GetListadoParaReportesAsync(
                ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc, limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener listado reportes RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "Error al obtener listado.", detalle = ex.Message });
        }
    }

    /// <param name="formato">excel (default) | pdf</param>
    [HttpGet("listado/{ruc}/excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportarListadoExcel(
        string ruc,
        [FromQuery] string titulo = "Reporte de Comprobantes",
        [FromQuery] string? codEstablecimiento = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] int? usuarioCreacion = null,
        [FromQuery] string? clienteNumDoc = null,
        [FromQuery] int? limit = null,
        [FromQuery] string formato = "excel")
    {
        try
        {
            if (formato.ToLower() == "pdf")
            {
                var pdf = await _reportesService.ExportarListadoPdfAsync(
                    titulo, ruc, codEstablecimiento, fechaDesde, fechaHasta,
                    usuarioCreacion, clienteNumDoc, limit);
                return File(pdf, "application/pdf", $"comprobantes-{ruc}-{DateTime.Now:yyyyMMdd}.pdf");
            }

            var bytes = await _reportesService.ExportarListadoReportesExcelAsync(
                titulo, ruc, codEstablecimiento, fechaDesde, fechaHasta,
                usuarioCreacion, clienteNumDoc, limit);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"comprobantes-{ruc}-{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar listado RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "Error al generar archivo.", detalle = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRODUCTOS TOP
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("productos-top/{ruc}")]
    [ProducesResponseType(typeof(IEnumerable<ProductoTopDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProductosTop(
        string ruc,
        [FromQuery] string? codEstablecimiento = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] int? usuarioCreacion = null,
        [FromQuery] string? clienteNumDoc = null,
        [FromQuery] int? limit = null,
        [FromQuery] string orderBy = "monto")
    {
        try
        {
            var result = await _reportesService.GetProductosTopAsync(
                ruc, codEstablecimiento, fechaDesde, fechaHasta,
                usuarioCreacion, clienteNumDoc, limit, orderBy);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener productos top RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "Error al obtener productos.", detalle = ex.Message });
        }
    }

    /// <param name="formato">excel (default) | pdf</param>
    [HttpGet("productos-top/{ruc}/excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportarProductosTopExcel(
        string ruc,
        [FromQuery] string titulo = "Top Productos",
        [FromQuery] string? codEstablecimiento = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] int? usuarioCreacion = null,
        [FromQuery] string? clienteNumDoc = null,
        [FromQuery] int? limit = null,
        [FromQuery] string orderBy = "monto",
        [FromQuery] string formato = "excel")
    {
        try
        {
            if (formato.ToLower() == "pdf")
            {
                var pdf = await _reportesService.ExportarProductosTopPdfAsync(
                    titulo, ruc, codEstablecimiento, fechaDesde, fechaHasta,
                    usuarioCreacion, clienteNumDoc, limit, orderBy);
                return File(pdf, "application/pdf", $"productos-top-{ruc}-{DateTime.Now:yyyyMMdd}.pdf");
            }

            var bytes = await _reportesService.ExportarProductosTopExcelAsync(
                titulo, ruc, codEstablecimiento, fechaDesde, fechaHasta,
                usuarioCreacion, clienteNumDoc, limit, orderBy);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"productos-top-{ruc}-{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar productos top RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "Error al generar archivo.", detalle = ex.Message });
        }
    }

    [HttpGet("medios-pago/{ruc}")]
    [ProducesResponseType(typeof(IEnumerable<MedioPagoTopDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMediosPagoTop(
        string ruc,
        [FromQuery] string? codEstablecimiento = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] int? usuarioCreacion = null,
        [FromQuery] string? clienteNumDoc = null,
        [FromQuery] int? limit = null)
    {
        try
        {
            var result = await _reportesService.GetMediosPagoTopAsync(
                ruc, codEstablecimiento, fechaDesde, fechaHasta,
                usuarioCreacion, clienteNumDoc, limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener medios de pago RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "Error al obtener medios de pago.", detalle = ex.Message });
        }
    }

    /// <param name="formato">excel (default) | pdf</param>
    [HttpGet("medios-pago/{ruc}/excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportarMediosPagoTopExcel(
        string ruc,
        [FromQuery] string titulo = "Top Medios de Pago",
        [FromQuery] string? codEstablecimiento = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] int? usuarioCreacion = null,
        [FromQuery] string? clienteNumDoc = null,
        [FromQuery] int? limit = null,
        [FromQuery] string formato = "excel")
    {
        try
        {
            if (formato.ToLower() == "pdf")
            {
                var pdf = await _reportesService.ExportarMediosPagoPdfAsync(
                    titulo, ruc, codEstablecimiento, fechaDesde, fechaHasta,
                    usuarioCreacion, clienteNumDoc, limit);
                return File(pdf, "application/pdf", $"medios-pago-{ruc}-{DateTime.Now:yyyyMMdd}.pdf");
            }

            var bytes = await _reportesService.ExportarMediosPagoTopExcelAsync(
                titulo, ruc, codEstablecimiento, fechaDesde, fechaHasta,
                usuarioCreacion, clienteNumDoc, limit);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"medios-pago-{ruc}-{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar medios de pago RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "Error al generar archivo.", detalle = ex.Message });
        }
    }

    /// <param name="formato">excel (default) | pdf</param>
    [HttpGet("control-caja/{ruc}/excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportarControlCajaExcel(
        string ruc,
        [FromQuery] string titulo = "Control de Caja",
        [FromQuery] string? codEstablecimiento = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] int? usuarioCreacion = null,
        [FromQuery] string? clienteNumDoc = null,
        [FromQuery] int? limit = null,
        [FromQuery] string formato = "excel")
    {
        try
        {
            if (formato.ToLower() == "pdf")
            {
                var pdf = await _reportesService.ExportarControlCajaPdfAsync(
                    titulo, ruc, codEstablecimiento, fechaDesde, fechaHasta,
                    usuarioCreacion, clienteNumDoc, limit);
                return File(pdf, "application/pdf", $"control-caja-{ruc}-{DateTime.Now:yyyyMMdd}.pdf");
            }

            var bytes = await _reportesService.ExportarControlCajaExcelAsync(
                titulo, ruc, codEstablecimiento, fechaDesde, fechaHasta,
                usuarioCreacion, clienteNumDoc, limit);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"control-caja-{ruc}-{DateTime.Now:yyyyMMdd}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar control de caja RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "Error al generar archivo.", detalle = ex.Message });
        }
    }

    [HttpGet("control-caja/{ruc}/ticket")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportarControlCajaTicket(
        string ruc,
        [FromQuery] string titulo = "REPORTE DE CAJA",
        [FromQuery] string nombreResponsable = "",
        [FromQuery] string? codEstablecimiento = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] int? usuarioCreacion = null,
        [FromQuery] string? clienteNumDoc = null,
        [FromQuery] int? limit = null)
    {
        try
        {
            var pdf = await _reportesService.ExportarControlCajaTicketAsync(
                titulo, ruc, nombreResponsable, codEstablecimiento,
                fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc, limit);

            return File(pdf, "application/pdf",
                $"ticket-caja-{ruc}-{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar ticket caja RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "Error al generar ticket.", detalle = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPER
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly string[] PeriodosValidos =
        { "hoy", "semana", "mes", "año", "personalizado" };

    private static bool PeriodoValido(
        string periodo, DateTime? desde, DateTime? hasta, out string error)
    {
        error = string.Empty;

        if (!PeriodosValidos.Contains(periodo.ToLower()))
        {
            error = $"Periodo '{periodo}' no válido. Use: hoy, semana, mes, año, personalizado.";
            return false;
        }

        if (periodo.ToLower() == "personalizado")
        {
            if (!desde.HasValue || !hasta.HasValue)
            {
                error = "Para periodo 'personalizado' se requieren los parámetros 'desde' y 'hasta'.";
                return false;
            }
            if (desde > hasta)
            {
                error = "La fecha 'desde' no puede ser mayor que 'hasta'.";
                return false;
            }
        }

        return true;
    }
}