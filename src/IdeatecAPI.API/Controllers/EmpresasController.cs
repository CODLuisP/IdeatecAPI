using IdeatecAPI.Application.Features.Comprobante.Services;
using IdeatecAPI.Application.Features.Empresas.DTOs;
using IdeatecAPI.Application.Features.Empresas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

public class FileUploadRequest
{
    public IFormFile File { get; set; } = null!;
}

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly IEmpresaService _empresaService;
    private readonly IComprobanteService _comprobanteService;

    public CompaniesController(IEmpresaService empresaService, IComprobanteService comprobanteService)
    {
        _empresaService = empresaService;
        _comprobanteService = comprobanteService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var empresas = await _empresaService.GetAllEmpresasAsync();
        return Ok(empresas);
    }

    [HttpGet("{ruc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByRuc(string ruc)
    {
        var empresa = await _empresaService.GetEmpresaByRucAsync(ruc);
        if (empresa is null)
            return NotFound(new { message = $"Empresa con RUC {ruc} no encontrada" });
        return Ok(empresa);
    }

    [HttpGet("{ruc}/comprobantes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComprobantes(
    string ruc,
    [FromQuery] DateTime fechaDesde,
    [FromQuery] DateTime fechaHasta)
    {
        var comprobantes = await _comprobanteService.GetByRucAndFechasAsync(ruc, fechaDesde, fechaHasta);
        return Ok(comprobantes);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateEmpresaDto dto)
    {
        var empresa = await _empresaService.CreateEmpresaAsync(dto);
        return CreatedAtAction(nameof(GetByRuc), new { ruc = empresa.Ruc }, empresa);
    }

    [HttpPut("{ruc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string ruc, [FromBody] UpdateEmpresaDto dto)
    {
        var empresa = await _empresaService.UpdateEmpresaAsync(ruc, dto);
        return Ok(empresa);
    }

    [HttpDelete("{ruc}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string ruc)
    {
        await _empresaService.DeleteEmpresaAsync(ruc);
        return NoContent();
    }

    [HttpPost("file/base64")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FileToBase64([FromForm] FileUploadRequest request)

    {
        try
        {
            var file = request.File;
            var result = await _empresaService.ConvertFileToBase64Async(
                file.OpenReadStream(),
                file.FileName,
                file.ContentType,
                file.Length
            );
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpPost("certificate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConvertCertificado([FromBody] CertificadoRequestDto dto)
    {
        try
        {
            var result = await _empresaService.ConvertCertificadoAsync(dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpPost("base64/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Base64ToFile([FromBody] Base64ToFileRequestDto dto)
    {
        try
        {
            var result = await _empresaService.ConvertBase64ToFileAsync(dto);
            return File(result.Bytes, result.ContentType, result.FileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("certificate/free")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerarCertificadoFree([FromBody] CertificadoFreeRequestDto dto)
    {
        try
        {
            var result = await _empresaService.GenerarCertificadoFreeAsync(dto);
            return File(result.Bytes, result.ContentType, result.FileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}