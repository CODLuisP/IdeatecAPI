using IdeatecAPI.Application.Features.Inventario.DTOs;
using IdeatecAPI.Application.Features.Inventario.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventarioController : ControllerBase
{
    private readonly IInventarioPepsService _inventarioPepsService;
    private readonly ILogger<InventarioController> _logger;

    public InventarioController(IInventarioPepsService inventarioPepsService, ILogger<InventarioController> logger)
    {
        _inventarioPepsService = inventarioPepsService;
        _logger = logger;
    }

    // GET api/inventario/kardex/{sucursalProductoId}?desde=&hasta=
    [HttpGet("kardex/{sucursalProductoId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetKardexAsync(int sucursalProductoId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        try
        {
            var kardex = await _inventarioPepsService.GetKardexAsync(sucursalProductoId, desde, hasta);
            return Ok(kardex ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el kardex del SucursalProductoId {SucursalProductoId}", sucursalProductoId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el kardex.",
                detalle = ex.Message
            });
        }
    }

    // GET api/inventario/stock-valorizado/producto/{sucursalProductoId}
    [HttpGet("stock-valorizado/producto/{sucursalProductoId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStockValorizadoProductoAsync(int sucursalProductoId)
    {
        try
        {
            var valorizado = await _inventarioPepsService.GetStockValorizadoAsync(sucursalProductoId);
            return Ok(valorizado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el stock valorizado del SucursalProductoId {SucursalProductoId}", sucursalProductoId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el stock valorizado.",
                detalle = ex.Message
            });
        }
    }

    // GET api/inventario/stock-valorizado/sucursal/{sucursalId}
    [HttpGet("stock-valorizado/sucursal/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStockValorizadoSucursalAsync(int sucursalId)
    {
        try
        {
            var valorizado = await _inventarioPepsService.GetStockValorizadoSucursalAsync(sucursalId);
            return Ok(valorizado ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el stock valorizado de la sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el stock valorizado.",
                detalle = ex.Message
            });
        }
    }

    // GET api/inventario/rentabilidad/sucursal/{sucursalId}?desde=&hasta=
    [HttpGet("rentabilidad/sucursal/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRentabilidadSucursalAsync(int sucursalId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        try
        {
            var rentabilidad = await _inventarioPepsService.GetRentabilidadPorProductoAsync(sucursalId, desde, hasta);
            return Ok(rentabilidad ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la rentabilidad de la sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener la rentabilidad.",
                detalle = ex.Message
            });
        }
    }

    // POST api/inventario/saldo-inicial
    // Backfill de un único uso: crea el lote inicial PEPS para productos con stock existente
    // (usa Último costo de compra como costo de referencia). Omite productos que ya tengan
    // un lote de saldo inicial registrado, así que es seguro reintentar.
    [HttpPost("saldo-inicial")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegistrarSaldoInicialAsync([FromBody] IEnumerable<RegistrarSaldoInicialDTO> items)
    {
        try
        {
            var creados = await _inventarioPepsService.RegistrarSaldoInicialAsync(items);
            return Ok(new { mensaje = "Saldo inicial registrado correctamente.", lotesCreados = creados });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar el saldo inicial de inventario");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al registrar el saldo inicial.",
                detalle = ex.Message
            });
        }
    }
}
