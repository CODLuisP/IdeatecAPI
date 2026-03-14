using MediatR;
using IdeatecAPI.Application.Common.Interfaces.Persistence;

namespace IdeatecAPI.Application.Features.Demo.SendDemoRequest;

public class SendDemoRequestHandler : IRequestHandler<SendDemoRequestCommand, SendDemoRequestResult>
{
  private readonly IEmailService _emailService;

  public SendDemoRequestHandler(IEmailService emailService)
  {
    _emailService = emailService;
  }

  public async Task<SendDemoRequestResult> Handle(SendDemoRequestCommand request, CancellationToken cancellationToken)
  {
    var html = $"""
            <div style="font-family: sans-serif; max-width: 520px; margin: 0 auto; background: #f8fafc; padding: 32px; border-radius: 12px;">
              <div style="background: #0f2e64; padding: 24px; border-radius: 10px; margin-bottom: 24px;">
                <h1 style="color: white; margin: 0; font-size: 20px;">
                  IDEA<span style="color: #ef4444;">TEC</span> — Nueva Demo Solicitada
                </h1>
              </div>
              <div style="background: white; border-radius: 10px; padding: 24px; border: 1px solid #e2e8f0;">
                <p style="color: #64748b; font-size: 14px; margin-top: 0;">
                  Un usuario ha solicitado una demostración personalizada desde la página de login.
                </p>
                <table style="width: 100%; border-collapse: collapse; margin-top: 16px;">
                  <tr>
                    <td style="padding: 10px 0; border-bottom: 1px solid #f1f5f9; color: #94a3b8; font-size: 13px; width: 40%;">Nombre</td>
                    <td style="padding: 10px 0; border-bottom: 1px solid #f1f5f9; color: #0f172a; font-size: 13px; font-weight: 600;">{request.Name}</td>
                  </tr>
                  <tr>
                    <td style="padding: 10px 0; border-bottom: 1px solid #f1f5f9; color: #94a3b8; font-size: 13px;">Empresa / RUC</td>
                    <td style="padding: 10px 0; border-bottom: 1px solid #f1f5f9; color: #0f172a; font-size: 13px; font-weight: 600;">{request.Company}</td>
                  </tr>
                  <tr>
                    <td style="padding: 10px 0; color: #94a3b8; font-size: 13px;">Teléfono</td>
                    <td style="padding: 10px 0; color: #0f172a; font-size: 13px; font-weight: 600;">{request.Phone}</td>
                  </tr>
                </table>
                <div style="margin-top: 20px; background: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 8px; padding: 14px;">
                  <p style="margin: 0; font-size: 13px; color: #15803d;">
                    Recuerda contactar al cliente en las próximas <strong>24 horas hábiles</strong>.
                  </p>
                </div>
              </div>
              <p style="text-align: center; color: #cbd5e1; font-size: 11px; margin-top: 20px;">
                IDEATEC S.A.C. · Sistema de Facturación Electrónica · Perú
              </p>
            </div>
        """;

    await _emailService.SendEmailAsync(toEmail: "velsatsac823@gmail.com", subject: "Nueva solicitud de demostración — IDEATEC", htmlBody: html);

    return new SendDemoRequestResult(true, "Solicitud enviada correctamente.");
  }
}