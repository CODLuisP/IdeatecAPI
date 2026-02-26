using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Features.Clientes.DTOs;
using IdeatecAPI.Application.Features.Clientes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IdeatecAPI.API.Controllers; 

[Route("[controller]")]
public class ClienteController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClienteController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var clientes = await _clienteService.GetAllClientesAsync();
        return Ok(clientes);
        
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarClienteDTO dto)
    {
        var clienteId = await _clienteService.RegistrarClienteAsync(dto);
        return StatusCode(201, new { clienteId, mensaje = "Cliente registrado correctamente" });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarClienteDTO dto)
    {
        if (id != dto.ClienteId)
            return BadRequest("El id de la URL no coincide con el del cuerpo.");

        var result = await _clienteService.EditarClienteAsync(dto);

        if (!result)
            return BadRequest("No se pudo actualizar el cliente.");

        return Ok(new { mensaje = "Cliente actualizado correctamente" });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Eliminar(int id)
    {
        var result = await _clienteService.EliminarClienteAsync(id);

        if (!result)
            return BadRequest("No se pudo eliminar el cliente.");

        return Ok(new { mensaje = "Cliente eliminado correctamente" });
    }
}