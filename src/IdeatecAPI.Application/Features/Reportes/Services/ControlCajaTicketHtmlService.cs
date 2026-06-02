using System.Text;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Common.Interfaces.Persistence.Reportes;
using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Application.Features.Reportes.Services;

public class ControlCajaTicketHtmlService : IControlCajaTicketHtmlService
{
    private readonly IUnitOfWork _unitOfWork;

    public ControlCajaTicketHtmlService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerarHtmlAsync(
        string titulo,
        IEnumerable<ControlCajaTicketItemDto> datos,
        string ruc,
        string? codEstablecimiento,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        string nombreResponsable)
    {
        var empresa = await _unitOfWork.Empresas.GetEmpresaByRucAsync(ruc);

        var lista = datos.ToList();

        // Separar movimientos y ajustes de otros períodos
        var idsDelPeriodo = lista.Select(x => x.ComprobanteId).ToHashSet();
        bool EsNota(ControlCajaTicketItemDto x)
            => x.TipoComprobante == "07" || x.TipoComprobante == "08";
        bool AfectaOtro(ControlCajaTicketItemDto x)
            => EsNota(x) && x.ComprobanteAfectadoId.HasValue
               && !idsDelPeriodo.Contains(x.ComprobanteAfectadoId.Value);

        var movimientos = lista.Where(x => !AfectaOtro(x)).ToList();
        var ajustes     = lista.Where(AfectaOtro).ToList();

        // Totales por medio de pago (solo movimientos)
        var resumenPago = movimientos
            .SelectMany(c => c.Pagos.Select(p => new
            {
                Medio  = NormalizarMedio(p.MedioPago),
                Moneda = c.TipoMoneda,
                p.Monto
            }))
            .GroupBy(x => (x.Medio, x.Moneda))
            .Select(g => (Medio: g.Key.Medio, Moneda: g.Key.Moneda, Total: g.Sum(x => x.Monto)))
            .Where(x => x.Total > 0)
            .OrderBy(x => x.Moneda).ThenBy(x => x.Medio)
            .ToList();

        var totalPen = movimientos
            .Where(x => x.TipoMoneda == "PEN")
            .Sum(x => x.TipoComprobante == "07" ? -x.ImporteTotal : x.ImporteTotal);
        var totalUsd = movimientos
            .Where(x => x.TipoMoneda == "USD")
            .Sum(x => x.TipoComprobante == "07" ? -x.ImporteTotal : x.ImporteTotal);

        var fechaReporte = DateTime.Now;

        // ── Logo ────────────────────────────────────────────────────────────
        string logoHtml = "";
        if (!string.IsNullOrEmpty(empresa?.LogoBase64))
        {
            var src = empresa.LogoBase64.StartsWith("data:")
                ? empresa.LogoBase64
                : $"data:image/png;base64,{empresa.LogoBase64}";
            logoHtml = $"<div class=\"center\"><img src=\"{src}\" style=\"max-width:55mm;max-height:20mm;object-fit:contain;\"></div>";
        }

        // ── Rango de fechas ──────────────────────────────────────────────────
        string rangoHtml = "";
        if (fechaDesde.HasValue)
        {
            var rango = fechaHasta.HasValue && fechaHasta.Value.Date != fechaDesde.Value.Date
                ? $"{fechaDesde:dd/MM/yyyy} - {fechaHasta:dd/MM/yyyy}"
                : fechaDesde.Value.ToString("dd/MM/yyyy");
            rangoHtml = $"<tr><td class=\"lbl\">Período:</td><td>{HE(rango)}</td></tr>";
        }

        string responsableHtml = !string.IsNullOrWhiteSpace(nombreResponsable)
            ? $"<tr><td class=\"lbl\">Responsable:</td><td>{HE(nombreResponsable)}</td></tr>"
            : "";

        // ── Tabla detalle movimientos ────────────────────────────────────────
        var detalle = BuildTablaDetalle(movimientos, "#1A2B4A");

        // ── Totales ──────────────────────────────────────────────────────────
        var totalesHtml = new StringBuilder();
        if (totalPen != 0)
            totalesHtml.Append($"<tr class=\"total-row\"><td>TOTAL (PEN)</td><td class=\"tr\">S/ {totalPen:N2}</td></tr>");
        if (totalUsd != 0)
            totalesHtml.Append($"<tr class=\"total-row\"><td>TOTAL (USD)</td><td class=\"tr\">$ {totalUsd:N2}</td></tr>");

        // ── Resumen por medio de pago ────────────────────────────────────────
        var resumenHtml = new StringBuilder();
        foreach (var r in resumenPago)
        {
            var simbolo = r.Moneda == "USD" ? "$" : "S/";
            resumenHtml.Append($"<tr><td>{HE(r.Medio)} ({HE(r.Moneda)})</td><td class=\"tr\">{simbolo} {r.Total:N2}</td></tr>");
        }

        // ── Ajustes de otros períodos ────────────────────────────────────────
        string ajustesHtml = "";
        if (ajustes.Any())
        {
            var tablaAjustes = BuildTablaDetalle(ajustes, "#7030A0");
            ajustesHtml = $@"
<hr style=""border-color:#7030A0;"">
<p class=""sec-title"" style=""color:#7030A0;"">AJUSTES DE OTROS PERÍODOS</p>
<p class=""small"" style=""color:#7030A0;font-style:italic;"">(no afectan el total anterior)</p>
{tablaAjustes}";
        }

        return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""utf-8"">
<title>{HE(titulo)}</title>
<style>
  @page {{
    size: 80mm auto;
    margin: 3mm 3mm 6mm 3mm;
  }}
  * {{ box-sizing: border-box; margin: 0; padding: 0; -webkit-print-color-adjust: exact; print-color-adjust: exact; }}
  body {{
    font-family: Arial, 'Helvetica Neue', sans-serif;
    font-size: 9px;
    width: 74mm;
    color: #1A1A1A;
    background: #fff;
  }}
  .center {{ text-align: center; }}
  .bold   {{ font-weight: bold; }}
  .small  {{ font-size: 8px; }}
  .tr     {{ text-align: right; }}
  hr      {{ border: none; border-top: 1px solid #1A2B4A; margin: 3px 0; }}

  .empresa-nombre {{ font-size: 11px; font-weight: bold; text-align: center; color: #1A2B4A; }}
  .empresa-sub    {{ font-size: 8px; text-align: center; color: #1A2B4A; }}

  .titulo {{ font-size: 10px; font-weight: bold; text-align: center; color: #1A2B4A; margin: 3px 0; }}

  table.cabecera {{ width: 100%; border-collapse: collapse; margin: 2px 0; font-size: 8px; }}
  table.cabecera td {{ padding: 1px 0; vertical-align: top; }}
  table.cabecera td.lbl {{ font-weight: bold; color: #1A2B4A; width: 38%; white-space: nowrap; }}

  table.detalle {{ width: 100%; border-collapse: collapse; margin: 2px 0; font-size: 8px; }}
  table.detalle th {{ background: #1A2B4A; color: #fff; font-weight: bold; font-size: 7px; padding: 2px; text-align: left; }}
  table.detalle th.tr {{ text-align: right; }}
  table.detalle td {{ padding: 2px; border-bottom: 0.3px solid #D0D7E3; vertical-align: top; }}
  table.detalle td.tr {{ text-align: right; }}
  table.detalle td.red {{ color: #C00000; }}

  table.totales {{ width: 100%; border-collapse: collapse; margin: 2px 0; font-size: 8px; }}
  table.totales td {{ padding: 1px 0; }}
  table.totales td.tr {{ text-align: right; }}
  table.totales tr.total-row td {{ font-weight: bold; }}

  .sec-title {{ font-weight: bold; font-size: 9px; color: #1A2B4A; margin: 3px 0 1px; }}

  .footer {{ font-size: 7px; text-align: center; color: #4A5568; font-style: italic; margin-top: 4px; }}
</style>
</head>
<body>

{logoHtml}

<p class=""empresa-nombre"">{HE(empresa?.NombreComercial ?? empresa?.RazonSocial ?? ruc)}</p>
<p class=""empresa-sub"">RUC: {HE(ruc)}</p>
{(!string.IsNullOrEmpty(empresa?.Direccion) ? $"<p class=\"empresa-sub\">{HE(empresa.Direccion)}</p>" : "")}

<hr>

<p class=""titulo"">{HE(titulo.ToUpper())}</p>

<table class=""cabecera"">
  <tr><td class=""lbl"">Fecha:</td><td>{fechaReporte:dd/MM/yyyy HH:mm:ss}</td></tr>
  {rangoHtml}
  {responsableHtml}
</table>

<hr>

<p class=""sec-title"">DETALLE</p>
{detalle}

<hr>

<table class=""totales"">
  {totalesHtml}
</table>

<hr>

<p class=""sec-title"">RESUMEN</p>
<table class=""totales"">
  {resumenHtml}
</table>

{ajustesHtml}

<p class=""footer"">Generado: {fechaReporte:dd/MM/yyyy HH:mm:ss}</p>

</body>
</html>";
    }

    // ── Tabla detalle reutilizable ────────────────────────────────────────────
    private static string BuildTablaDetalle(List<ControlCajaTicketItemDto> items, string headerColor)
    {
        var sb = new StringBuilder();
        sb.Append($"<table class=\"detalle\" style=\"--hc:{headerColor};\">");
        sb.Append($"<thead><tr style=\"background:{headerColor};\">");
        sb.Append("<th>#</th><th>Serie</th><th>Num.</th><th class=\"tr\">Monto</th><th>Mon</th><th>T.Pago</th>");
        sb.Append("</tr></thead><tbody>");

        int n = 1;
        foreach (var d in items)
        {
            bool esNC   = d.TipoComprobante == "07";
            decimal monto = esNC ? -d.ImporteTotal : d.ImporteTotal;
            string medios = d.Pagos.Any()
                ? string.Join("/", d.Pagos.Select(p => AbreviarMedio(p.MedioPago)))
                : "-";
            string redClass = esNC ? " red" : "";

            sb.Append("<tr>");
            sb.Append($"<td>{n++}</td>");
            sb.Append($"<td>{HE(d.Serie)}</td>");
            sb.Append($"<td>{d.Correlativo ?? 0}</td>");
            sb.Append($"<td class=\"tr{redClass}\">{FmtTicket(monto, d.TipoMoneda)}</td>");
            sb.Append($"<td>{HE(d.TipoMoneda == "USD" ? "USD" : "PEN")}</td>");
            sb.Append($"<td>{HE(medios)}</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string FmtTicket(decimal v, string? moneda)
        => moneda == "USD" ? $"$ {v:N2}" : $"S/ {v:N2}";

    private static string NormalizarMedio(string? medio)
    {
        if (string.IsNullOrWhiteSpace(medio)) return "Efectivo";
        return medio.Trim().ToLower() switch
        {
            "efectivo"                                                          => "Efectivo",
            "yape"                                                              => "Yape",
            "plin"                                                              => "Plin",
            "transferencia" or "transferencia bancaria"                         => "Transferencia",
            "tarjeta" or "tarjeta de crédito" or "tarjeta de débito"
                or "tarjeta de debito"                                          => "Tarjeta",
            "depósito" or "deposito" or "depósito bancario"
                or "deposito bancario"                                          => "Depósito",
            "pos"                                                               => "POS",
            "cheque"                                                            => "Cheque",
            var s => char.ToUpper(s[0]) + s[1..]
        };
    }

    private static string AbreviarMedio(string? medio)
    {
        if (string.IsNullOrWhiteSpace(medio)) return "Efec.";
        return medio.Trim().ToLower() switch
        {
            "efectivo"                                                          => "Efec.",
            "yape"                                                              => "Yape",
            "plin"                                                              => "Plin",
            "transferencia" or "transferencia bancaria"                         => "Trans.",
            "tarjeta" or "tarjeta de crédito" or "tarjeta de débito"
                or "tarjeta de debito"                                          => "Tarj.",
            "depósito" or "deposito" or "depósito bancario"
                or "deposito bancario"                                          => "Dep.",
            "pos"                                                               => "POS",
            "cheque"                                                            => "Cheq.",
            var s => s.Length > 5 ? s[..5] + "." : s
        };
    }

    private static string HE(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
