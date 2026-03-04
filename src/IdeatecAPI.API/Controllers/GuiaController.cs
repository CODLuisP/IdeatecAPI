using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.GuiaRemision.DTOs;
using IdeatecAPI.Application.Features.GuiaRemision.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/guias")]
//[Authorize]

public class GuiaController : ControllerBase
{
    private readonly IGuiaService _guiaService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IXmlGuiaBuilderService _xmlBuilder;

    public GuiaController(
        IGuiaService guiaService,
        IUnitOfWork unitOfWork,
        IXmlGuiaBuilderService xmlBuilder)
    {
        _guiaService = guiaService;
        _unitOfWork = unitOfWork;
        _xmlBuilder = xmlBuilder;
    }

    [HttpGet("{empresaId}")]
    public async Task<IActionResult> GetAll(int empresaId)
    {
        var guias = await _guiaService.GetAllAsync(empresaId);
        return Ok(guias);
    }

    [HttpGet("detalle/{guiaId}")]
    public async Task<IActionResult> GetById(int guiaId)
    {
        var guia = await _guiaService.GetByIdAsync(guiaId);
        if (guia is null) return NotFound();
        return Ok(guia);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGuiaDto dto)
    {
        var guia = await _guiaService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { guiaId = guia.GuiaId }, guia);
    }

    [HttpPost("{guiaId}/send")]
    public async Task<IActionResult> SendToSunat(int guiaId)
    {
        var guia = await _guiaService.SendToSunatAsync(guiaId);
        return Ok(guia);
    }

    [HttpPost("{guiaId}/consultar")]
    public async Task<IActionResult> Consultar(int guiaId)
    {
        var guia = await _guiaService.SendToSunatAsync(guiaId);
        return Ok(guia);
    }

    [HttpDelete("{guiaId}")]
    public async Task<IActionResult> Delete(int guiaId)
    {
        await _guiaService.DeleteAsync(guiaId);
        return NoContent();
    }

    [HttpGet("{guiaId}/xml-preview")]
    public async Task<IActionResult> XmlPreview(int guiaId)
    {
        var guia = await _unitOfWork.Guias.GetByIdAsync(guiaId);
        if (guia is null) return NotFound();

        var details = (await _unitOfWork.GuiaDetalles.GetByGuiaIdAsync(guiaId)).ToList();
        var xml = guia.TipoDoc == "31"
            ? _xmlBuilder.BuildXmlTransportista(guia, details)
            : _xmlBuilder.BuildXml(guia, details);

        return Content(xml, "application/xml");
    }
}