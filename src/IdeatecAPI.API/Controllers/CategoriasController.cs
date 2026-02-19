using Microsoft.AspNetCore.Mvc;
using IdeatecAPI.Application.Features.Categorias.Services;
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
}