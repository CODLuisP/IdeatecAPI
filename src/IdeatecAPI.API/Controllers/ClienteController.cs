using IdeatecAPI.Application.Features.Clientes.DTOs;
using IdeatecAPI.Application.Features.Clientes.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClienteController : ControllerBase
{
    private readonly IClienteService _clienteService;
    private readonly ILogger<ClienteController> _logger;

    public ClienteController(IClienteService clienteService, ILogger<ClienteController> logger)
    {
        _clienteService = clienteService;
        _logger = logger;
    }

    // GET api/cliente/ruc/{empresaRuc}
    [HttpGet("ruc/{empresaRuc}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllClientesRucAsync(string empresaRuc)
    {
        try
        {
            var clientes = await _clienteService.GetAllClientesRucAsync(empresaRuc);

            if (clientes == null || !clientes.Any())
                return NotFound(new { mensaje = $"No se encontraron clientes para el RUC '{empresaRuc}'." });

            return Ok(clientes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener clientes para RUC {EmpresaRuc}", empresaRuc);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los clientes.",
                detalle = ex.Message
            });
        }
    }

    // GET api/cliente/sucursal/{sucursalId}
    [HttpGet("sucursal/{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllClientesSucursalAsync(int sucursalId)
    {
        try
        {
            var clientes = await _clienteService.GetAllClientesSucursalAsync(sucursalId);

            if (clientes == null || !clientes.Any())
                return NotFound(new { mensaje = $"No se encontraron clientes para la sucursal con ID {sucursalId}." });

            return Ok(clientes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener clientes de la sucursal {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener los clientes.",
                detalle = ex.Message
            });
        }
    }

    // GET api/cliente/detalle/{empresaRuc}/{clienteId}
    [HttpGet("detalle/{empresaRuc}/{clienteId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetClienteByIdEmpresaAsync(string empresaRuc, int clienteId)
    {
        try
        {
            var cliente = await _clienteService.GetClienteByIdEmpresaAsync(empresaRuc, clienteId);

            if (cliente == null)
                return NotFound(new { mensaje = $"No se encontró el cliente con ID {clienteId} para el RUC '{empresaRuc}'." });

            return Ok(cliente);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Argumento inválido al obtener cliente ID {ClienteId} para RUC {EmpresaRuc}", clienteId, empresaRuc);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cliente ID {ClienteId} para RUC {EmpresaRuc}", clienteId, empresaRuc);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener el cliente.",
                detalle = ex.Message
            });
        }
    }

    // POST api/cliente
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegistrarClienteAsync([FromBody] RegistrarClienteDTO dto)
    {
        try
        {
            var clienteCreado = await _clienteService.RegistrarClienteAsync(dto);
            return StatusCode(StatusCodes.Status201Created, clienteCreado);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Intento de registro duplicado: Documento {NumeroDocumento}", dto.NumeroDocumento);
            return Conflict(new { mensaje = ex.Message });
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1062)
        {
            _logger.LogWarning("Duplicado en base de datos: Documento {NumeroDocumento}", dto.NumeroDocumento);
            return Conflict(new { mensaje = $"Ya existe un cliente con el documento '{dto.NumeroDocumento}'." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar cliente con documento {NumeroDocumento}", dto.NumeroDocumento);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al registrar el cliente.",
                detalle = ex.Message
            });
        }
    }

    // PUT api/cliente/{id}
    [HttpPut("{clienteId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EditarClienteAsync(int clienteId, [FromBody] EditarClienteDTO dto)
    {
        try
        {
            dto.ClienteId = clienteId;
            var resultado = await _clienteService.EditarClienteAsync(dto);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró el cliente con ID {clienteId}." });

            return Ok(new { mensaje = "Cliente actualizado correctamente." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Argumento inválido al editar cliente ID {ClienteId}", clienteId);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar cliente con ID {ClienteId}", clienteId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al editar el cliente.",
                detalle = ex.Message
            });
        }
    }

    // DELETE api/cliente/{id}
    [HttpDelete("{clienteId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EliminarClienteAsync(int clienteId)
    {
        try
        {
            var resultado = await _clienteService.EliminarClienteAsync(clienteId);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró el cliente con ID {clienteId}." });

            return Ok(new { mensaje = "Cliente eliminado correctamente." });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Argumento inválido al eliminar cliente ID {ClienteId}", clienteId);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar cliente con ID {ClienteId}", clienteId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al eliminar el cliente.",
                detalle = ex.Message
            });
        }
    }
}