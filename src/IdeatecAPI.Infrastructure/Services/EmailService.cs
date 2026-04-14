using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IdeatecAPI.Application.Common.Interfaces.Persistence;

namespace IdeatecAPI.Infrastructure.Services;

public class EmailService : IEmailService
{
  private readonly IConfiguration _config;
  private readonly ILogger<EmailService> _logger;

  public EmailService(IConfiguration config, ILogger<EmailService> logger)
  {
    _config = config;
    _logger = logger;
  }

  public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
  {
    try
    {
      var smtpConfig = _config.GetSection("Smtp");

      var message = new MimeMessage();
      message.From.Add(new MailboxAddress(
          smtpConfig["FromName"] ?? "IDEATEC Factus",
          smtpConfig["FromEmail"] ?? "noreply@ideatec.pe"
      ));
      message.To.Add(new MailboxAddress(toName, toEmail));
      message.Subject = "Recuperación de contraseña – IDEATEC Factus";

      // ── Cuerpo del email (HTML) ────────────────────────────────────
      var bodyBuilder = new BodyBuilder
      {
        HtmlBody = BuildEmailHtml(toName, resetLink),
        TextBody = $"Hola {toName},\n\nHaz clic en el siguiente enlace para restablecer tu contraseña (válido por 30 minutos):\n{resetLink}\n\nSi no solicitaste este cambio, ignora este correo.\n\n— Equipo IDEATEC"
      };
      message.Body = bodyBuilder.ToMessageBody();

      // ── Envío SMTP ─────────────────────────────────────────────────
      using var client = new SmtpClient();
      await client.ConnectAsync(
          smtpConfig["Host"],
          int.Parse(smtpConfig["Port"] ?? "587"),
          SecureSocketOptions.StartTls
      );
      await client.AuthenticateAsync(smtpConfig["Username"], smtpConfig["Password"]);
      await client.SendAsync(message);
      await client.DisconnectAsync(true);

      _logger.LogInformation("Email de reset enviado a {Email}", toEmail);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error al enviar email de reset a {Email}", toEmail);
      throw; // Re-lanzar para que el handler lo capture
    }
  }

  // ── Template HTML del email ────────────────────────────────────────────
  private static string BuildEmailHtml(string nombre, string resetLink) => $"""
        <!DOCTYPE html>
        <html lang="es">
        <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width"></head>
        <body style="margin:0;padding:0;background:#f1f5f9;font-family:Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f1f5f9;padding:40px 0;">
            <tr><td align="center">
              <table width="560" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                
                <!-- Header -->
                <tr>
                  <td style="background:#0f2e64;padding:32px 40px;text-align:center;">
                    <h1 style="margin:0;color:#ffffff;font-size:24px;font-weight:800;letter-spacing:-0.5px;">
                      IDEA<span style="color:#ef4444;">TEC</span>
                      <span style="font-weight:400;font-size:14px;color:#93c5fd;display:block;margin-top:4px;">Facturación Electrónica</span>
                    </h1>
                  </td>
                </tr>

                <!-- Body -->
                <tr>
                  <td style="padding:40px;">
                    <h2 style="margin:0 0 8px;color:#0f172a;font-size:20px;">Recuperación de contraseña</h2>
                    <p style="margin:0 0 24px;color:#64748b;font-size:15px;">Hola, <strong>{nombre}</strong></p>
                    <p style="margin:0 0 24px;color:#475569;font-size:14px;line-height:1.6;">
                      Recibimos una solicitud para restablecer la contraseña de tu cuenta. 
                      Haz clic en el botón a continuación para crear una nueva contraseña.
                      Este enlace es válido por <strong>30 minutos</strong>.
                    </p>

                    <!-- CTA Button -->
                    <table width="100%" cellpadding="0" cellspacing="0">
                      <tr>
                        <td align="center" style="padding:8px 0 32px;">
                          <a href="{resetLink}"
                             style="display:inline-block;padding:14px 32px;background:#0f2e64;color:#ffffff;text-decoration:none;border-radius:10px;font-weight:700;font-size:15px;">
                            Restablecer contraseña
                          </a>
                        </td>
                      </tr>
                    </table>

                    <!-- Link alternativo -->
                    <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:16px;margin-bottom:24px;">
                      <p style="margin:0 0 6px;color:#64748b;font-size:12px;">Si el botón no funciona, copia este enlace:</p>
                      <p style="margin:0;word-break:break-all;font-size:12px;color:#0f2e64;">{resetLink}</p>
                    </div>

                    <!-- Aviso de seguridad -->
                    <div style="background:#fef3c7;border:1px solid #fde68a;border-radius:8px;padding:14px;">
                      <p style="margin:0;color:#92400e;font-size:13px;">
                        ⚠️ Si no solicitaste este cambio, ignora este correo. Tu contraseña no será modificada.
                      </p>
                    </div>
                  </td>
                </tr>

                <!-- Footer -->
                <tr>
                  <td style="background:#f8fafc;border-top:1px solid #e2e8f0;padding:20px 40px;text-align:center;">
                    <p style="margin:0;color:#94a3b8;font-size:12px;">
                      © {DateTime.Now.Year} IDEATEC S.A.C. – Facturación Electrónica Perú<br>
                      <a href="https://ideatec.pe" style="color:#0f2e64;">ideatec.pe</a>
                    </p>
                  </td>
                </tr>

              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

  public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, byte[]? adjunto = null, string? nombreAdjunto = null)
  {
    try
    {
      var smtpConfig = _config.GetSection("Smtp");

      var message = new MimeMessage();
      message.From.Add(new MailboxAddress(
          smtpConfig["FromName"] ?? "IDEATEC Factus",
          smtpConfig["FromEmail"] ?? "noreply@ideatec.pe"
      ));
      message.To.Add(new MailboxAddress(toEmail, toEmail));
      message.Subject = subject;

      var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

      // ← adjuntar PDF si viene
      if (adjunto != null && !string.IsNullOrWhiteSpace(nombreAdjunto))
      {
        bodyBuilder.Attachments.Add(nombreAdjunto, adjunto, ContentType.Parse("application/pdf"));
      }

      message.Body = bodyBuilder.ToMessageBody();

      using var client = new SmtpClient();
      await client.ConnectAsync(smtpConfig["Host"], int.Parse(smtpConfig["Port"] ?? "587"), SecureSocketOptions.StartTls);
      await client.AuthenticateAsync(smtpConfig["Username"], smtpConfig["Password"]);
      await client.SendAsync(message);
      await client.DisconnectAsync(true);

      _logger.LogInformation("Email enviado a {Email}", toEmail);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error al enviar email a {Email}", toEmail);
      throw;
    }
  }
}