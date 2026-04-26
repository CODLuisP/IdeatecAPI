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

    /// <summary>
    /// GET api/reportes/empresa/{ruc}?periodo=hoy&desde=&hasta=&limite=10&usuarioId=
    /// </summary>
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
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los reportes.",
                detalle = ex.Message
            });
        }
    }

    /// <summary>
    /// GET api/reportes/empresa/{ruc}/export?periodo=hoy&desde=&hasta=&usuarioId=
    /// Sin límite — para exportar Excel completo
    /// </summary>
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
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al exportar los datos.",
                detalle = ex.Message
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POR SUCURSAL
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// GET api/reportes/sucursal/{sucursalId}?periodo=hoy&desde=&hasta=&limite=10&usuarioId=
    /// </summary>
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
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los reportes.",
                detalle = ex.Message
            });
        }
    }

    /// <summary>
    /// GET api/reportes/sucursal/{sucursalId}/export?periodo=hoy&desde=&hasta=&usuarioId=
    /// Sin límite — para exportar Excel completo
    /// </summary>
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
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al exportar los datos.",
                detalle = ex.Message
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPER — validación de periodo
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