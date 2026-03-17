namespace IdeatecAPI.Application.Features.Email.SendEmail;

public record SendEmailDto(
    string ToEmail,
    string ToName,
    string Subject,
    string Body,
    string Tipo = "0",                        // "0","1","3","9"
    DatosComprobante? Comprobante = null,
    DatosGuiaRemision? Guia = null
);