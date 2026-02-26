using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Comprobante.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComprobantesController : ControllerBase
{
    private readonly IComprobanteService _comprobanteService;

    public ComprobantesController(IComprobanteService comprobanteService)
    {
        _comprobanteService = comprobanteService;
    }

    [HttpPost("GenerarXml")]
    public async Task<IActionResult> Generar([FromBody] GenerarComprobanteDTO dto)
    {
        var resultado = await _comprobanteService.GenerarComprobanteAsync(dto);

        if (!resultado.Exitoso)
            return BadRequest(resultado);

        return Ok(resultado);
    }
}