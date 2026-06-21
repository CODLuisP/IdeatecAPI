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
                                      IFormFile? pdf = null,
                                      IFormFile? xml = null)
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

        // Construir lista de adjuntos (pdf y/o xml)
        var adjuntos = new List<(byte[] Bytes, string Nombre)>();
        foreach (var archivo in new[] { pdf, xml })
        {
            if (archivo is null) continue;
            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            adjuntos.Add((ms.ToArray(), archivo.FileName));
        }

        var result = await _mediator.Send(new SendEmailCommand(
            toEmail, toName, subject, body,
            tipoEnum, comprobante, guia,
            adjuntos.Count > 0 ? adjuntos : null
        ));

        if (!result.Success)
            return StatusCode(500, new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message });
    }

    [HttpPost("notificar")]
    [AllowAnonymous]
    public async Task<IActionResult> Notificar([FromForm] string toEmail,
                                              [FromForm] string toName,
                                              [FromForm] string subject,
                                              [FromForm] string mensaje)
    {
        if (string.IsNullOrWhiteSpace(toEmail) ||
            string.IsNullOrWhiteSpace(toName) ||
            string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(mensaje))
        {
            return BadRequest(new { success = false, message = "Los campos toEmail, toName, subject y mensaje son requeridos." });
        }

        var body = EmailTemplateBuilder.BuildNotificacionVencimientoServicio(toName, subject, mensaje);

        var result = await _mediator.Send(new SendEmailCommand(
            toEmail,
            toName,
            subject,
            body,
            TipoComprobante.Notificacion
        ));

        if (!result.Success)
            return StatusCode(500, new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message });
    }
}