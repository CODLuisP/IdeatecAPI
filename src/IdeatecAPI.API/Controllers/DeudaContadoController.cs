using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IdeatecAPI.Application.Features.DeudaContado.Services;
using IdeatecAPI.Application.Features.DeudaContado.DTOs;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeudaContadoController : ControllerBase
{
    private readonly IDeudaContadoService _deudaContadoService;

    public DeudaContadoController(IDeudaContadoService deudaContadoService)
    {
        _deudaContadoService = deudaContadoService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDeudaContado(
        [FromQuery] string empresaRuc,
        [FromQuery] string? establecimientoAnexo,
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] string? clienteNumDoc)
    {
        if (string.IsNullOrEmpty(empresaRuc))
            return BadRequest(new { message = "El RUC de la empresa es obligatorio" });

        var result = await _deudaContadoService.GetDeudaContadoAsync(
            empresaRuc,
            establecimientoAnexo,
            fechaInicio,
            fechaFin,
            clienteNumDoc);

        return Ok(result);
    }

    [HttpGet("{pagoId}/historial")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistorialPagos(int pagoId)
    {
        try
        {
            var historial = await _deudaContadoService.GetHistorialPagosByPagoIdAsync(pagoId);

            if (!historial.Any())
                return NotFound(new { message = $"No se encontró historial de pagos para el pago {pagoId}" });

            return Ok(historial);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{pagoId}/pagar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegistrarPago(int pagoId, [FromBody] RegistrarPagoDeudaContadoDto dto)
    {
        if (pagoId != dto.PagoId)
            return BadRequest(new { message = "El ID de la URL no coincide con el del cuerpo" });

        try
        {
            var result = await _deudaContadoService.RegistrarPagoAsync(dto);

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

    [HttpGet("excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DescargarExcel(
        [FromQuery] string empresaRuc,
        [FromQuery] string? establecimientoAnexo,
        [FromQuery] string? clienteNumDoc,
        [FromQuery] DateTime? fechaInicio,
        [FromQuery] DateTime? fechaFin,
        [FromQuery] string? tituloReporte)
    {
        if (string.IsNullOrEmpty(empresaRuc))
            return BadRequest(new { message = "El RUC de la empresa es obligatorio" });

        try
        {
            var filtro = new ReporteDeudaContadoFiltroDto
            {
                EmpresaRuc           = empresaRuc,
                EstablecimientoAnexo = establecimientoAnexo,
                ClienteNumDoc        = clienteNumDoc,
                FechaInicio          = fechaInicio,
                FechaFin             = fechaFin,
                TituloReporte = tituloReporte
            };

            var bytes = await _deudaContadoService.GenerarExcelAsync(filtro);
            var nombreArchivo = $"Deudas_Contado_{empresaRuc}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombreArchivo);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}