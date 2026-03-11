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

    // ✅ POST: api/producto
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarProductoDTO dto)
    {
        await _productoService.RegistrarProductoAsync(dto);

        return StatusCode(StatusCodes.Status201Created, 
            new { mensaje = "Producto registrado correctamente" });
    }

    // ✅ PUT: api/producto
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Editar([FromBody] EditarProductoDTO dto)
    {
        await _productoService.EditarProductoAsync(dto);

        return Ok(new { mensaje = "Producto actualizado correctamente" });
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
