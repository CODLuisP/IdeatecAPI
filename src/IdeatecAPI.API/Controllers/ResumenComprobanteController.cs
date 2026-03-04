using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Features.ResumenComprobante.DTO;
using IdeatecAPI.Application.Features.ResumenComprobante.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumenComprobanteController : ControllerBase
{
    private readonly IResumenComprobanteService _resumenComprobanteService;

    public ResumenComprobanteController(IResumenComprobanteService resumenComprobanteService)
    {
        _resumenComprobanteService = resumenComprobanteService;
    }

    // GET: api/ResumenComprobante
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var resumenes = await _resumenComprobanteService.GetAllResumenComprobanteAsync();
        return Ok(resumenes);
    }

    // GET: api/ResumenComprobante/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var resumen = await _resumenComprobanteService.GetResumenComprobanteByIdAsync(id);
        if (resumen is null)
            return NotFound($"Resumen con id {id} no encontrado");

        return Ok(resumen);
    }

    // POST: api/ResumenComprobante
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] AgregarResumenComprobanteDTO dto)
    {
        var resultado = await _resumenComprobanteService.RegistrarResumenComprobanteAsync(dto);
        if (!resultado.Exitoso)
            return BadRequest(resultado.Mensaje);

        return Ok(resultado);
    }

    // POST: api/ResumenComprobante/5/enviar-sunat
    [HttpPost("{id:int}/enviar-sunat")]
    public async Task<IActionResult> EnviarSunat(int id)
    {
        var resultado = await _resumenComprobanteService.SendToSunatAsync(id);
        if (!resultado.Exitoso)
            return BadRequest(resultado);

        return Ok(resultado);
    }
}