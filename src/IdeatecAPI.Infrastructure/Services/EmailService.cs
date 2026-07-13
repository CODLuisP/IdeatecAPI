using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IdeatecAPI.Application.Common.Interfaces.Persistence;

namespace IdeatecAPI.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _http;

    private const string MailerSendApiUrl = "https://api.mailersend.com/v1/email";

    public EmailService(IConfiguration config, ILogger<EmailService> logger, IHttpClientFactory httpFactory)
    {
        _config = config;
        _logger = logger;
        _http = httpFactory.CreateClient("mailersend");
    }

    // ── Método privado central: envío via API HTTP ─────────────────────────
    private async Task SendViaApiAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        string? textBody = null,
        IEnumerable<(byte[] Bytes, string Nombre)>? adjuntos = null)
    {
        var smtpConfig = _config.GetSection("Smtp");
        var apiKey = smtpConfig["ApiKey"] ?? throw new InvalidOperationException("MailerSend ApiKey no configurado.");

        // Soporta múltiples destinatarios separados por coma en un solo campo
        var destinatarios = toEmail
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (destinatarios.Length == 0)
            throw new InvalidOperationException("No se proporcionó ningún destinatario válido.");

        // Construir payload
        var payload = new Dictionary<string, object>
        {
            ["from"] = new { email = smtpConfig["FromEmail"], name = smtpConfig["FromName"] },
            ["to"]   = destinatarios.Select(email => new { email, name = toName }).ToArray(),
            ["subject"] = subject,
            ["html"] = htmlBody,
        };

        if (!string.IsNullOrWhiteSpace(textBody))
            payload["text"] = textBody;

        // Adjuntos en base64
        var lista = adjuntos?.ToList();
        if (lista is { Count: > 0 })
        {
            payload["attachments"] = lista.Select(a => new
            {
                content     = Convert.ToBase64String(a.Bytes),
                filename    = a.Nombre,
                disposition = "attachment"
            }).ToArray();
        }

        var json    = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, MailerSendApiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"MailerSend API error {(int)response.StatusCode}: {body}");
        }
    }

    // ── SendPasswordResetEmailAsync ────────────────────────────────────────
    public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink)
    {
        try
        {
            await SendViaApiAsync(
                toEmail:  toEmail,
                toName:   toName,
                subject:  "Recuperación de contraseña – IDEATEC Factus",
                htmlBody: BuildEmailHtml(toName, resetLink),
                textBody: $"Hola {toName},\n\nHaz clic aquí para restablecer tu contraseña (válido 30 min):\n{resetLink}\n\nSi no lo solicitaste, ignora este correo.\n\n— Equipo IDEATEC"
            );

            _logger.LogInformation("Email de reset enviado a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email de reset a {Email}", toEmail);
            throw;
        }
    }

    // ── SendEmailAsync ─────────────────────────────────────────────────────
    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlBody,
        IEnumerable<(byte[] Bytes, string Nombre)>? adjuntos = null)
    {
        try
        {
            await SendViaApiAsync(
                toEmail:  toEmail,
                toName:   toEmail,
                subject:  subject,
                htmlBody: htmlBody,
                adjuntos: adjuntos
            );

            _logger.LogInformation("Email enviado a {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email a {Email}", toEmail);
            throw;
        }
    }

    // ── Template HTML (sin cambios) ────────────────────────────────────────
    private static string BuildEmailHtml(string nombre, string resetLink) => $"""
        <!DOCTYPE html>
        <html lang="es">
        <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width"></head>
        <body style="margin:0;padding:0;background:#f1f5f9;font-family:Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f1f5f9;padding:40px 0;">
            <tr><td align="center">
              <table width="560" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                
                <tr>
                  <td style="background:#0f2e64;padding:32px 40px;text-align:center;">
                    <h1 style="margin:0;color:#ffffff;font-size:24px;font-weight:800;letter-spacing:-0.5px;">
                      IDEA<span style="color:#ef4444;">TEC</span>
                      <span style="font-weight:400;font-size:14px;color:#93c5fd;display:block;margin-top:4px;">Facturación Electrónica</span>
                    </h1>
                  </td>
                </tr>

                <tr>
                  <td style="padding:40px;">
                    <h2 style="margin:0 0 8px;color:#0f172a;font-size:20px;">Recuperación de contraseña</h2>
                    <p style="margin:0 0 24px;color:#64748b;font-size:15px;">Hola, <strong>{nombre}</strong></p>
                    <p style="margin:0 0 24px;color:#475569;font-size:14px;line-height:1.6;">
                      Recibimos una solicitud para restablecer la contraseña de tu cuenta. 
                      Haz clic en el botón a continuación para crear una nueva contraseña.
                      Este enlace es válido por <strong>30 minutos</strong>.
                    </p>

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

                    <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:16px;margin-bottom:24px;">
                      <p style="margin:0 0 6px;color:#64748b;font-size:12px;">Si el botón no funciona, copia este enlace:</p>
                      <p style="margin:0;word-break:break-all;font-size:12px;color:#0f2e64;">{resetLink}</p>
                    </div>

                    <div style="background:#fef3c7;border:1px solid #fde68a;border-radius:8px;padding:14px;">
                      <p style="margin:0;color:#92400e;font-size:13px;">
                        Si no solicitaste este cambio, ignora este correo. Tu contraseña no será modificada.
                      </p>
                    </div>
                  </td>
                </tr>

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
}