using IdeatecAPI.Application.Features.Proveedor.DTOs;
using IdeatecAPI.Application.Features.Proveedor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProveedorController : ControllerBase
{
    private readonly IProveedorService _proveedorService;
    private readonly ICompraProveedorService _compraProveedorService;
    private readonly ILogger<ProveedorController> _logger;

    public ProveedorController(
        IProveedorService proveedorService,
        ICompraProveedorService compraProveedorService,
        ILogger<ProveedorController> logger)
    {
        _proveedorService = proveedorService;
        _compraProveedorService = compraProveedorService;
        _logger = logger;
    }

    // GET api/proveedor/ruc/{rucEmpresa}
    [HttpGet("ruc/{rucEmpresa}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllByRucEmpresaAsync(string rucEmpresa)
    {
        try
        {
            var proveedores = await _proveedorService.GetAllByRucEmpresaAsync(rucEmpresa);
            return Ok(proveedores ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener proveedores para RUC {RucEmpresa}", rucEmpresa);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los proveedores.",
                detalle = ex.Message
            });
        }
    }

    // GET api/proveedor/detalle/{rucEmpresa}/{proveedorId}
    [HttpGet("detalle/{rucEmpresa}/{proveedorId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByIdRucEmpresaAsync(string rucEmpresa, int proveedorId)
    {
        try
        {
            var proveedor = await _proveedorService.GetByIdRucEmpresaAsync(rucEmpresa, proveedorId);

            if (proveedor == null)
                return NotFound(new { mensaje = $"No se encontró el proveedor con ID {proveedorId} para el RUC '{rucEmpresa}'." });

            return Ok(proveedor);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener proveedor ID {ProveedorId} para RUC {RucEmpresa}", proveedorId, rucEmpresa);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el proveedor.",
                detalle = ex.Message
            });
        }
    }

    // GET api/proveedor/search/{rucEmpresa}?q=ana
    [HttpGet("search/{rucEmpresa}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SearchAsync(string rucEmpresa, [FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { mensaje = "Debes ingresar al menos una letra para buscar." });

        try
        {
            var proveedores = await _proveedorService.SearchAsync(rucEmpresa, q);
            return Ok(proveedores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar proveedores para RUC {RucEmpresa} con palabra '{Palabra}'", rucEmpresa, q);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al buscar los proveedores.",
                detalle = ex.Message
            });
        }
    }

    // POST api/proveedor
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegistrarAsync([FromBody] RegistrarProveedorDTO dto)
    {
        try
        {
            var proveedorCreado = await _proveedorService.RegistrarAsync(dto);
            return StatusCode(StatusCodes.Status201Created, proveedorCreado);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1062)
        {
            return Conflict(new { mensaje = $"Ya existe un proveedor con el documento '{dto.NumDocumento}'." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar proveedor con documento {NumDocumento}", dto.NumDocumento);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al registrar el proveedor.",
                detalle = ex.Message
            });
        }
    }

    // PUT api/proveedor/{id}
    [HttpPut("{proveedorId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EditarAsync(int proveedorId, [FromBody] EditarProveedorDTO dto)
    {
        try
        {
            dto.ProveedorId = proveedorId;
            var resultado = await _proveedorService.EditarAsync(dto);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró el proveedor con ID {proveedorId}." });

            return Ok(new { mensaje = "Proveedor actualizado correctamente." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar proveedor con ID {ProveedorId}", proveedorId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al editar el proveedor.",
                detalle = ex.Message
            });
        }
    }

    // DELETE api/proveedor/{id}
    [HttpDelete("{proveedorId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EliminarAsync(int proveedorId)
    {
        try
        {
            var resultado = await _proveedorService.EliminarAsync(proveedorId);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró el proveedor con ID {proveedorId}." });

            return Ok(new { mensaje = "Proveedor eliminado correctamente." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar proveedor con ID {ProveedorId}", proveedorId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al eliminar el proveedor.",
                detalle = ex.Message
            });
        }
    }

    // ===================== Compras =====================

    // GET api/proveedor/compra/sucursal/{sucursalId}
    [HttpGet("compra/sucursal/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetComprasBySucursalAsync(int sucursalId)
    {
        try
        {
            var compras = await _compraProveedorService.GetAllBySucursalAsync(sucursalId);
            return Ok(compras ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener compras de la sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener las compras.",
                detalle = ex.Message
            });
        }
    }

    // GET api/proveedor/compra/proveedor/{proveedorId}
    [HttpGet("compra/proveedor/{proveedorId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetComprasByProveedorAsync(int proveedorId)
    {
        try
        {
            var compras = await _compraProveedorService.GetAllByProveedorAsync(proveedorId);
            return Ok(compras ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener compras del proveedor {ProveedorId}", proveedorId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener las compras.",
                detalle = ex.Message
            });
        }
    }

    // GET api/proveedor/compra/documento/{docReferencia}/{sucursalId}
    [HttpGet("compra/documento/{docReferencia}/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetComprasByDocReferenciaAsync(string docReferencia, int sucursalId)
    {
        try
        {
            var compras = await _compraProveedorService.GetByDocReferenciaAsync(docReferencia, sucursalId);
            return Ok(compras ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener compras del documento {DocReferencia}", docReferencia);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener las compras.",
                detalle = ex.Message
            });
        }
    }

    // POST api/proveedor/compra
    [HttpPost("compra")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegistrarCompraAsync([FromBody] RegistrarCompraProveedorDTO dto)
    {
        try
        {
            var compraCreada = await _compraProveedorService.RegistrarAsync(dto);
            return StatusCode(StatusCodes.Status201Created, compraCreada);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar compra del proveedor {ProveedorId}", dto.ProveedorId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al registrar la compra.",
                detalle = ex.Message
            });
        }
    }

    // DELETE api/proveedor/compra/{id}
    [HttpDelete("compra/{compraProveedorId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EliminarCompraAsync(int compraProveedorId)
    {
        try
        {
            var resultado = await _compraProveedorService.EliminarAsync(compraProveedorId);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró la compra con ID {compraProveedorId}." });

            return Ok(new { mensaje = "Compra eliminada correctamente." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar compra con ID {CompraProveedorId}", compraProveedorId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al eliminar la compra.",
                detalle = ex.Message
            });
        }
    }
}
