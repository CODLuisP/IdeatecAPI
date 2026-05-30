using Microsoft.AspNetCore.Mvc;
using IdeatecAPI.Application.Features.Vales.Services;
using IdeatecAPI.Application.Features.Vales.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ValesController : ControllerBase
{
    private readonly IValeService _valeService;

    public ValesController(IValeService valeService)
    {
        _valeService = valeService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var vales = await _valeService.GetAllValesAsync();
        return Ok(vales);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarValeDto dto)
    {
        var result = await _valeService.RegistrarValeAsync(dto);

        if (!result)
            return BadRequest(new { message = "No se pudo registrar el vale" });

        return StatusCode(201, new { message = "Vale registrado correctamente" });
    }

    [HttpPut("{idVale}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Editar(int idVale, [FromBody] EditarValeDto dto)
    {
        var result = await _valeService.EditarValeAsync(idVale, dto);

        if (!result)
            return BadRequest(new { message = "No se pudo actualizar el vale" });

        return Ok(new { message = "Vale actualizado correctamente" });
    }

    [HttpDelete("{idVale}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Eliminar(int idVale)
    {
        var result = await _valeService.EliminarValeAsync(idVale);

        if (!result)
            return BadRequest(new { message = "No se pudo eliminar el vale" });

        return Ok(new { message = "Vale eliminado correctamente" });
    }
}
