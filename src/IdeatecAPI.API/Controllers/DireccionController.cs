using IdeatecAPI.Application.Features.Direccion.DTOs;
using IdeatecAPI.Application.Features.Direccion.Services;
using Microsoft.AspNetCore.Mvc;


namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
}