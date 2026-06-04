using IdeatecAPI.Application.Features.NotificacionesEnviadas.DTOs;
using IdeatecAPI.Application.Features.NotificacionesEnviadas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/notificacionesenviadas")]
[Authorize]
public class NotificacionesEnviadasController : ControllerBase
{
    private readonly INotificacionEnviadaService _service;
    private readonly ILogger<NotificacionesEnviadasController> _logger;

    public NotificacionesEnviadasController(
        INotificacionEnviadaService service,
        ILogger<NotificacionesEnviadasController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // GET api/notificacionesenviadas
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener notificaciones enviadas");
            return StatusCode(500, new { mensaje = "Error al obtener las notificaciones enviadas.", detalle = ex.Message });
        }
    }

    // POST api/notificacionesenviadas
    // Body: { "id": 123, "emailEnviado": true, "wspEnviado": false }
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarNotificacionEnviadaDto dto)
    {
        try
        {
            var result = await _service.RegistrarAsync(dto);

            if (!result)
                return BadRequest(new { mensaje = "No se pudo registrar la notificación enviada." });

            return StatusCode(201, new { mensaje = "Notificación enviada registrada correctamente." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar notificación enviada ID {Id}", dto.Id);
            return StatusCode(500, new { mensaje = "Error al registrar la notificación enviada.", detalle = ex.Message });
        }
    }

    // PUT api/notificacionesenviadas/{id}
    // Body: { "emailEnviado": true, "wspEnviado": true }
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarNotificacionEnviadaDto dto)
    {
        try
        {
            var result = await _service.EditarAsync(id, dto);

            if (!result)
                return NotFound(new { mensaje = $"No se encontró la notificación enviada con ID {id}." });

            return Ok(new { mensaje = "Notificación enviada actualizada correctamente." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar notificación enviada ID {Id}", id);
            return StatusCode(500, new { mensaje = "Error al actualizar la notificación enviada.", detalle = ex.Message });
        }
    }

    // DELETE api/notificacionesenviadas/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var result = await _service.EliminarAsync(id);

            if (!result)
                return NotFound(new { mensaje = $"No se encontró la notificación enviada con ID {id}." });

            return Ok(new { mensaje = "Notificación enviada eliminada correctamente." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar notificación enviada ID {Id}", id);
            return StatusCode(500, new { mensaje = "Error al eliminar la notificación enviada.", detalle = ex.Message });
        }
    }
}
