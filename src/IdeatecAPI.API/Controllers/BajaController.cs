using IdeatecAPI.Application.Features.ComunicacionBaja.DTOs;
using IdeatecAPI.Application.Features.ComunicacionBaja.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/bajas")]
//[Authorize]

public class BajaController : ControllerBase
{
    private readonly IBajaService _bajaService;

    public BajaController(IBajaService bajaService)
    {
        _bajaService = bajaService;
    }

    /// <summary>
    /// Obtiene todas las comunicaciones de baja de una empresa
    /// </summary>
    [HttpGet("{empresaId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(int empresaId)
    {
        var bajas = await _bajaService.GetAllAsync(empresaId);
        return Ok(bajas);
    }

    /// <summary>
    /// Obtiene una comunicación de baja por ID
    /// </summary>
    [HttpGet("detalle/{bajaId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int bajaId)
    {
        var baja = await _bajaService.GetByIdAsync(bajaId);
        if (baja is null) return NotFound();
        return Ok(baja);
    }

    /// <summary>
    /// Crea una nueva comunicación de baja
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBajaDto dto)
    {
        var baja = await _bajaService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { bajaId = baja.BajaId }, baja);
    }

    /// <summary>
    /// Envía la comunicación de baja a SUNAT
    /// </summary>
    [HttpPost("{bajaId}/send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendToSunat(int bajaId)
    {
        var baja = await _bajaService.SendToSunatAsync(bajaId);
        return Ok(baja);
    }

    /// <summary>
    /// Anula una comunicación de baja que no haya sido aceptada por SUNAT
    /// </summary>
    [HttpDelete("{bajaId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int bajaId)
    {
        await _bajaService.DeleteAsync(bajaId);
        return NoContent();
    }
}