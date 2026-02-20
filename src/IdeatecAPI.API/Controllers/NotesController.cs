using IdeatecAPI.Application.Features.Notas.DTOs;
using IdeatecAPI.Application.Features.Notas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/notes")]
//[Authorize]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public NotesController(
    INoteService noteService
    )
    {
        _noteService = noteService;

    }

    /// <summary>
    /// Genera el XML, lo firma y lo envía a SUNAT
    /// </summary>
    [HttpPost("{comprobanteId}/send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendToSunat(int comprobanteId)
    {
        var note = await _noteService.SendToSunatAsync(comprobanteId);
        return Ok(note);
    }

    /// <summary>
    /// Obtiene todas las notas de crédito/débito de una empresa
    /// </summary>
    [HttpGet("{empresaId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(int empresaId)
    {
        var notes = await _noteService.GetAllNotesAsync(empresaId);
        return Ok(notes);
    }

    /// <summary>
    /// Obtiene una nota por su ID de comprobante
    /// </summary>
    [HttpGet("detail/{comprobanteId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int comprobanteId)
    {
        var note = await _noteService.GetNoteByIdAsync(comprobanteId);
        if (note is null)
            return NotFound(new { message = $"Nota con ID {comprobanteId} no encontrada" });

        return Ok(note);
    }

    /// <summary>
    /// Crea una nueva nota de crédito o débito
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateNoteDto dto)
    {
        var note = await _noteService.CreateNoteAsync(dto);
        return CreatedAtAction(nameof(GetById), new { comprobanteId = note.ComprobanteId }, note);
    }

    /// <summary>
    /// Actualiza el estado SUNAT de una nota (respuesta de envío)
    /// </summary>
    [HttpPatch("{comprobanteId}/sunat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEstadoSunat(int comprobanteId, [FromBody] UpdateNoteEstadoDto dto)
    {
        var note = await _noteService.UpdateEstadoSunatAsync(comprobanteId, dto);
        return Ok(note);
    }

    /// <summary>
    /// Anula (soft delete) una nota que no haya sido aceptada por SUNAT
    /// </summary>
    [HttpDelete("{comprobanteId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int comprobanteId)
    {
        await _noteService.DeleteNoteAsync(comprobanteId);
        return NoContent();
    }
}