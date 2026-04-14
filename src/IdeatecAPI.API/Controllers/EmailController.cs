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
    public async Task<IActionResult> Send([FromForm] string toEmail,
                                      [FromForm] string toName,
                                      [FromForm] string subject,
                                      [FromForm] string body,
                                      [FromForm] string tipo = "0",
                                      [FromForm] string? comprobanteJson = null,
                                      [FromForm] string? guiaJson = null,
                                      IFormFile? adjunto = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail) ||
            string.IsNullOrWhiteSpace(toName) ||
            string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(body))
            return BadRequest(new { success = false, message = "Todos los campos son requeridos." });

        var tipoEnum = tipo switch
        {
            "1" => TipoComprobante.Factura,
            "3" => TipoComprobante.Boleta,
            "9" => TipoComprobante.GuiaRemision,
            _ => TipoComprobante.Texto
        };

        // Deserializar comprobante/guia desde JSON string
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var comprobante = comprobanteJson != null
            ? System.Text.Json.JsonSerializer.Deserialize<DatosComprobante>(comprobanteJson, jsonOptions)
            : null;

        var guia = guiaJson != null
            ? System.Text.Json.JsonSerializer.Deserialize<DatosGuiaRemision>(guiaJson, jsonOptions)
            : null;

        // Leer bytes del PDF
        byte[]? adjuntoBytes = null;
        string? nombreAdjunto = null;
        if (adjunto != null)
        {
            using var ms = new MemoryStream();
            await adjunto.CopyToAsync(ms);
            adjuntoBytes = ms.ToArray();
            nombreAdjunto = adjunto.FileName;
        }

        var result = await _mediator.Send(new SendEmailCommand(
            toEmail, toName, subject, body,
            tipoEnum, comprobante, guia,
            adjuntoBytes, nombreAdjunto
        ));

        if (!result.Success)
            return StatusCode(500, new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message });
    }
}