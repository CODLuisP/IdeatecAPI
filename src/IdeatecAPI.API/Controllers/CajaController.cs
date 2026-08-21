using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IdeatecAPI.Application.Features.Caja.DTOs;
using IdeatecAPI.Application.Features.Caja.Services;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CajaController : ControllerBase
{
    private readonly ICajaService _cajaService;
    private readonly ILogger<CajaController> _logger;

    public CajaController(ICajaService cajaService, ILogger<CajaController> logger)
    {
        _cajaService = cajaService;
        _logger = logger;
    }

    /// <summary>Estado de la caja de una sucursal para el usuario autenticado.</summary>
    [HttpGet("estado/{sucursalId:int}")]
    public async Task<IActionResult> GetEstado(int sucursalId)
    {
        try
        {
            return Ok(await _cajaService.GetEstadoAsync(sucursalId, UsuarioId()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo estado de caja de la sucursal {SucursalId}", sucursalId);
            return StatusCode(500, new { mensaje = "No se pudo obtener el estado de la caja", detalle = ex.Message });
        }
    }

    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir([FromBody] AbrirCajaDto dto)
    {
        try
        {
            return Ok(await _cajaService.AbrirCajaAsync(dto, UsuarioId(), NombreUsuario()));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abriendo caja en la sucursal {SucursalId}", dto.SucursalId);
            return StatusCode(500, new { mensaje = "No se pudo abrir la caja", detalle = ex.Message });
        }
    }

    /// <summary>
    /// Inicia el turno del usuario en una caja ya abierta. Es idempotente: si
    /// ya tiene turno abierto devuelve el estado actual sin crear otro.
    /// </summary>
    [HttpPost("turno/iniciar")]
    public async Task<IActionResult> IniciarTurno([FromBody] IniciarTurnoDto dto)
    {
        try
        {
            return Ok(await _cajaService.IniciarTurnoAsync(dto.SucursalId, UsuarioId(), NombreUsuario()));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error iniciando turno en la sucursal {SucursalId}", dto.SucursalId);
            return StatusCode(500, new { mensaje = "No se pudo iniciar el turno", detalle = ex.Message });
        }
    }

    /// <summary>Montos esperados del turno, para mostrarlos antes de confirmar el cuadre.</summary>
    [HttpGet("turno/{cajaTurnoId:int}/cuadre")]
    public async Task<IActionResult> GetCuadre(int cajaTurnoId)
    {
        try
        {
            return Ok(await _cajaService.GetCuadreAsync(cajaTurnoId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo cuadre del turno {CajaTurnoId}", cajaTurnoId);
            return StatusCode(500, new { mensaje = "No se pudo obtener el cuadre", detalle = ex.Message });
        }
    }

    /// <summary>Cierra el turno; con cerrarCaja=true además cierra la caja del día.</summary>
    [HttpPost("cuadrar")]
    public async Task<IActionResult> Cuadrar([FromBody] CuadrarCajaDto dto)
    {
        try
        {
            return Ok(await _cajaService.CuadrarAsync(dto, UsuarioId(), NombreUsuario()));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cuadrando el turno {CajaTurnoId}", dto.CajaTurnoId);
            return StatusCode(500, new { mensaje = "No se pudo cuadrar la caja", detalle = ex.Message });
        }
    }

    /// <summary>Registra un retiro de efectivo del turno abierto (con motivo).</summary>
    [HttpPost("retiro")]
    public async Task<IActionResult> Retiro([FromBody] RegistrarRetiroDto dto)
    {
        try
        {
            return Ok(await _cajaService.RegistrarRetiroAsync(dto, UsuarioId(), NombreUsuario()));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registrando retiro del turno {CajaTurnoId}", dto.CajaTurnoId);
            return StatusCode(500, new { mensaje = "No se pudo registrar el retiro", detalle = ex.Message });
        }
    }

    /// <summary>Corte de caja de un día (y opcionalmente de un cajero puntual).</summary>
    [HttpGet("corte/{sucursalId:int}")]
    public async Task<IActionResult> GetCorteDiario(
        int sucursalId, [FromQuery] DateTime fecha, [FromQuery] int? usuarioId)
    {
        try
        {
            return Ok(await _cajaService.GetCorteDiarioAsync(sucursalId, fecha, usuarioId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo el corte diario de la sucursal {SucursalId}", sucursalId);
            return StatusCode(500, new { mensaje = "No se pudo obtener el corte diario", detalle = ex.Message });
        }
    }

    [HttpGet("historial/{ruc}")]
    public async Task<IActionResult> GetHistorial(
        string ruc,
        [FromQuery] int? sucursalId,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] string? estado,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            return Ok(await _cajaService.GetHistorialAsync(ruc, sucursalId, desde, hasta, estado, page, pageSize));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo historial de cajas del RUC {Ruc}", ruc);
            return StatusCode(500, new { mensaje = "No se pudo obtener el historial", detalle = ex.Message });
        }
    }

    [HttpGet("{cajaAperturaId:int}")]
    public async Task<IActionResult> GetDetalle(int cajaAperturaId)
    {
        try
        {
            var detalle = await _cajaService.GetDetalleAsync(cajaAperturaId);
            if (detalle == null)
                return NotFound(new { mensaje = $"No se encontró la caja {cajaAperturaId}" });

            return Ok(detalle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo detalle de la caja {CajaAperturaId}", cajaAperturaId);
            return StatusCode(500, new { mensaje = "No se pudo obtener el detalle", detalle = ex.Message });
        }
    }

    private int UsuarioId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!int.TryParse(claim, out var usuarioId))
            throw new UnauthorizedAccessException("No se pudo identificar al usuario del token.");

        return usuarioId;
    }

    private string NombreUsuario() =>
        User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
}
