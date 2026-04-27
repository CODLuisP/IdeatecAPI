using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IdeatecAPI.Application.Features.CuentasPorCobrar.Services;
using IdeatecAPI.Application.Features.CuentasPorCobrar.DTOs;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CuentasPorCobrarController : ControllerBase
{
    private readonly ICuentasPorCobrarService _cuentasPorCobrarService;

    public CuentasPorCobrarController(ICuentasPorCobrarService cuentasPorCobrarService)
    {
        _cuentasPorCobrarService = cuentasPorCobrarService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCuentasPorCobrar(
        [FromQuery] string empresaRuc,
        [FromQuery] string? establecimientoAnexo,
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] string? clienteNumDoc)
    {
        if (string.IsNullOrEmpty(empresaRuc))
            return BadRequest(new { message = "El RUC de la empresa es obligatorio" });

        var result = await _cuentasPorCobrarService.GetCuentasPorCobrarAsync(
            empresaRuc,
            establecimientoAnexo,
            fechaInicio,
            fechaFin,
            clienteNumDoc);

        return Ok(result);
    }

    [HttpGet("{comprobanteId}/cuotas")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCuotas(int comprobanteId)
    {
        try
        {
            var cuotas = await _cuentasPorCobrarService.GetCuotasByComprobanteIdAsync(comprobanteId);

            if (!cuotas.Any())
                return NotFound(new { message = $"No se encontraron cuotas para el comprobante {comprobanteId}" });

            return Ok(cuotas);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("cuotas/{cuotaId}/pagar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PagarCuota(int cuotaId, [FromBody] PagarCuotaDto dto)
    {
        if (cuotaId != dto.CuotaId)
            return BadRequest(new { message = "El ID de la URL no coincide con el del cuerpo" });

        try
        {
            var result = await _cuentasPorCobrarService.PagarCuotaAsync(dto);

            if (!result)
                return BadRequest(new { message = "No se pudo registrar el pago" });

            return Ok(new { message = "Pago registrado correctamente" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}