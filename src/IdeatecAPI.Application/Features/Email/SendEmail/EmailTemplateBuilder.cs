namespace IdeatecAPI.Application.Features.Email.SendEmail;

public static class EmailTemplateBuilder
{
    // ── Tipo 0: Solo texto ─────────────────────────────────────────────
    public static string BuildTextEmail(string toName, string subject, string body) =>
        BuildBase(toName, subject, GetBadge(TipoComprobante.Texto),
            $"""
            <div style="background:#f8fafc;border-left:3px solid #0f2e64;border-radius:0 8px 8px 0;padding:16px 20px;margin-bottom:24px;">
              <p style="margin:0;color:#475569;font-size:14px;line-height:1.7;">{Escape(body)}</p>
            </div>
            """);

    // ── Tipo 1 y 3: Factura / Boleta ───────────────────────────────────
    public static string BuildComprobanteEmail(
        string toName, string subject, string body,
        TipoComprobante tipo, DatosComprobante datos)
    {
        var titulo = tipo == TipoComprobante.Factura ? "FACTURA ELECTRÓNICA" : "BOLETA ELECTRÓNICA";
        var filas  = string.Join("", datos.Items.Select(i => $"""
            <tr style="border-bottom:0.5px solid #f1f5f9;">
              <td style="padding:6px 0;color:#0f172a;">{Escape(i.Descripcion)}</td>
              <td style="padding:6px 0;color:#0f172a;text-align:center;">{i.Cantidad}</td>
              <td style="padding:6px 0;color:#0f172a;text-align:right;">S/ {(i.Cantidad * i.PrecioUnitario):F2}</td>
            </tr>
            """));

        var content = $"""
            <div style="background:#f8fafc;border-left:3px solid #0f2e64;border-radius:0 8px 8px 0;padding:14px 18px;margin-bottom:18px;">
              <p style="margin:0;color:#475569;font-size:14px;line-height:1.7;">{Escape(body)}</p>
            </div>
            <div style="border:0.5px solid #e2e8f0;border-radius:10px;overflow:hidden;margin-bottom:18px;">
              <div style="background:#0f2e64;padding:10px 16px;display:flex;justify-content:space-between;align-items:center;">
                <div>
                  <span style="color:#fff;font-size:12px;font-weight:700;">{titulo}</span>
                  <span style="color:#93c5fd;font-size:11px;margin-left:8px;">{Escape(datos.SerieNumero)}</span>
                </div>
                <span style="background:#16a34a;color:#fff;font-size:10px;font-weight:700;padding:2px 8px;border-radius:99px;margin-left:8px;">{Escape(datos.EstadoSunat)}</span>
              </div>
              <div style="padding:14px 16px;">
                <table style="width:100%;border-collapse:collapse;font-size:13px;">
                  <tr style="border-bottom:0.5px solid #f1f5f9;">
                    <td style="padding:5px 0;color:#64748b;">Descripción</td>
                    <td style="padding:5px 0;color:#64748b;text-align:center;width:50px;">Cant.</td>
                    <td style="padding:5px 0;color:#64748b;text-align:right;width:90px;">Importe</td>
                  </tr>
                  {filas}
                  <tr>
                    <td colspan="2" style="padding:8px 0 2px;color:#64748b;font-size:12px;">IGV (18%)</td>
                    <td style="padding:8px 0 2px;color:#64748b;text-align:right;">S/ {datos.Igv:F2}</td>
                  </tr>
                  <tr>
                    <td colspan="2" style="padding:2px 0 0;color:#0f2e64;font-weight:700;font-size:14px;">TOTAL</td>
                    <td style="padding:2px 0 0;color:#0f2e64;font-weight:700;text-align:right;font-size:14px;">S/ {datos.Total:F2}</td>
                  </tr>
                </table>
              </div>
              <div style="background:#f0fdf4;border-top:0.5px solid #bbf7d0;padding:8px 16px;display:flex;align-items:center;gap:6px;">
                <span style="color:#15803d;font-size:12px;">Comprobante aceptado por SUNAT</span>
              </div>
            </div>
            """;

        return BuildBase(toName, subject, GetBadge(tipo), content);
    }

    // ── Tipo 9: Guía de remisión ───────────────────────────────────────
    public static string BuildGuiaEmail(
        string toName, string subject, string body, DatosGuiaRemision datos)
    {
        var bienes = string.Join("", datos.Bienes.Select(b => $"""
            <tr style="border-bottom:0.5px solid #f1f5f9;">
              <td style="padding:5px 0;color:#0f172a;">{Escape(b.Descripcion)}</td>
              <td style="padding:5px 0;color:#0f172a;text-align:center;">{b.Cantidad}</td>
              <td style="padding:5px 0;color:#0f172a;text-align:right;">{Escape(b.Unidad)}</td>
            </tr>
            """));

        var content = $"""
            <div style="background:#f8fafc;border-left:3px solid #0f2e64;border-radius:0 8px 8px 0;padding:14px 18px;margin-bottom:18px;">
              <p style="margin:0;color:#475569;font-size:14px;line-height:1.7;">{Escape(body)}</p>
            </div>
            <div style="border:0.5px solid #e2e8f0;border-radius:10px;overflow:hidden;margin-bottom:18px;">
              <div style="background:#0f2e64;padding:10px 16px;display:flex;justify-content:space-between;align-items:center;">
                <div>
                  <span style="color:#fff;font-size:12px;font-weight:700;">GUÍA DE REMISIÓN</span>
                  <span style="color:#93c5fd;font-size:11px;margin-left:8px;">{Escape(datos.SerieNumero)}</span>
                </div>
                <span style="background:#16a34a;color:#fff;font-size:10px;font-weight:700;padding:2px 8px;border-radius:99px;margin-left:8px;">{Escape(datos.EstadoSunat)}</span>
              </div>
              <div style="padding:14px 16px;">
                <table style="width:100%;border-collapse:collapse;font-size:13px;margin-bottom:12px;">
                  <tr style="border-bottom:0.5px solid #f1f5f9;">
                    <td style="padding:5px 0;color:#64748b;width:45%;">Motivo de traslado</td>
                    <td style="padding:5px 0;color:#0f172a;">{Escape(datos.MotivoTraslado)}</td>
                  </tr>
                  <tr style="border-bottom:0.5px solid #f1f5f9;">
                    <td style="padding:5px 0;color:#64748b;">Fecha de traslado</td>
                    <td style="padding:5px 0;color:#0f172a;">{Escape(datos.FechaTraslado)}</td>
                  </tr>
                  <tr style="border-bottom:0.5px solid #f1f5f9;">
                    <td style="padding:5px 0;color:#64748b;">Dirección de partida</td>
                    <td style="padding:5px 0;color:#0f172a;">{Escape(datos.DireccionPartida)}</td>
                  </tr>
                  <tr>
                    <td style="padding:5px 0;color:#64748b;">Dirección de llegada</td>
                    <td style="padding:5px 0;color:#0f172a;">{Escape(datos.DireccionLlegada)}</td>
                  </tr>
                </table>
                <p style="margin:8px 0 6px;color:#64748b;font-size:11px;font-weight:700;letter-spacing:0.5px;">BIENES TRANSPORTADOS</p>
                <table style="width:100%;border-collapse:collapse;font-size:13px;">
                  <tr style="border-bottom:0.5px solid #f1f5f9;">
                    <td style="padding:5px 0;color:#64748b;">Descripción</td>
                    <td style="padding:5px 0;color:#64748b;text-align:center;width:50px;">Cant.</td>
                    <td style="padding:5px 0;color:#64748b;text-align:right;width:60px;">Unidad</td>
                  </tr>
                  {bienes}
                </table>
              </div>
              <div style="background:#f0fdf4;border-top:0.5px solid #bbf7d0;padding:8px 16px;display:flex;align-items:center;gap:6px;">
                <div style="width:6px;height:6px;border-radius:50%;background:#16a34a;flex-shrink:0;"></div>
                <span style="color:#15803d;font-size:12px;">Guía aceptada por SUNAT</span>
              </div>
            </div>
            """;

        return BuildBase(toName, subject, GetBadge(TipoComprobante.GuiaRemision), content);
    }

    // ── Helpers ────────────────────────────────────────────────────────
    private static (string Label, string Bg, string Color) GetBadge(TipoComprobante tipo) => tipo switch
    {
        TipoComprobante.Factura      => ("FACTURA",         "#dbeafe", "#1e40af"),
        TipoComprobante.Boleta       => ("BOLETA",          "#fce7f3", "#be185d"),
        TipoComprobante.GuiaRemision => ("GUÍA DE REMISIÓN","#fef9c3", "#854d0e"),
        _                            => ("MENSAJE",         "#e0e7ff", "#3730a3"),
    };

    private static string Escape(string s) =>
        System.Web.HttpUtility.HtmlEncode(s).Replace("\n", "<br/>");

    // ── Base compartida ────────────────────────────────────────────────
    private static string BuildBase(
        string toName, string subject,
        (string Label, string Bg, string Color) badge,
        string content) => $"""
        <!DOCTYPE html>
        <html lang="es">
        <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width"></head>
        <body style="margin:0;padding:0;background:#f1f5f9;font-family:Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f1f5f9;padding:40px 0;">
            <tr><td align="center">
              <table width="560" cellpadding="0" cellspacing="0"
                     style="background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);">

                <tr>
                  <td style="background:#0f2e64;padding:28px 36px;text-align:center;">
                    <h1 style="margin:0;color:#fff;font-size:22px;font-weight:800;letter-spacing:-0.5px;">
                      IDEA<span style="color:#ef4444;">TEC</span>
                    </h1>
                    <span style="color:#93c5fd;font-size:12px;">Facturación Electrónica</span>
                  </td>
                </tr>

                <tr>
                  <td style="background:#f8fafc;border-bottom:1px solid #e2e8f0;padding:10px 36px;">
                    <span style="background:{badge.Bg};color:{badge.Color};font-size:11px;font-weight:700;
                                 padding:3px 10px;border-radius:99px;">{badge.Label}</span>
                  </td>
                </tr>

                <tr>
                  <td style="padding:32px 36px;">
                    <h2 style="margin:0 0 6px;color:#0f172a;font-size:18px;">{Escape(subject)}</h2>
                    <p style="margin:0 0 20px;color:#64748b;font-size:13px;">
                      Estimado/a <strong style="color:#0f2e64;">{Escape(toName)}</strong>
                    </p>
                    {content}
                    <div style="background:#fef3c7;border:0.5px solid #fde68a;border-radius:8px;padding:12px 14px;margin-top:20px;">
                      <p style="margin:0;color:#92400e;font-size:12px;">
                        Para consultas escríbenos a <strong>soporte@ideatec.pe</strong>
                      </p>
                    </div>
                  </td>
                </tr>

                <tr>
                  <td style="background:#f8fafc;border-top:1px solid #e2e8f0;padding:16px 36px;text-align:center;">
                    <p style="margin:0;color:#94a3b8;font-size:11px;">
                      © {DateTime.Now.Year} IDEATEC S.A.C. – Facturación Electrónica Perú<br>
                      <a href="https://ideatec.pe" style="color:#0f2e64;text-decoration:none;">ideatec.pe</a>
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