using Microsoft.AspNetCore.Mvc;
using IdeatecAPI.Application.Features.Categorias.Services;
using IdeatecAPI.Application.Features.Categorias.DTOs;
namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaService _categoriaService;

    public CategoriasController(ICategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var categorias = await _categoriaService.GetAllCategoriasAsync();
        return Ok(categorias);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var categoria = await _categoriaService.GetCategoriaByIdAsync(id);
        
        if (categoria == null)
            return NotFound(new { message = $"Categoría con ID {id} no encontrada" });

        return Ok(categoria);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarCategoriaDto dto)
    {
        var result = await _categoriaService.RegistrarCategoriaAsync(dto);

        if (!result)
            return BadRequest(new { message = "No se pudo registrar la categoría" });

        return StatusCode(201, new { message = "Categoría registrada correctamente" });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarCategoriaDto dto)
    {
        if (id != dto.CategoriaId)
            return BadRequest(new { message = "El ID de la URL no coincide con el del cuerpo" });

        var result = await _categoriaService.EditarCategoriaAsync(dto);

        if (!result)
            return BadRequest(new { message = "No se pudo actualizar la categoría" });

        return Ok(new { message = "Categoría actualizada correctamente" });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Eliminar(int id)
    {
        var result = await _categoriaService.EliminarCategoriaAsync(id);

        if (!result)
            return BadRequest(new { message = "No se pudo eliminar la categoría" });

        return Ok(new { message = "Categoría eliminada correctamente" });
    }
}