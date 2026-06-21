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
    IEnumerable<(byte[] Bytes, string Nombre)>? Adjuntos = null
) : IRequest<SendEmailResult>;

public record SendEmailResult(bool Success, string Message);