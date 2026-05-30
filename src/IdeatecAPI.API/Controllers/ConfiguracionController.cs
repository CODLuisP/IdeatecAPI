using Microsoft.AspNetCore.Mvc;
using IdeatecAPI.Application.Features.Configuracion.Services;
using IdeatecAPI.Application.Features.Configuracion.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfiguracionService _configuracionService;

    public ConfiguracionController(IConfiguracionService configuracionService)
    {
        _configuracionService = configuracionService;
    }

    [HttpGet("{ruc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByRuc(int ruc)
    {
        var configuracion = await _configuracionService.GetByRucAsync(ruc);

        if (configuracion == null)
            return NotFound(new { message = $"No se encontró configuración para el RUC {ruc}" });

        return Ok(configuracion);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarConfiguracionDto dto)
    {
        var result = await _configuracionService.RegistrarConfiguracionAsync(dto);

        if (!result)
            return BadRequest(new { message = "No se pudo registrar la configuración" });

        return StatusCode(201, new { message = "Configuración registrada correctamente" });
    }

    [HttpPut("{ruc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Editar(int ruc, [FromBody] EditarConfiguracionDto dto)
    {
        var result = await _configuracionService.EditarConfiguracionAsync(ruc, dto);

        if (!result)
            return BadRequest(new { message = "No se pudo actualizar la configuración" });

        return Ok(new { message = "Configuración actualizada correctamente" });
    }
}
