// IdeatecAPI.API.Controllers/PlantillaVelsatController.cs
using IdeatecAPI.Application.Features.PlantillaVelsat.DTOs;
using IdeatecAPI.Application.Features.PlantillaVelsat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlantillaVelsatController : ControllerBase
{
    private readonly IPlantillaVelsatService _service;
    private readonly ILogger<PlantillaVelsatController> _logger;

    public PlantillaVelsatController(IPlantillaVelsatService service, ILogger<PlantillaVelsatController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET api/plantillavelsat/{periodo}
    [HttpGet("{periodo}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllAsync(string? periodo = null)
    {
        try
        {
            var registros = await _service.GetAllAsync(periodo);
            return Ok(registros ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener plantillavelsat {Periodo}", periodo ?? "todos");
            return StatusCode(500, new { mensaje = "Error al obtener los registros.", detalle = ex.Message });
        }
    }

    // POST api/plantillavelsat
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CrearAsync([FromBody] CrearPlantillaVelsatDTO dto)
    {
        try
        {
            var creado = await _service.CrearAsync(dto);
            return StatusCode(StatusCodes.Status201Created, creado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear plantillavelsat");
            return StatusCode(500, new { mensaje = "Error al crear el registro.", detalle = ex.Message });
        }
    }

    // PATCH api/plantillavelsat/{id}
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EditarAsync(int id, [FromBody] EditarPlantillaVelsatDTO dto)
    {
        try
        {
            var resultado = await _service.EditarAsync(id, dto);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró el registro con ID {id}." });

            return Ok(new { mensaje = "Registro actualizado correctamente." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar plantillavelsat ID {Id}", id);
            return StatusCode(500, new { mensaje = "Error al editar el registro.", detalle = ex.Message });
        }
    }

    // DELETE api/plantillavelsat/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EliminarAsync(int id)
    {
        try
        {
            var resultado = await _service.EliminarAsync(id);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró el registro con ID {id}." });

            return Ok(new { mensaje = "Registro eliminado correctamente." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar plantillavelsat ID {Id}", id);
            return StatusCode(500, new { mensaje = "Error al eliminar el registro.", detalle = ex.Message });
        }
    }
}