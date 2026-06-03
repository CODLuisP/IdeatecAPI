using ClosedXML.Excel;
using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Reportes.DTOs;
using IdeatecAPI.Application.Features.Reportes.Services;
using IdeatecAPI.Application.Features.Trabajadores.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportesController : ControllerBase
{
    private readonly IReportesService _reportesService;
    private readonly ITrabajadorService _trabajadorService;
    private readonly ILogger<ReportesController> _logger;

    public ReportesController(
        IReportesService reportesService,
        ITrabajadorService trabajadorService,
        ILogger<ReportesController> logger)
    {
        _reportesService   = reportesService;
        _trabajadorService = trabajadorService;
        _logger            = logger;
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

    [HttpGet("control-caja/{ruc}/ticket-pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportarControlCajaTicketPdf(
        string ruc,
        [FromQuery] string titulo = "REPORTE DE CAJA",
        [FromQuery] string nombreResponsable = "",
        [FromQuery] string? codEstablecimiento = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] int? usuarioCreacion = null,
        [FromQuery] string? clienteNumDoc = null,
        [FromQuery] int? limit = null,
        [FromQuery] string? nombreUsuario = null)
    {
        try
        {
            var bytes = await _reportesService.ExportarControlCajaTicketPdfAsync(
                titulo, ruc, nombreResponsable, codEstablecimiento,
                fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc, limit, nombreUsuario);

            return File(bytes, "application/pdf",
                $"ticket-caja-{ruc}-{DateTime.Now:yyyyMMddHHmm}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar ticket PDF caja RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "Error al generar ticket PDF.", detalle = ex.Message });
        }
    }

    [HttpGet("control-caja/{ruc}/ticket-html")]
    [HttpGet("control-caja/{ruc}/ticket")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportarControlCajaTicketHtml(
        string ruc,
        [FromQuery] string titulo = "REPORTE DE CAJA",
        [FromQuery] string nombreResponsable = "",
        [FromQuery] string? codEstablecimiento = null,
        [FromQuery] DateTime? fechaDesde = null,
        [FromQuery] DateTime? fechaHasta = null,
        [FromQuery] int? usuarioCreacion = null,
        [FromQuery] string? clienteNumDoc = null,
        [FromQuery] int? limit = null,
        [FromQuery] string? nombreUsuario = null)
    {
        try
        {
            var html = await _reportesService.ExportarControlCajaTicketHtmlAsync(
                titulo, ruc, nombreResponsable, codEstablecimiento,
                fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc, limit, nombreUsuario);

            return Content(html, "text/html; charset=utf-8");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar ticket HTML caja RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "Error al generar ticket.", detalle = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VENTAS POR PRODUCTO (matriz diaria / semanal / quincenal / mensual)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Genera un Excel con la cantidad de servicios/productos vendidos por período.
    /// agrupacion: diario | mensual | semanal | quincenal
    ///   - diario    → requiere 'fecha' (yyyy-MM-dd). Una columna con el total del día.
    ///   - mensual   → requiere 'mes' y 'anio'. Columnas: 1..31 (cada día del mes).
    ///   - semanal   → requiere 'fecha' (inicio). Columnas: los 7 días desde esa fecha.
    ///   - quincenal → requiere 'mes', 'anio' y 'quincena' (1 o 2). Columnas: cada día de la quincena.
    /// </summary>
    [HttpGet("ventas-producto-excel/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetVentasProductoExcelAsync(
        int sucursalId,
        [FromQuery] string agrupacion  = "mensual",
        [FromQuery] string? fecha      = null,
        [FromQuery] int? mes           = null,
        [FromQuery] int? anio          = null,
        [FromQuery] int? quincena      = null)
    {
        // ── Validar agrupación ──────────────────────────────────────────────
        agrupacion = agrupacion.ToLower().Trim();
        var agrupacionesValidas = new[] { "diario", "mensual", "semanal", "quincenal" };
        if (!agrupacionesValidas.Contains(agrupacion))
            return BadRequest(new { mensaje = $"Agrupación inválida. Use: {string.Join(", ", agrupacionesValidas)}." });

        var cultura = System.Globalization.CultureInfo.InvariantCulture;
        DateTime fechaDesde, fechaHasta;
        string subtitulo;

        // ── Calcular rango según agrupación ────────────────────────────────
        if (agrupacion is "diario" or "semanal")
        {
            if (string.IsNullOrWhiteSpace(fecha) ||
                !DateTime.TryParseExact(fecha.Trim(), "yyyy-MM-dd", cultura,
                    System.Globalization.DateTimeStyles.None, out var fechaParsed))
                return BadRequest(new { mensaje = $"Para '{agrupacion}' se requiere 'fecha' en formato yyyy-MM-dd." });

            fechaDesde = fechaParsed.Date;
            fechaHasta = agrupacion == "diario" ? fechaDesde : fechaDesde.AddDays(6);
            subtitulo  = agrupacion == "diario"
                ? $"Día {fechaDesde:dd/MM/yyyy}"
                : $"Semana {fechaDesde:dd/MM/yyyy} – {fechaHasta:dd/MM/yyyy}";
        }
        else // mensual | quincenal
        {
            if (!mes.HasValue || !anio.HasValue)
                return BadRequest(new { mensaje = "Para esta agrupación se requieren 'mes' y 'anio'." });
            if (mes < 1 || mes > 12)
                return BadRequest(new { mensaje = "El parámetro 'mes' debe estar entre 1 y 12." });

            int diasEnMes = DateTime.DaysInMonth(anio.Value, mes.Value);

            if (agrupacion == "quincenal")
            {
                if (!quincena.HasValue || quincena is not (1 or 2))
                    return BadRequest(new { mensaje = "Para 'quincenal' se requiere 'quincena' con valor 1 o 2." });

                fechaDesde = quincena == 1
                    ? new DateTime(anio.Value, mes.Value, 1)
                    : new DateTime(anio.Value, mes.Value, 16);
                fechaHasta = quincena == 1
                    ? new DateTime(anio.Value, mes.Value, 15)
                    : new DateTime(anio.Value, mes.Value, diasEnMes);
                subtitulo = quincena == 1
                    ? $"{quincena}ra Quincena – {fechaDesde:MMMM yyyy}"
                    : $"{quincena}da Quincena – {fechaDesde:MMMM yyyy}";
            }
            else // mensual
            {
                fechaDesde = new DateTime(anio.Value, mes.Value, 1);
                fechaHasta = new DateTime(anio.Value, mes.Value, diasEnMes);
                subtitulo  = $"Mes {mes:D2} - {anio}";
            }
        }

        try
        {
            var filas = (await _trabajadorService.GetVentasPorDiaAsync(
                sucursalId, fechaDesde, fechaHasta)).ToList();

            if (filas.Count == 0)
                return NoContent();

            var productos = filas.Select(f => f.Descripcion!).Distinct().OrderBy(d => d).ToList();

            // Lookup (descripcion, dia) -> cantidad
            var lookup = filas.ToDictionary(
                f => (f.Descripcion!, f.Dia),
                f => f.SumaCantidad);

            // ── Paleta ────────────────────────────────────────────────────
            var azulOscuro = XLColor.FromHtml("#1F4E79");
            var azulMedio  = XLColor.FromHtml("#2E75B6");
            var azulClaro  = XLColor.FromHtml("#D6E4F0");
            var azulFila   = XLColor.FromHtml("#EBF3FB");
            var blanco     = XLColor.White;
            var verdeTotal = XLColor.FromHtml("#E2EFDA");

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Ventas por Producto");
            ws.ShowGridLines = false;

            // ── Definir columnas: (cabecera, día numérico) ────────────────
            // Para todos los casos generamos una columna por cada día del rango.
            // La cabecera muestra el número de día; el lookup usa DAY(fechaEmision).
            List<(string Cabecera, List<int> Dias)> columnas = Enumerable
                .Range(0, (fechaHasta - fechaDesde).Days + 1)
                .Select(offset =>
                {
                    var dia = fechaDesde.AddDays(offset);
                    return (dia.Day.ToString(), new List<int> { dia.Day });
                })
                .ToList();

            bool conTotal = agrupacion != "diario";
            int totalCols = 1 + columnas.Count + (conTotal ? 1 : 0);

            // ── Título ────────────────────────────────────────────────────
            string tituloTexto = "REPORTE VENTAS POR PRODUCTO";
            ws.Cell(1, 1).Value = tituloTexto;
            var tRange = ws.Range(1, 1, 1, totalCols);
            tRange.Merge();
            tRange.Style
                .Font.SetBold(true)
                .Font.SetFontSize(14)
                .Font.SetFontColor(azulOscuro)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // ── Subtítulo ─────────────────────────────────────────────────
            ws.Cell(2, 1).Value = subtitulo;
            var sRange = ws.Range(2, 1, 2, totalCols);
            sRange.Merge();
            sRange.Style
                .Font.SetBold(true)
                .Font.SetFontSize(11)
                .Font.SetFontColor(azulMedio)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // ── Cabecera ──────────────────────────────────────────────────
            int headerRow = 4;
            ws.Cell(headerRow, 1).Value = "Producto";
            for (int c = 0; c < columnas.Count; c++)
                ws.Cell(headerRow, c + 2).Value = columnas[c].Cabecera;
            if (conTotal)
                ws.Cell(headerRow, totalCols).Value = "TOTAL";

            var hRange = ws.Range(headerRow, 1, headerRow, totalCols);
            hRange.Style
                .Fill.SetBackgroundColor(azulMedio)
                .Font.SetFontColor(blanco)
                .Font.SetBold(true)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);
            ws.Row(headerRow).Height = 30;

            // ── Filas de datos ────────────────────────────────────────────
            for (int r = 0; r < productos.Count; r++)
            {
                int excelRow = headerRow + 1 + r;
                string prod = productos[r];
                var bgRow = r % 2 == 0 ? azulFila : blanco;

                // Nombre del producto
                ws.Cell(excelRow, 1).Value = prod;
                ws.Cell(excelRow, 1).Style
                    .Fill.SetBackgroundColor(azulClaro)
                    .Alignment.SetWrapText(true)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                decimal totalFila = 0;
                for (int c = 0; c < columnas.Count; c++)
                {
                    decimal suma = columnas[c].Dias
                        .Sum(dia => lookup.TryGetValue((prod, dia), out var v) ? v : 0);
                    totalFila += suma;

                    var cell = ws.Cell(excelRow, c + 2);
                    cell.Value = suma > 0 ? suma : (XLCellValue)"";
                    cell.Style
                        .Fill.SetBackgroundColor(bgRow)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                if (conTotal)
                {
                    var tc = ws.Cell(excelRow, totalCols);
                    tc.Value = totalFila;
                    tc.Style
                        .Fill.SetBackgroundColor(verdeTotal)
                        .Font.SetBold(true)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }
            }

            // ── Fila TOTAL por columna (solo cuando hay pivote) ───────────
            if (conTotal)
            {
                int totalRow = headerRow + productos.Count + 1;
                ws.Cell(totalRow, 1).Value = "TOTAL";
                ws.Cell(totalRow, 1).Style
                    .Fill.SetBackgroundColor(azulOscuro)
                    .Font.SetFontColor(blanco)
                    .Font.SetBold(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                decimal granTotal = 0;
                for (int c = 0; c < columnas.Count; c++)
                {
                    decimal sumaCol = productos.Sum(prod =>
                        columnas[c].Dias.Sum(dia =>
                            lookup.TryGetValue((prod, dia), out var v) ? v : 0));
                    granTotal += sumaCol;

                    var cell = ws.Cell(totalRow, c + 2);
                    cell.Value = sumaCol;
                    cell.Style
                        .Fill.SetBackgroundColor(azulOscuro)
                        .Font.SetFontColor(blanco)
                        .Font.SetBold(true)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                var gtCell = ws.Cell(totalRow, totalCols);
                gtCell.Value = granTotal;
                gtCell.Style
                    .Fill.SetBackgroundColor(azulOscuro)
                    .Font.SetFontColor(blanco)
                    .Font.SetBold(true)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            // ── Anchos ────────────────────────────────────────────────────
            ws.Column(1).Width = 38;
            for (int c = 2; c <= totalCols; c++)
                ws.Column(c).Width = agrupacion == "mensual" ? 5 : 14;

            ws.SheetView.FreezeRows(headerRow);

            // ── Exportar ──────────────────────────────────────────────────
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            string sufijo = agrupacion switch
            {
                "diario"    => fechaDesde.ToString("yyyyMMdd"),
                "semanal"   => $"{fechaDesde:yyyyMMdd}_{fechaHasta:yyyyMMdd}",
                "quincenal" => $"{anio}{mes:D2}_Q{quincena}",
                _           => $"{anio}{mes:D2}"
            };
            string fileName = $"ReporteVentasProducto_{agrupacion}_{sufijo}.xlsx";

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar Excel ventas-producto sucursal {SucursalId}", sucursalId);
            return StatusCode(500, new { mensaje = "Ocurrió un error al generar el Excel.", detalle = ex.Message });
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