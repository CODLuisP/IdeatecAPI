using IdeatecAPI.Application.Features.Empresas.DTOs;
using IdeatecAPI.Application.Features.Empresas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly IEmpresaService _empresaService;

    public CompaniesController(IEmpresaService empresaService)
    {
        _empresaService = empresaService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var empresas = await _empresaService.GetAllEmpresasAsync();
        return Ok(empresas);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var empresa = await _empresaService.GetEmpresaByIdAsync(id);
        if (empresa is null)
            return NotFound(new { message = $"Empresa con ID {id} no encontrada" });
        return Ok(empresa);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEmpresaDto dto)
    {
        var empresa = await _empresaService.CreateEmpresaAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = empresa.Id }, empresa);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmpresaDto dto)
    {
        var empresa = await _empresaService.UpdateEmpresaAsync(id, dto);
        return Ok(empresa);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _empresaService.DeleteEmpresaAsync(id);
        return NoContent();
    }
}