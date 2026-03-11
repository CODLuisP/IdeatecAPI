using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Features.SerieCorrelativo.DTOs;
using IdeatecAPI.Application.Features.SerieCorrelativo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SerieCorrelativoController : ControllerBase
{
    private readonly ISerieCorrelativoService _serieCorrelativoService;

    public SerieCorrelativoController(ISerieCorrelativoService serieCorrelativoService)
    {
        _serieCorrelativoService = serieCorrelativoService;
    }

    [HttpGet("{empresaId}/{tipoComprobante}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(int empresaId, string tipoComprobante)
    {
        var resultado = await _serieCorrelativoService.GetSerieCorrelativoAsync(empresaId, tipoComprobante);
        return Ok(resultado);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Registrar([FromBody] AgregarSerieCorrelativoDTO dto)
    {
        var id = await _serieCorrelativoService.RegistrarSerieCorrelativoAsync(dto);
        return Ok(new { serieId = id });
    }
}