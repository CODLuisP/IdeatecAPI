using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Features.Productos.DTO;
using IdeatecAPI.Application.Features.Productos.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductoController : ControllerBase
{
    private readonly IProductoService _productoService;

    public ProductoController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    // ✅ GET: api/producto
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var productos = await _productoService.GetAllProductosAsync();
        return Ok(productos);
    }

    // ✅ GET: api/producto/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var producto = await _productoService.GetProductoByIdAsync(id);

        if (producto == null)
            return NotFound();

        return Ok(producto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarProductoDTO dto)
    {
        try
        {
            var producto = await _productoService.RegistrarProductoAsync(dto);
            return StatusCode(201, producto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message }); // 409
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarProductoDTO dto)
    {
        try
        {
            var result = await _productoService.EditarProductoAsync(dto);

            if (!result)
                return BadRequest(new { mensaje = "No se pudo actualizar el producto." });

            return Ok(new { mensaje = "Producto actualizado correctamente" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    // ✅ DELETE: api/producto/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _productoService.EliminarProductoAsync(id);

        return Ok(new { mensaje = $"Producto id = {id} eliminado correctamente" });
    }

}
