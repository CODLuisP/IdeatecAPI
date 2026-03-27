using IdeatecAPI.Application.Features.Email.SendEmail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class EmailController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("send")]
    [AllowAnonymous]
    public async Task<IActionResult> Send([FromBody] SendEmailDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ToEmail) ||
            string.IsNullOrWhiteSpace(request.ToName)  ||
            string.IsNullOrWhiteSpace(request.Subject) ||
            string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { success = false, message = "Todos los campos son requeridos." });

        // Mapear string → enum
        var tipo = request.Tipo switch
        {
            "1" => TipoComprobante.Factura,
            "3" => TipoComprobante.Boleta,
            "9" => TipoComprobante.GuiaRemision,
            _   => TipoComprobante.Texto
        };

        // Validar que lleguen los datos necesarios según el tipo
        if (tipo is TipoComprobante.Factura or TipoComprobante.Boleta && request.Comprobante is null)
            return BadRequest(new { success = false, message = "Se requieren datos del comprobante para este tipo." });

        if (tipo == TipoComprobante.GuiaRemision && request.Guia is null)
            return BadRequest(new { success = false, message = "Se requieren datos de la guía de remisión." });

        var result = await _mediator.Send(new SendEmailCommand(
            request.ToEmail,
            request.ToName,
            request.Subject,
            request.Body,
            tipo,
            request.Comprobante,
            request.Guia
        ));

        if (!result.Success)
            return StatusCode(500, new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message });
    }
}