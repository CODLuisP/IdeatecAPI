using IdeatecAPI.Application.Features.Trabajadores.DTOs;
using IdeatecAPI.Application.Features.Trabajadores.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class TrabajadorController : ControllerBase
{
    private readonly ITrabajadorService _trabajadorService;
    private readonly ILogger<TrabajadorController> _logger;

    public TrabajadorController(ITrabajadorService trabajadorService, ILogger<TrabajadorController> logger)
    {
        _trabajadorService = trabajadorService;
        _logger = logger;
    }

    // GET api/trabajador/sucursal/{sucursalId}
    [HttpGet("sucursal/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllBySucursalAsync(int sucursalId)
    {
        try
        {
            var trabajadores = await _trabajadorService.GetAllBySucursalAsync(sucursalId);
            return Ok(trabajadores ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener trabajadores de la sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los trabajadores.",
                detalle = ex.Message
            });
        }
    }

    // GET api/trabajador/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        try
        {
            var trabajador = await _trabajadorService.GetByIdAsync(id);

            if (trabajador == null)
                return NotFound(new { mensaje = $"No se encontró el trabajador con ID {id}." });

            return Ok(trabajador);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Argumento inválido al obtener trabajador ID {Id}", id);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener trabajador ID {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el trabajador.",
                detalle = ex.Message
            });
        }
    }

    // GET api/trabajador/search/{sucursalId}?q=juan
    [HttpGet("search/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchAsync(int sucursalId, [FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { mensaje = "Debes ingresar al menos una letra para buscar." });

        try
        {
            var trabajadores = await _trabajadorService.SearchAsync(sucursalId, q);
            return Ok(trabajadores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar trabajadores en sucursal {SucursalId} con '{Palabra}'", sucursalId, q);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al buscar los trabajadores.",
                detalle = ex.Message
            });
        }
    }

    // POST api/trabajador
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegistrarAsync([FromBody] RegistrarTrabajadorDTO dto)
    {
        try
        {
            var creado = await _trabajadorService.RegistrarAsync(dto);
            return StatusCode(StatusCodes.Status201Created, creado);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Intento de registro duplicado: DNI {Dni}", dto.Dni);
            return Conflict(new { mensaje = ex.Message });
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1062)
        {
            _logger.LogWarning("Duplicado en BD: DNI {Dni}", dto.Dni);
            return Conflict(new { mensaje = $"Ya existe un trabajador con el DNI '{dto.Dni}'." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar trabajador con DNI {Dni}", dto.Dni);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al registrar el trabajador.",
                detalle = ex.Message
            });
        }
    }

    // PUT api/trabajador/{id}
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EditarAsync(int id, [FromBody] EditarTrabajadorDTO dto)
    {
        try
        {
            dto.Id = id;
            var resultado = await _trabajadorService.EditarAsync(dto);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró el trabajador con ID {id}." });

            return Ok(new { mensaje = "Trabajador actualizado correctamente." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Argumento inválido al editar trabajador ID {Id}", id);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar trabajador ID {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al editar el trabajador.",
                detalle = ex.Message
            });
        }
    }

    // DELETE api/trabajador/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EliminarAsync(int id)
    {
        try
        {
            var resultado = await _trabajadorService.EliminarAsync(id);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró el trabajador con ID {id}." });

            return Ok(new { mensaje = "Trabajador eliminado correctamente." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Argumento inválido al eliminar trabajador ID {Id}", id);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar trabajador ID {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al eliminar el trabajador.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("{id:int}/reporte")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetReporteAsync(
    int id,
    [FromQuery] DateTime? fechaDesde,
    [FromQuery] DateTime? fechaHasta)
    {
        try
        {
            var reporte = await _trabajadorService.GetReporteByTrabajadorAsync(id, fechaDesde, fechaHasta);

            if (reporte == null)
                return NotFound(new { mensaje = $"No se encontraron servicios para el trabajador ID {id} en el rango indicado." });

            return Ok(reporte);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar reporte del trabajador ID {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al generar el reporte.",
                detalle = ex.Message
            });
        }
    }

    // GET api/trabajador/ranking/{sucursalId}?fechaDesde=...&fechaHasta=...
    [HttpGet("ranking/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRankingAsync(
        int sucursalId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta)
    {
        try
        {
            var ranking = await _trabajadorService.GetRankingBySucursalAsync(
                sucursalId, fechaDesde, fechaHasta);
            return Ok(ranking);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ranking de trabajadores sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el ranking.",
                detalle = ex.Message
            });
        }
    }

    // GET api/trabajador/servicios-top/{sucursalId}?fechaDesde=...&fechaHasta=...&limit=10
    [HttpGet("servicios-top/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetServiciosTopAsync(
        int sucursalId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta)
    {
        try
        {
            var servicios = await _trabajadorService.GetServiciosTopBySucursalAsync(
                sucursalId, fechaDesde, fechaHasta);
            return Ok(servicios);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener servicios top sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los servicios top.",
                detalle = ex.Message
            });
        }
    }

    // GET api/trabajador/reporte-cliente/{sucursalId}?q=juan&fechaDesde=...&fechaHasta=...
    [HttpGet("reporte-cliente/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetReporteByClienteAsync(
        int sucursalId,
        [FromQuery] string q,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { mensaje = "Debes ingresar al menos una letra para buscar." });

        try
        {
            var reporte = await _trabajadorService.GetReporteByClienteAsync(
                sucursalId, q, fechaDesde, fechaHasta);
            return Ok(reporte);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar reporte por cliente sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al generar el reporte.",
                detalle = ex.Message
            });
        }
    }

    // GET api/trabajador/detalle-servicio/{sucursalId}?descripcion=Pintado de uñas&fechaDesde=...
    [HttpGet("detalle-servicio/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDetalleByServicioAsync(
        int sucursalId,
        [FromQuery] string descripcion,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            return BadRequest(new { mensaje = "Descripción requerida." });

        try
        {
            var detalle = await _trabajadorService.GetDetalleByServicioAsync(
                sucursalId, descripcion, fechaDesde, fechaHasta);
            return Ok(detalle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener detalle servicio {Descripcion}", descripcion);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error.",
                detalle = ex.Message
            });
        }
    }
}