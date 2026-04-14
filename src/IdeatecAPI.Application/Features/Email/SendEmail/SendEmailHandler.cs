using IdeatecAPI.Application.Features.Email.SendEmail;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using MediatR;

namespace IdeatecAPI.Application.Features.Email.SendEmail;

public class SendEmailHandler : IRequestHandler<SendEmailCommand, SendEmailResult>
{
    private readonly IEmailService _emailService;

    public SendEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<SendEmailResult> Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var html = request.Tipo switch
            {
                TipoComprobante.Factura or TipoComprobante.Boleta =>
                    EmailTemplateBuilder.BuildComprobanteEmail(
                        request.ToName, request.Subject, request.Body,
                        request.Tipo, request.Comprobante!),

                TipoComprobante.GuiaRemision =>
                    EmailTemplateBuilder.BuildGuiaEmail(
                        request.ToName, request.Subject, request.Body,
                        request.Guia!),

                _ => EmailTemplateBuilder.BuildTextEmail(
                        request.ToName, request.Subject, request.Body)
            };

            await _emailService.SendEmailAsync(
                request.ToEmail,
                request.Subject,
                html,
                request.Adjunto,        // ← nuevo
                request.NombreAdjunto   // ← nuevo
            );
            return new SendEmailResult(true, "Correo enviado correctamente.");
        }
        catch (Exception ex)
        {
            return new SendEmailResult(false, $"Error al enviar el correo: {ex.Message}");
        }
    }
}