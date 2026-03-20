using IdeatecAPI.Application.Features.Sucursal.DTOs;
using IdeatecAPI.Application.Features.Sucursal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SucursalController : ControllerBase
{
    private readonly ISucursalService _sucursalService;
    private readonly ILogger<SucursalController> _logger;

    public SucursalController(ISucursalService sucursalService, ILogger<SucursalController> logger)
    {
        _sucursalService = sucursalService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerTodos()
    {
        try
        {
            var ruc = User.FindFirst("ruc")?.Value;
            if (string.IsNullOrEmpty(ruc))
                return Unauthorized(new { mensaje = "RUC no encontrado en el token" });

            var resultado = await _sucursalService.GetByRucSucursalAsync(ruc);
            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener sucursales");
            return StatusCode(500, new { mensaje = "Error al obtener las sucursales.", detalle = ex.Message });
        }
    }

    [HttpGet("{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerPorId(int sucursalId)
    {
        try
        {
            var resultado = await _sucursalService.GetByIdSucursalAsync(sucursalId);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Sucursal no encontrada: ID {SucursalId}", sucursalId);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener sucursal con ID {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al obtener la sucursal.",
                detalle = ex.Message
            });
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Registrar([FromBody] AgregarSucursalDTO dto)
    {
        try
        {
            // Tomar datos del token
            dto.EmpresaRuc = User.FindFirst("ruc")?.Value ?? "";
            dto.EmailAdmin = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            dto.UsernameAdminActual = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";

            var sucursalCreada = await _sucursalService.RegistrarSucursalAsync(dto);
            return CreatedAtAction(nameof(ObtenerPorId), new { sucursalId = sucursalCreada.SucursalId }, sucursalCreada);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Intento de registro duplicado: RUC {EmpresaRuc}, Establecimiento {CodEstablecimiento}", dto.EmpresaRuc, dto.CodEstablecimiento);
            return Conflict(new { mensaje = ex.Message });
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1062)
        {
            _logger.LogWarning("Duplicado en base de datos: RUC {EmpresaRuc}, Establecimiento {CodEstablecimiento}", dto.EmpresaRuc, dto.CodEstablecimiento);
            return Conflict(new
            {
                mensaje = $"Ya existe una sucursal con RUC '{dto.EmpresaRuc}' y código de establecimiento '{dto.CodEstablecimiento}'."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar sucursal para RUC {EmpresaRuc}", dto.EmpresaRuc);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al registrar la sucursal.",
                detalle = ex.Message
            });
        }
    }

    [HttpPut("{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Editar(int sucursalId, [FromBody] EditarSucursalDTO dto)
    {
        try
        {
            dto.SucursalId = sucursalId;
            var resultado = await _sucursalService.EditarSucursalAsync(dto);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró la sucursal con ID {sucursalId}." });

            return Ok(new { mensaje = "Sucursal actualizada correctamente." });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Sucursal no encontrada al editar: ID {SucursalId}", sucursalId);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar sucursal con ID {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al editar la sucursal.",
                detalle = ex.Message
            });
        }
    }

    [HttpDelete("{sucursalId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Eliminar(int sucursalId)
    {
        try
        {
            var resultado = await _sucursalService.EliminarSucursalAsync(sucursalId);

            if (!resultado)
                return NotFound(new { mensaje = $"No se encontró la sucursal con ID {sucursalId}." });

            return Ok(new { mensaje = "Sucursal eliminada correctamente." });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Sucursal no encontrada al eliminar: ID {SucursalId}", sucursalId);
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar sucursal con ID {SucursalId}", sucursalId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                mensaje = "Ocurrió un error al eliminar la sucursal.",
                detalle = ex.Message
            });
        }
    }
}