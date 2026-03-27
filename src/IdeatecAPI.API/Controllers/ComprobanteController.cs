using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Comprobante.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class ComprobantesController : ControllerBase
{
    private readonly IComprobanteService _comprobanteService;

    public ComprobantesController(IComprobanteService comprobanteService)
    {
        _comprobanteService = comprobanteService;
    }

    [HttpGet("cliente/{clienteNumDoc}/cantidad")]
    public async Task<IActionResult> GetCantidadByCliente(string clienteNumDoc)
    {
        var cantidad = await _comprobanteService.GetCantidadByClienteNumDocAsync(clienteNumDoc);
        return Ok(cantidad);
    }

    [HttpPost("GenerarXml")]
    public async Task<IActionResult> Generar([FromBody] GenerarComprobanteDTO dto)
    {
        var resultado = await _comprobanteService.GenerarComprobanteAsync(dto);

        if (!resultado.Exitoso)
            return BadRequest(resultado);

        return Ok(resultado);
    }

    // POST api/comprobantes/{id}/enviar-sunat
    [HttpPost("{id}/enviar-sunat")]
    public async Task<IActionResult> EnviarSunat(int id)
    {
        try
        {
            var result = await _comprobanteService.SendToSunatAsync(id);
            return result.Exitoso ? Ok(result) : BadRequest(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var comprobante = await _comprobanteService.GetComprobanteByIdAsync(id);
        if (comprobante == null)
            return NotFound(new { mensaje = "Comprobante no encontrado" });

        return Ok(comprobante);
    }

    // GET api/comprobantes/estado/{estado}
    [HttpGet("estado/{estado}")]
    public async Task<IActionResult> GetByEstado(string estado)
    {
        var comprobantes = await _comprobanteService.GetComprobanteByEstadoAsync(estado);
        return Ok(comprobantes);
    }
}