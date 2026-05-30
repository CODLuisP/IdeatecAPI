using Microsoft.AspNetCore.Mvc;
using IdeatecAPI.Application.Features.NotificacionDias.Services;
using IdeatecAPI.Application.Features.NotificacionDias.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificacionDiasController : ControllerBase
{
    private readonly INotificacionDiasService _notificacionDiasService;

    public NotificacionDiasController(INotificacionDiasService notificacionDiasService)
    {
        _notificacionDiasService = notificacionDiasService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var registros = await _notificacionDiasService.GetAllNotificacionDiasAsync();
        return Ok(registros);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarNotificacionDiasDto dto)
    {
        var result = await _notificacionDiasService.RegistrarNotificacionDiasAsync(dto);

        if (!result)
            return BadRequest(new { message = "No se pudo registrar el registro de notificación de días" });

        return StatusCode(201, new { message = "Notificación de días registrada correctamente" });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarNotificacionDiasDto dto)
    {
        var result = await _notificacionDiasService.EditarNotificacionDiasAsync(id, dto);

        if (!result)
            return BadRequest(new { message = "No se pudo actualizar el registro de notificación de días" });

        return Ok(new { message = "Notificación de días actualizada correctamente" });
    }
}
