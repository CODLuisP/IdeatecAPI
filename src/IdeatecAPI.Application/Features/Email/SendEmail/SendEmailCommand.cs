using MediatR; 
namespace IdeatecAPI.Application.Features.Email.SendEmail;

public record SendEmailCommand(
    string ToEmail,
    string ToName,
    string Subject,
    string Body,
    TipoComprobante Tipo = TipoComprobante.Texto,
    DatosComprobante? Comprobante = null,
    DatosGuiaRemision? Guia = null,
    byte[]? Adjunto = null,          // ← nuevo
    string? NombreAdjunto = null     // ← nuevo
) : IRequest<SendEmailResult>;

public record SendEmailResult(bool Success, string Message);