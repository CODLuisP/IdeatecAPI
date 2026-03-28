using IdeatecAPI.Application.Features.Direccion.DTOs;
using IdeatecAPI.Application.Features.Direccion.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class DireccionController : ControllerBase
{
   private readonly IDireccionService _direccionService;

    public DireccionController(IDireccionService direccionService)
    {
        _direccionService = direccionService;
    }

    [HttpPost("direccion")]
    public async Task<IActionResult> CrearDireccion([FromBody] RegistrarDireccionDTO dto)
    {
        await _direccionService.CrearDireccionAsync(dto);
        return Ok(new { mensaje = "Dirección registrada correctamente" });
    }

    // ✅ EDITAR
    [HttpPut("{direccionId}")]
    public async Task<IActionResult> EditarDireccion(
        int direccionId,
        [FromBody] DireccionDTO dto)
    {
        if (direccionId != dto.DireccionId)
            return BadRequest("El id de la URL no coincide con el del body.");

        var actualizado = await _direccionService.EditarDireccionAsync(dto);

        if (!actualizado)
            return NotFound("Dirección no encontrada.");

        return Ok(new { mensaje = "Dirección actualizada correctamente" });
    }

    // ✅ ELIMINAR
    [HttpDelete("{direccionId}")]
    public async Task<IActionResult> EliminarDireccion(int direccionId)
    {
        var eliminado = await _direccionService
            .EliminarDireccionAsync(direccionId);

        if (!eliminado)
            return NotFound("Dirección no encontrada.");

        return Ok(new { mensaje = "Dirección eliminada correctamente" });
    }
}