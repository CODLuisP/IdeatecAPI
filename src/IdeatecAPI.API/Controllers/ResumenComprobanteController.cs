using IdeatecAPI.Application.Features.ResumenComprobante.DTO;
using IdeatecAPI.Application.Features.ResumenComprobante.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResumenComprobanteController : ControllerBase
{
    private readonly IResumenComprobanteService _resumenComprobanteService;
    private readonly ILogger<ResumenComprobanteController> _logger;

    public ResumenComprobanteController(
        IResumenComprobanteService resumenComprobanteService,
        ILogger<ResumenComprobanteController> logger)
    {
        _resumenComprobanteService = resumenComprobanteService;
        _logger = logger;
    }

    // GET: api/ResumenComprobante
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var resumenes = await _resumenComprobanteService.GetAllResumenComprobanteAsync();
            return Ok(resumenes ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los resúmenes");
            return StatusCode(503, new { mensaje = "Servicio no disponible. Intente más tarde." });
        }
    }

    // GET: api/ResumenComprobante/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var resumen = await _resumenComprobanteService.GetResumenComprobanteByIdAsync(id);
            if (resumen is null)
                return NotFound(new { mensaje = $"Resumen con id {id} no encontrado" });

            return Ok(resumen);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener resumen con id {Id}", id);
            return StatusCode(503, new { mensaje = "Servicio no disponible. Intente más tarde." });
        }
    }

    // POST: api/ResumenComprobante
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] AgregarResumenComprobanteDTO dto)
    {
        try
        {
            var resultado = await _resumenComprobanteService.RegistrarResumenComprobanteAsync(dto);
            if (!resultado.Exitoso)
                return BadRequest(new { mensaje = resultado.Mensaje });

            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar resumen comprobante");
            return StatusCode(503, new { mensaje = "Servicio no disponible. Intente más tarde." });
        }
    }

    // POST: api/ResumenComprobante/5/enviar-sunat
    [HttpPost("{id:int}/enviar-sunat")]
    public async Task<IActionResult> EnviarSunat(int id)
    {
        try
        {
            var resultado = await _resumenComprobanteService.SendToSunatAsync(id);
            if (!resultado.Exitoso)
                return BadRequest(new { mensaje = resultado.Mensaje ?? "Error al enviar a SUNAT" });

            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de conexión con SUNAT para id {Id}", id);
            return StatusCode(503, new { mensaje = "No se pudo conectar con SUNAT. Intente más tarde." });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout al conectar con SUNAT para id {Id}", id);
            return StatusCode(504, new { mensaje = "Tiempo de espera agotado al conectar con SUNAT." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al enviar a SUNAT id {Id}", id);
            return StatusCode(503, new { mensaje = "Servicio no disponible. Intente más tarde." });
        }
    }

    // GET: api/ResumenComprobante/listado
    [HttpGet("listado")]
    public async Task<IActionResult> GetListado(
        [FromQuery] string ruc,
        [FromQuery] string? establecimiento,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50)
    {
        if (string.IsNullOrEmpty(ruc))
            return BadRequest(new { mensaje = "El RUC es requerido" });

        try
        {
            var resumenes = await _resumenComprobanteService.GetResumenesByFiltroAsync(
                ruc, establecimiento, fechaDesde, fechaHasta, page, limit);

            return Ok(resumenes ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener listado para RUC {Ruc}", ruc);
            return StatusCode(503, new { mensaje = "Servicio no disponible. Intente más tarde." });
        }
    }

    // GET: api/ResumenComprobante/{id}/detalles
    [HttpGet("{id:int}/detalles")]
    public async Task<IActionResult> GetDetalles(int id)
    {
        try
        {
            var resumen = await _resumenComprobanteService.GetResumenConDetallesAsync(id);
            return Ok(resumen ?? new ObtenerResumenComprobanteDTO());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener detalles del resumen id {Id}", id);
            return StatusCode(503, new { mensaje = "Servicio no disponible. Intente más tarde." });
        }
    }

    // GET: api/ResumenComprobante/proximo-numero
    [HttpGet("proximo-numero")]
    public async Task<IActionResult> GetProximoNumero(
        [FromQuery] string ruc,
        [FromQuery] string establecimiento,
        [FromQuery] DateTime fecha)
    {
        if (string.IsNullOrEmpty(ruc))
            return BadRequest(new { mensaje = "El RUC es requerido" });

        if (string.IsNullOrEmpty(establecimiento))
            return BadRequest(new { mensaje = "El establecimiento es requerido" });

        try
        {
            var numero = await _resumenComprobanteService.GetProximoNumeroEnvioAsync(
                ruc, establecimiento, fecha);

            return Ok(new
            {
                numeroEnvio   = numero,
                identificador = $"RC-{fecha:yyyyMMdd}-{numero}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener próximo número para RUC {Ruc}", ruc);
            return StatusCode(503, new { mensaje = "Servicio no disponible. Intente más tarde." });
        }
    }
}