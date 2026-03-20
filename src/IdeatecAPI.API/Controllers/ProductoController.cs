using IdeatecAPI.Application.Features.Productos.DTO;
using IdeatecAPI.Application.Features.Productos.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductoController : ControllerBase
{
    private readonly IProductoService _productoService;
    private readonly ILogger<ProductoController> _logger;

    public ProductoController(IProductoService productoService, ILogger<ProductoController> logger)
    {
        _productoService = productoService;
        _logger = logger;
    }

    [HttpGet("{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(int sucursalId)
    {
        try
        {
            var productos = await _productoService.GetAllProductosAsync(sucursalId);

            if (productos == null || !productos.Any())
                return NotFound(new { mensaje = $"No se encontraron productos para la sucursal con ID {sucursalId}." });

            return Ok(productos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener productos de la sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los productos.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("base/{empresaRuc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllBaseRuc(string empresaRuc)
    {
        try
        {
            var productos = await _productoService.GetAllProductosBaseRucAsync(empresaRuc);

            if (productos == null || !productos.Any())
                return NotFound(new { mensaje = $"No se encontraron productos para el RUC '{empresaRuc}'." });

            return Ok(productos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener productos base para RUC {EmpresaRuc}", empresaRuc);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los productos.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("disponibles/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProductosDisponibles(int sucursalId)
    {
        try
        {
            var productos = await _productoService.GetProductosRucDisponiblesAsync(sucursalId);

            if (productos == null || !productos.Any())
                return NotFound(new { mensaje = "No hay productos disponibles para agregar a esta sucursal." });

            return Ok(productos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener productos disponibles para sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los productos disponibles.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("detalle/{productoId:int}/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int productoId, int sucursalId)
    {
        try
        {
            var producto = await _productoService.GetProductoByIdAsync(productoId, sucursalId);

            if (producto == null)
                return NotFound(new { mensaje = $"No se encontró el producto con ID {productoId} en la sucursal {sucursalId}." });

            return Ok(producto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener producto ID {ProductoId} en sucursal {SucursalId}", productoId, sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el producto.",
                detalle = ex.Message
            });
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarProductoDTO dto)
    {
        try
        {
            var productoCreado = await _productoService.RegistrarProductoAsync(dto);
        return CreatedAtAction(nameof(GetById), new { productoId = productoCreado.ProductoId, sucursalId = dto.SucursalId }, productoCreado);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Intento de registro duplicado: Código {Codigo}", dto.Codigo);
            return Conflict(new { mensaje = ex.Message });
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1062)
        {
            _logger.LogWarning("Duplicado en base de datos: Código {Codigo}", dto.Codigo);
            return Conflict(new { mensaje = $"Ya existe un producto con el código '{dto.Codigo}'." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar producto con código {Codigo}", dto.Codigo);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al registrar el producto.",
                detalle = ex.Message
            });
        }
    }

    [HttpPut("{productoId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Editar(int productoId, [FromBody] EditarProductoDTO dto)
    {
        try
        {
            dto.ProductoId = productoId;
            var resultado = await _productoService.EditarProductoAsync(dto);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró el producto con ID {productoId}." });

            return Ok(new { mensaje = "Producto actualizado correctamente." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Argumento inválido al editar producto ID {ProductoId}", productoId);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar producto con ID {ProductoId}", productoId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al editar el producto.",
                detalle = ex.Message
            });
        }
    }

    [HttpDelete("{sucursalProductoId:int}")]
    public async Task<IActionResult> Eliminar(int sucursalProductoId)
    {
        try
        {
            var resultado = await _productoService.EliminarSucursalProductoAsync(sucursalProductoId);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró el producto con SucursalProductoId {sucursalProductoId}." });

            return Ok(new { mensaje = "Producto desactivado de la sucursal correctamente." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Argumento inválido al eliminar SucursalProductoId {SucursalProductoId}", sucursalProductoId);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar SucursalProductoId {SucursalProductoId}", sucursalProductoId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al eliminar el producto de la sucursal.",
                detalle = ex.Message
            });
        }
    }

    [HttpGet("{empresaRuc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllRuc(string empresaRuc)
    {
        try
        {
            var productos = await _productoService.GetAllProductosRucAsync(empresaRuc);

            if (productos == null || !productos.Any())
                return NotFound(new { mensaje = $"No se encontraron productos para el RUC '{empresaRuc}'." });

            return Ok(productos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener productos para RUC {EmpresaRuc}", empresaRuc);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los productos.",
                detalle = ex.Message
            });
        }
    }
}