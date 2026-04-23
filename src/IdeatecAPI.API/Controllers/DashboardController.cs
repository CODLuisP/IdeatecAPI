using IdeatecAPI.Application.Features.Dashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    // GET api/dashboard/empresa/{ruc}?desde=2025-01-01&hasta=2025-01-31&limite=10
    [HttpGet("empresa/{ruc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboardPorEmpresa(
        string ruc,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int limite = 10)
    {
        try
        {
            var result = await _dashboardService.GetDashboardPorEmpresaAsync(ruc, desde, hasta, limite);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dashboard para RUC {Ruc}", ruc);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el dashboard.",
                detalle = ex.Message
            });
        }
    }

    // GET api/dashboard/sucursal/{sucursalId}?desde=2025-01-01&hasta=2025-01-31&limite=10
    [HttpGet("sucursal/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboardPorSucursal(
        int sucursalId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int limite = 10)
    {
        try
        {
            var result = await _dashboardService.GetDashboardPorSucursalAsync(sucursalId, desde, hasta, limite);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Sucursal no encontrada: {SucursalId}", sucursalId);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dashboard para sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el dashboard.",
                detalle = ex.Message
            });
        }
    }
}