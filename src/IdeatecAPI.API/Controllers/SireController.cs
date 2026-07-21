using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Sire.Services;
using IdeatecAPI.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/sire")]
[Authorize]
public class SireController : ControllerBase
{
    private readonly ISireService _sireService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SireController> _logger;

    public SireController(ISireService sireService, IUnitOfWork unitOfWork, ILogger<SireController> logger)
    {
        _sireService = sireService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet("periodos")]
    public async Task<IActionResult> ConsultarPeriodos([FromQuery] string ruc)
    {
        if (string.IsNullOrWhiteSpace(ruc))
            return BadRequest(new { mensaje = "El parámetro ruc es requerido." });

        var empresa = await ValidarEmpresaAsync(ruc);
        if (empresa is null)
            return NotFound(new { mensaje = "Empresa no encontrada o sin credenciales SIRE configuradas." });

        var resultado = await _sireService.ConsultarPeriodosAsync(
            empresa.Ruc, empresa.SolUsuario!, empresa.SolClave!, empresa.ClientId!, empresa.ClientSecret!);

        return Ok(resultado);
    }

    [HttpGet("propuesta/{perTributario}/comprobantes")]
    public async Task<IActionResult> DescargarPropuesta(string perTributario, [FromQuery] string ruc)
    {
        if (string.IsNullOrWhiteSpace(ruc))
            return BadRequest(new { mensaje = "El parámetro ruc es requerido." });

        var empresa = await ValidarEmpresaAsync(ruc);
        if (empresa is null)
            return NotFound(new { mensaje = "Empresa no encontrada o sin credenciales SIRE configuradas." });

        var resultado = await _sireService.DescargarPropuestaAsync(
            empresa.Ruc, empresa.SolUsuario!, empresa.SolClave!, empresa.ClientId!, empresa.ClientSecret!, perTributario);

        return Ok(resultado);
    }

    [HttpPost("aceptar-propuesta/{perTributario}")]
    public async Task<IActionResult> AceptarPropuesta(string perTributario, [FromQuery] string ruc)
    {
        if (string.IsNullOrWhiteSpace(ruc))
            return BadRequest(new { mensaje = "El parámetro ruc es requerido." });

        var empresa = await ValidarEmpresaAsync(ruc);
        if (empresa is null)
            return NotFound(new { mensaje = "Empresa no encontrada o sin credenciales SIRE configuradas." });

        var resultado = await _sireService.AceptarPropuestaAsync(
            empresa.Ruc, empresa.SolUsuario!, empresa.SolClave!, empresa.ClientId!, empresa.ClientSecret!, perTributario);

        var registro = await _unitOfWork.SireRegistros.GetByRucAndPeriodoAsync(empresa.Ruc, perTributario);
        if (registro is null)
        {
            registro = new SireRegistro
            {
                RucEmpresa = empresa.Ruc,
                PerTributario = perTributario,
                NumTicket = resultado.NumTicket,
                Estado = resultado.Success ? "PROPUESTA_ACEPTADA" : "ERROR",
                RespuestaSunat = resultado.RespuestaCruda,
                Mensaje = resultado.Mensaje,
                FechaConsulta = DateTime.Now
            };
            await _unitOfWork.SireRegistros.CreateSireRegistroAsync(registro);
        }
        else
        {
            registro.NumTicket = resultado.NumTicket;
            registro.Estado = resultado.Success ? "PROPUESTA_ACEPTADA" : "ERROR";
            registro.RespuestaSunat = resultado.RespuestaCruda;
            registro.Mensaje = resultado.Mensaje;
            registro.FechaConsulta = DateTime.Now;
            await _unitOfWork.SireRegistros.UpdateSireRegistroAsync(registro);
        }

        return Ok(resultado);
    }

    [HttpPost("cerrar/{perTributario}")]
    public async Task<IActionResult> RegistrarPreliminar(string perTributario, [FromQuery] string ruc)
    {
        if (string.IsNullOrWhiteSpace(ruc))
            return BadRequest(new { mensaje = "El parámetro ruc es requerido." });

        var empresa = await ValidarEmpresaAsync(ruc);
        if (empresa is null)
            return NotFound(new { mensaje = "Empresa no encontrada o sin credenciales SIRE configuradas." });

        var resultado = await _sireService.RegistrarPreliminarAsync(
            empresa.Ruc, empresa.SolUsuario!, empresa.SolClave!, empresa.ClientId!, empresa.ClientSecret!, perTributario);

        var registro = await _unitOfWork.SireRegistros.GetByRucAndPeriodoAsync(empresa.Ruc, perTributario);
        if (registro is not null)
        {
            registro.Estado = resultado.Success ? "CERRADO" : "ERROR";
            registro.RespuestaSunat = resultado.RespuestaCruda;
            registro.Mensaje = resultado.Mensaje;
            registro.FechaCierre = resultado.Success ? DateTime.Now : registro.FechaCierre;
            await _unitOfWork.SireRegistros.UpdateSireRegistroAsync(registro);
        }

        return Ok(resultado);
    }

    [HttpGet("historial")]
    public async Task<IActionResult> GetHistorial([FromQuery] string ruc)
    {
        if (string.IsNullOrWhiteSpace(ruc))
            return BadRequest(new { mensaje = "El parámetro ruc es requerido." });

        var historial = await _unitOfWork.SireRegistros.GetHistorialByRucAsync(ruc);
        return Ok(historial);
    }

    private async Task<Domain.Entities.Empresa?> ValidarEmpresaAsync(string ruc)
    {
        var empresa = await _unitOfWork.Empresas.GetEmpresaByRucAsync(ruc);
        if (empresa is null)
            return null;

        if (string.IsNullOrWhiteSpace(empresa.SolUsuario) || string.IsNullOrWhiteSpace(empresa.SolClave)
            || string.IsNullOrWhiteSpace(empresa.ClientId) || string.IsNullOrWhiteSpace(empresa.ClientSecret))
            return null;

        return empresa;
    }
}
