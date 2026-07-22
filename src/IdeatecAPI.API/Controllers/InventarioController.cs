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

    // GET api/inventario/rentabilidad/producto/{sucursalProductoId}/detalle?desde=&hasta=
    [HttpGet("rentabilidad/producto/{sucursalProductoId:int}/detalle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRentabilidadDiariaAsync(int sucursalProductoId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        try
        {
            var detalle = await _inventarioPepsService.GetRentabilidadDiariaAsync(sucursalProductoId, desde, hasta);
            return Ok(detalle ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el detalle diario de rentabilidad del SucursalProductoId {SucursalProductoId}", sucursalProductoId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el detalle de rentabilidad.",
                detalle = ex.Message
            });
        }
    }

    // GET api/inventario/vencidos/sucursal/{sucursalId}
    // Vista previa de solo lectura de los lotes vencidos (no descuenta stock ni desactiva nada).
    // Se usa para mostrar al usuario qué se va a retirar antes de confirmar con retirar-vencidos.
    [HttpGet("vencidos/sucursal/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLotesVencidosSucursalAsync(int sucursalId)
    {
        try
        {
            var lotesVencidos = await _inventarioPepsService.GetLotesVencidosReporteAsync(sucursalId);
            return Ok(lotesVencidos ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener los lotes vencidos de la sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los productos vencidos.",
                detalle = ex.Message
            });
        }
    }

    // PUT api/inventario/lote/{inventarioLoteId}/fecha-vencimiento
    // Corrige la fecha de vencimiento de un lote ya registrado (p.ej. error al registrar la compra).
    // No afecta cantidad, costo ni Kardex. Solo aplica sobre lotes activos (estado = 1);
    // uno ya dado de baja por vencimiento se considera historia cerrada y no se puede editar.
    [HttpPut("lote/{inventarioLoteId:int}/fecha-vencimiento")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActualizarFechaVencimientoLoteAsync(int inventarioLoteId, [FromBody] ActualizarFechaVencimientoDTO dto)
    {
        try
        {
            var actualizado = await _inventarioPepsService.ActualizarFechaVencimientoLoteAsync(inventarioLoteId, dto.FechaVencimiento);
            if (!actualizado)
                return NotFound(new { mensaje = "No se encontró el lote o ya no está activo." });

            return Ok(new { mensaje = "Fecha de vencimiento actualizada correctamente." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar la fecha de vencimiento del lote {InventarioLoteId}", inventarioLoteId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al actualizar la fecha de vencimiento.",
                detalle = ex.Message
            });
        }
    }

    // POST api/inventario/retirar-vencidos
    // Retira lotes cuya fecha de vencimiento ya pasó: pone saldoCantidad = 0, estado = 0,
    // registra Kardex SALIDA_VENCIMIENTO y descuenta el stock del producto.
    // Si se omite sucursalProductoId, procesa todos los productos de todas las sucursales.
    [HttpPost("retirar-vencidos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RetirarVencidosAsync([FromQuery] int? sucursalProductoId)
    {
        try
        {
            var resultado = await _inventarioPepsService.RetirarLotesVencidosAsync(sucursalProductoId);
            return Ok(new
            {
                mensaje = resultado.TotalLotesRetirados > 0
                    ? "Productos vencidos retirados correctamente."
                    : "No se encontraron lotes vencidos para retirar.",
                resultado.TotalLotesRetirados,
                resultado.TotalProductosAfectados,
                resultado.TotalCantidadRetirada,
                resultado.TotalCostoRetirado
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al retirar productos vencidos");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al retirar productos vencidos.",
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
