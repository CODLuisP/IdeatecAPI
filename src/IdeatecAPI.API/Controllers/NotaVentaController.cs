using IdeatecAPI.Application.Features.NotaVenta.DTOs;
using IdeatecAPI.Application.Features.NotaVenta.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotaVentaController : ControllerBase
{
    private readonly INotaVentaService _notaVentaService;
    private readonly ILogger<NotaVentaController> _logger;

    public NotaVentaController(INotaVentaService notaVentaService, ILogger<NotaVentaController> logger)
    {
        _notaVentaService = notaVentaService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerarNotaVenta([FromBody] GenerarNotaVentaDTO dto)
    {
        try
        {
            var resultado = await _notaVentaService.GenerarNotaVentaAsync(dto);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar nota de venta");
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error interno al generar la nota de venta.", detalle = ex.Message });
        }
    }

    [HttpPatch("{id}/anular")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AnularNotaVenta(int id, [FromBody] AnularNotaVentaDTO dto)
    {
        try
        {
            var resultado = await _notaVentaService.AnularNotaVentaAsync(id, dto.Motivo, dto.UsuarioId);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular nota de venta {ComprobanteId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error interno al anular la nota de venta.", detalle = ex.Message });
        }
    }

    [HttpPatch("{id}/detalle/{detalleId}/devolver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DevolverItemNotaVenta(int id, int detalleId, [FromBody] DevolverItemNotaVentaDTO dto)
    {
        try
        {
            var resultado = await _notaVentaService.DevolverItemNotaVentaAsync(id, detalleId, dto.Cantidad, dto.Motivo, dto.UsuarioId);
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al devolver artículo {DetalleId} de la nota de venta {ComprobanteId}", detalleId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error interno al devolver el artículo.", detalle = ex.Message });
        }
    }

    [HttpGet("sucursal/{sucursalId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListarNotasVenta(
        int sucursalId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int? limit,
        [FromQuery] int? offset)
    {
        try
        {
            var lista = await _notaVentaService.ListarNotasVentaAsync(sucursalId, fechaDesde, fechaHasta, limit, offset);
            return Ok(lista);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al listar notas de venta para sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error interno al listar notas de venta.", detalle = ex.Message });
        }
    }
}
