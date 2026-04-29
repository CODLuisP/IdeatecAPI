using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Comprobante.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComprobantesController : ControllerBase
{
    private readonly IComprobanteService _comprobanteService;
    private readonly IComprobantePdfService _pdfService;
    private readonly ILogger<ComprobantesController> _logger;

    public ComprobantesController(IComprobanteService comprobanteService, IComprobantePdfService pdfService, ILogger<ComprobantesController> logger)
    {
        _comprobanteService = comprobanteService;
        _pdfService         = pdfService;
        _logger             = logger;
    }

    [HttpGet("cliente/{clienteNumDoc}/cantidad")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCantidadByCliente(string clienteNumDoc)
    {
        try
        {
            var cantidad = await _comprobanteService.GetCantidadByClienteNumDocAsync(clienteNumDoc);
            return Ok(cantidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cantidad de comprobantes para cliente {ClienteNumDoc}", clienteNumDoc);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener la cantidad de comprobantes.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("ruc/{ruc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByRucAndFechas(
        string ruc,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int? limit = null)
    {
        try
        {
            var (desde, hasta) = NormalizarFechas(fechaDesde, fechaHasta);
            var comprobantes = await _comprobanteService.GetByRucAndFechasAsync(ruc, desde, hasta, limit);
            return Ok(comprobantes ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener comprobantes para RUC {Ruc}", ruc);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los comprobantes.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("empresa/{rucEmpresa}/cliente/{clienteNumDoc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByDocClienteAndFechas(
        string rucEmpresa,
        string clienteNumDoc,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta)
    {
        try
        {
            var (desde, hasta) = NormalizarFechas(fechaDesde, fechaHasta);
            var comprobantes = await _comprobanteService.GetByDocClienteAndFechasAsync(rucEmpresa, clienteNumDoc, desde, hasta);
            return Ok(comprobantes ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener comprobantes para cliente {ClienteNumDoc}", clienteNumDoc);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los comprobantes.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("sucursal/{sucursalId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)] // solo si sucursal no existe
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBySucursal(
        int sucursalId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int? limit = null)
    {
        try
        {
            var (desde, hasta) = NormalizarFechas(fechaDesde, fechaHasta);
            var comprobantes = await _comprobanteService.GetBySucursalAndFechasAsync(sucursalId, desde, hasta, limit);
            return Ok(comprobantes ?? []);
        }
        catch (KeyNotFoundException ex)
        {
            // 404 solo cuando la sucursal no existe, no cuando no hay comprobantes
            _logger.LogWarning("Sucursal no encontrada: {Mensaje}", ex.Message);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener comprobantes para sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los comprobantes.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("empresa/{rucEmpresa}/usuario/{usuarioId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByDocUsuario(
        string rucEmpresa,
        int usuarioId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta)
    {
        try
        {
            var (desde, hasta) = NormalizarFechas(fechaDesde, fechaHasta);
            var comprobantes = await _comprobanteService.GetByDocUsuarioAndFechasAsync(rucEmpresa, usuarioId, desde, hasta);
            return Ok(comprobantes ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener comprobantes para usuario {UsuarioId}", usuarioId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los comprobantes.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("listado/ruc/{ruc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetListadoByRuc(
        string ruc,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int? limit = null)
    {
        try
        {
            var (desde, hasta) = NormalizarFechas(fechaDesde, fechaHasta);
            var result = await _comprobanteService.GetListadoByRucAndFechasAsync(ruc, desde, hasta, limit);
            return Ok(result ?? Enumerable.Empty<object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener listado por RUC {Ruc}", ruc);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los comprobantes.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("listado/sucursal/{sucursalId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)] // solo si sucursal no existe
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetListadoBySucursal(
        int sucursalId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] int? limit = null)
    {
        try
        {
            var (desde, hasta) = NormalizarFechas(fechaDesde, fechaHasta);
            var result = await _comprobanteService.GetListadoBySucursalAndFechasAsync(sucursalId, desde, hasta, limit);
            return Ok(result ?? Enumerable.Empty<object>());
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Sucursal no encontrada: {Mensaje}", ex.Message);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener listado por sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los comprobantes.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("listado/empresa/{rucEmpresa}/cliente/{clienteNumDoc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetListadoByCliente(
        string rucEmpresa,
        string clienteNumDoc,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta)
    {
        try
        {
            var (desde, hasta) = NormalizarFechas(fechaDesde, fechaHasta);
            var result = await _comprobanteService.GetListadoByDocClienteAndFechasAsync(rucEmpresa, clienteNumDoc, desde, hasta);
            return Ok(result ?? Enumerable.Empty<object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener listado por cliente {ClienteNumDoc}", clienteNumDoc);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los comprobantes.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("listado/empresa/{rucEmpresa}/usuario/{usuarioId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetListadoByUsuario(
        string rucEmpresa,
        int usuarioId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta)
    {
        try
        {
            var (desde, hasta) = NormalizarFechas(fechaDesde, fechaHasta);
            var result = await _comprobanteService.GetListadoByDocUsuarioAndFechasAsync(rucEmpresa, usuarioId, desde, hasta);
            return Ok(result ?? Enumerable.Empty<object>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener listado por usuario {UsuarioId}", usuarioId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los comprobantes.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("listado/sucursal/{sucursalId}/cliente/{clienteNumDoc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)] // solo si sucursal no existe
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetListadoByClienteAndSucursal(
        int sucursalId,
        string clienteNumDoc,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta)
    {
        try
        {
            var (desde, hasta) = NormalizarFechas(fechaDesde, fechaHasta);
            var result = await _comprobanteService.GetListadoByClienteAndSucursalAsync(sucursalId, clienteNumDoc, desde, hasta);
            return Ok(result ?? Enumerable.Empty<object>());
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Sucursal no encontrada: {Mensaje}", ex.Message);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener listado por cliente {ClienteNumDoc} y sucursal {SucursalId}", clienteNumDoc, sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los comprobantes.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("{id}/detalles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDetalles(int id)
    {
        try
        {
            var resultado = await _comprobanteService.GetDetallesByComprobanteIdAsync(id);
            if (resultado == null)
                return NotFound(new { mensaje = $"No se encontró el comprobante con ID {id}." });
            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener detalles del comprobante ID {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los detalles.",
                detalle = ex.Message
            });
        }
    }

    // resto de métodos sin cambios...
    // (EnviarSunat, Generar, DescargarPdf, GetById, GetByRucSerieNumero, GetByEstado, ActualizarCorreoWhatsapp)

    private static (DateTime? desde, DateTime? hasta) NormalizarFechas(DateTime? fechaDesde, DateTime? fechaHasta)
    {
        if (!fechaDesde.HasValue) return (null, null);
        var desde = fechaDesde.Value.Date;
        var hasta = fechaHasta.HasValue
            ? fechaHasta.Value.Date.AddDays(1).AddSeconds(-1)
            : fechaDesde.Value.Date.AddDays(1).AddSeconds(-1);
        return (desde, hasta);
    }

    [HttpPatch("actualizar/{id}/correo-whatsapp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActualizarCorreoWhatsapp(int id, [FromBody] ActualizarCorreoWhatsappDTO dto)
    {
        try
        {
            await _comprobanteService.ActualizarCorreoWhatsappAsync(id, dto);
            return Ok(new { mensaje = "Datos actualizados correctamente" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Comprobante no encontrado: {Mensaje}", ex.Message);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar correo/whatsapp del comprobante ID {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al actualizar los datos.",
                detalle = ex.Message
            });
        }
    }

    [HttpPost("GenerarXml")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Generar([FromBody] GenerarComprobanteDTO dto)
    {
        try
        {
            var resultado = await _comprobanteService.GenerarComprobanteAsync(dto);
            return StatusCode(StatusCodes.Status201Created, resultado);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Operación inválida al generar comprobante: {Mensaje}", ex.Message);
            return Conflict(new { mensaje = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Recurso no encontrado al generar comprobante: {Mensaje}", ex.Message);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar comprobante {Serie}-{Correlativo}", dto.Serie, dto.Correlativo);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al generar el comprobante.",
                detalle = ex.Message
            });
        }
    }

    [HttpPost("{id}/enviar-sunat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EnviarSunat(int id)
    {
        try
        {
            var resultado = await _comprobanteService.SendToSunatAsync(id);
            return resultado.Exitoso ? Ok(resultado) : BadRequest(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Comprobante no encontrado al enviar a SUNAT: ID {Id}", id);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Operación inválida al enviar comprobante ID {Id} a SUNAT: {Mensaje}", id, ex.Message);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar comprobante ID {Id} a SUNAT", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al enviar el comprobante a SUNAT.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("{id}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK,  Type = typeof(FileContentResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DescargarPdf(int id,
        [FromQuery] TamanoPdf tamano = TamanoPdf.A4)
    {
        try
        {
            var pdfBytes = await _pdfService.GenerarPdfAsync(id, tamano);
    
            // Recuperar número de comprobante para el nombre del archivo
            var comprobante = await _comprobanteService.GetComprobanteByIdAsync(id);
            var nombreArchivo = comprobante?.NumeroCompleto ?? id.ToString();
            nombreArchivo = string.Concat(nombreArchivo
                .Replace("/", "-").Replace("\\", "-")
                .Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
    
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{nombreArchivo}.pdf\"";
            return File(pdfBytes, "application/pdf");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Comprobante no encontrado al generar PDF: {Mensaje}", ex.Message);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar PDF del comprobante ID {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al generar el PDF.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var comprobante = await _comprobanteService.GetComprobanteByIdAsync(id);

            if (comprobante == null)
                return NotFound(new { mensaje = $"No se encontró el comprobante con ID {id}." });

            return Ok(comprobante);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener comprobante con ID {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el comprobante.",
                detalle = ex.Message
            });
        }
    }

    //toma en cuenta estado sunat aceptado
    [HttpGet("{ruc}/{serie}/{numero}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByRucSerieNumero(string ruc, string serie, int numero)
    {
        try
        {
            var comprobante = await _comprobanteService.GetByRucSerieNumeroAsync(ruc, serie, numero);

            if (comprobante == null)
                return NotFound(new { mensaje = $"No se encontró el comprobante {serie}-{numero} para el RUC '{ruc}'." });

            return Ok(comprobante);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener comprobante {Ruc}/{Serie}/{Numero}", ruc, serie, numero);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el comprobante.",
                detalle = ex.Message
            });
        }
    }

    //Busca sin tener encunta estado sunat
    [HttpGet("{ruc}/{serie}/{numero}/unico")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByComprobanteUnico(string ruc, string serie, int numero)
    {
        try
        {
            var comprobante = await _comprobanteService.GetByComprobanteUnicoAsync(ruc, serie, numero);
            return Ok(comprobante);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener comprobante único {Ruc}/{Serie}/{Numero}", ruc, serie, numero);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el comprobante.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("estado/{estado}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByEstado(string estado)
    {
        try
        {
            var comprobantes = await _comprobanteService.GetComprobanteByEstadoAsync(estado);

            if (comprobantes == null || !comprobantes.Any())
                return NotFound(new { mensaje = $"No se encontraron comprobantes con estado '{estado}'." });

            return Ok(comprobantes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener comprobantes con estado {Estado}", estado);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los comprobantes.",
                detalle = ex.Message
            });
        }
    }
}