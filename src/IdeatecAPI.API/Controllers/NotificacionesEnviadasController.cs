using Microsoft.AspNetCore.Mvc;
using IdeatecAPI.Application.Features.NotificacionesEnviadas.Services;
using IdeatecAPI.Application.Features.NotificacionesEnviadas.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificacionesEnviadasController : ControllerBase
{
    private readonly INotificacionEnviadaService _notificacionEnviadaService;

    public NotificacionesEnviadasController(INotificacionEnviadaService notificacionEnviadaService)
    {
        _notificacionEnviadaService = notificacionEnviadaService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var notificaciones = await _notificacionEnviadaService.GetAllNotificacionesEnviadasAsync();
        return Ok(notificaciones);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarNotificacionEnviadaDto dto)
    {
        var result = await _notificacionEnviadaService.RegistrarNotificacionEnviadaAsync(dto);

        if (!result)
            return BadRequest(new { message = "No se pudo registrar la notificación enviada" });

        return StatusCode(201, new { message = "Notificación enviada registrada correctamente" });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarNotificacionEnviadaDto dto)
    {
        var result = await _notificacionEnviadaService.EditarNotificacionEnviadaAsync(id, dto);

        if (!result)
            return BadRequest(new { message = "No se pudo actualizar la notificación enviada" });

        return Ok(new { message = "Notificación enviada actualizada correctamente" });
    }
}
