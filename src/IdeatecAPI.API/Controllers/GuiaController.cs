using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.GuiaRemision.DTOs;
using IdeatecAPI.Application.Features.GuiaRemision.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/guias")]
[Authorize]
public class GuiaController : ControllerBase
{
    private readonly IGuiaService _guiaService;
    private readonly IGuiaPdfService _pdfService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IXmlGuiaBuilderService _xmlBuilder;
    private readonly ILogger<GuiaController> _logger;

    public GuiaController(
        IGuiaService guiaService,
        IGuiaPdfService pdfService,
        IUnitOfWork unitOfWork,
        IXmlGuiaBuilderService xmlBuilder,
        ILogger<GuiaController> logger)
    {
        _guiaService = guiaService;
        _pdfService = pdfService;
        _unitOfWork = unitOfWork;
        _xmlBuilder = xmlBuilder;
        _logger = logger;
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

    [HttpGet("{ruc}/{serie}/{correlativo}")]
    public async Task<IActionResult> GetBySerieCorrelativo(string ruc, string serie, int correlativo)
    {
        var guia = await _guiaService.GetBySerieCorrelativoAsync(ruc, serie, correlativo);
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

    [HttpGet("{guiaId}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileContentResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> DescargarPdf(int guiaId)
    {
        try
        {
            var pdfBytes = await _pdfService.GenerarPdfAsync(guiaId);

            var guia = await _unitOfWork.Guias.GetByIdAsync(guiaId);
            var nombreArchivo = guia?.NumeroCompleto ?? guiaId.ToString();
            nombreArchivo = string.Concat(nombreArchivo
                .Replace("/", "-").Replace("\\", "-")
                .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));

            Response.Headers["Content-Disposition"] = $"inline; filename=\"{nombreArchivo}.pdf\"";
            return File(pdfBytes, "application/pdf");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Guía no encontrada al generar PDF: {Mensaje}", ex.Message);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar PDF de guía ID {Id}", guiaId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al generar el PDF.",
                detalle = ex.Message
            });
        }
    }
}