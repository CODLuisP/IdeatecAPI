using System.Text;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using QRCoder;

namespace IdeatecAPI.Application.Features.Comprobante.Services;

public class ComprobanteHtmlService : IComprobanteHtmlService
{
    private readonly IUnitOfWork _unitOfWork;

    public ComprobanteHtmlService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerarHtmlTicketAsync(int comprobanteId, TamanoPdf tamano)
    {
        var comprobante = await _unitOfWork.Comprobantes.GetByIdAsync(comprobanteId)
            ?? throw new KeyNotFoundException($"Comprobante {comprobanteId} no encontrado.");

        var empresa = await _unitOfWork.Empresas.GetEmpresaByRucAsync(comprobante.EmpresaRuc ?? "")
            ?? throw new KeyNotFoundException($"Empresa con RUC '{comprobante.EmpresaRuc}' no encontrada.");

        var datos  = await _unitOfWork.Comprobantes.GetDatosCompletosByComprobanteIdAsync(comprobanteId);
        var vales  = (await _unitOfWork.Comprobantes.GetValesFullByComprobanteIdAsync(comprobanteId)).ToList();

        var detalles     = datos.Detalles.ToList();
        var pagos        = datos.Pagos.ToList();
        var cuotas       = datos.Cuotas.ToList();
        var leyendas     = datos.Leyendas.ToList();
        var detracciones = datos.Detracciones.ToList();

        bool es58 = tamano == TamanoPdf.Ticket58mm;
        string anchoMm  = es58 ? "54mm" : "76mm";   // papel - márgenes
        string paginaMm = es58 ? "58mm" : "80mm";
        string margenMm = es58 ? "1mm"  : "2mm";
        string moneda   = comprobante.TipoMoneda ?? "PEN";

        // ── QR ──────────────────────────────────────────────────────────────
        string qrBase64 = GenerarQrBase64(comprobante);

        // ── Logo ─────────────────────────────────────────────────────────────
        string logoHtml = "";
        if (!string.IsNullOrEmpty(empresa.LogoBase64))
        {
            var src = empresa.LogoBase64.StartsWith("data:")
                ? empresa.LogoBase64
                : $"data:image/png;base64,{empresa.LogoBase64}";
            logoHtml = $"<div class=\"center\"><img src=\"{src}\" style=\"max-width:55mm;max-height:20mm;object-fit:contain;\"></div>";
        }

        // ── Tipo comprobante ─────────────────────────────────────────────────
        string tipoNombre = comprobante.TipoComprobante switch
        {
            "01" => "FACTURA ELECTRÓNICA",
            "03" => "BOLETA ELECTRÓNICA",
            "07" => "NOTA DE CRÉDITO",
            "08" => "NOTA DE DÉBITO",
            _    => "COMPROBANTE ELECTRÓNICO"
        };

        // ── Tabla de ítems ───────────────────────────────────────────────────
        bool mostrarCodigo = detalles.Any(d => !string.IsNullOrWhiteSpace(d.Codigo));
        var  items         = new StringBuilder();

        items.Append("<table class=\"items\">");
        items.Append("<thead><tr>");
        items.Append("<th>#</th>");
        if (mostrarCodigo) items.Append("<th>Cód</th>");
        items.Append("<th>Cant</th><th class=\"tdesc\">Descripción</th><th>P.Vent</th><th>Total</th>");
        items.Append("</tr></thead><tbody>");

        int idx = 1;
        foreach (var d in detalles)
        {
            bool esGratuito = d.TipoAfectacionIGV is "11" or "21" or "31";
            decimal pVent   = (d.DescuentoTotal ?? 0) > 0
                ? Math.Round((d.PrecioVenta ?? 0) + (d.DescuentoUnitario ?? 0), 2)
                : (d.PrecioVenta ?? 0);
            string descTxt  = HE(d.Descripcion ?? "-") + (esGratuito ? " <em>(GR)</em>" : "");
            string totalTxt = esGratuito ? "0.00" : (d.TotalVentaItem ?? 0).ToString("F2");

            items.Append("<tr>");
            items.Append($"<td>{idx++}</td>");
            if (mostrarCodigo) items.Append($"<td>{HE(d.Codigo ?? "-")}</td>");
            items.Append($"<td>{d.Cantidad:F2}</td>");
            items.Append($"<td class=\"tdesc\">{descTxt}</td>");
            items.Append($"<td class=\"tr\">{pVent:F2}</td>");
            items.Append($"<td class=\"tr\">{totalTxt}</td>");
            items.Append("</tr>");

            if ((d.DescuentoUnitario ?? 0) > 0)
            {
                int colspan = mostrarCodigo ? 6 : 5;
                items.Append($"<tr><td colspan=\"{colspan}\" class=\"tr\" style=\"font-size:8px;color:#555;\">Dscto: -{d.DescuentoUnitario:F2}</td></tr>");
            }
        }
        items.Append("</tbody></table>");

        // ── Totales ──────────────────────────────────────────────────────────
        var totales = new StringBuilder();
        totales.Append("<table class=\"totales\">");
        FilaTot(totales, "Op. Gravadas",   Fmt(comprobante.TotalOperacionesGravadas ?? 0, moneda));
        FilaTot(totales, "Op. Exoneradas", Fmt(comprobante.TotalOperacionesExoneradas ?? 0, moneda));
        FilaTot(totales, "Op. Inafectas",  Fmt(comprobante.TotalOperacionesInafectas ?? 0, moneda));
        decimal porIgv = detalles.FirstOrDefault(d => (d.PorcentajeIGV ?? 0) > 0)?.PorcentajeIGV ?? 18;
        FilaTot(totales, $"I.G.V. ({porIgv:G29}%)", Fmt(comprobante.TotalIGV ?? 0, moneda));
        if ((comprobante.TotalIcbper ?? 0) > 0)
            FilaTot(totales, "ICBPER", Fmt(comprobante.TotalIcbper ?? 0, moneda));
        if ((comprobante.TotalDescuentos ?? 0) > 0)
            FilaTot(totales, "Descuentos", $"-{Fmt(comprobante.TotalDescuentos ?? 0, moneda)}");
        if ((comprobante.DescuentoGlobal ?? 0) > 0)
            FilaTot(totales, "Dscto. Global", $"-{Fmt(comprobante.DescuentoGlobal ?? 0, moneda)}");
        totales.Append($"<tr class=\"total-final\"><td>IMPORTE TOTAL</td><td>{Fmt(comprobante.ImporteTotal ?? 0, moneda)}</td></tr>");
        totales.Append("</table>");

        // ── Leyendas ─────────────────────────────────────────────────────────
        var leyendasHtml = new StringBuilder();
        foreach (var l in leyendas)
            leyendasHtml.Append($"<p class=\"leyenda\">{HE(l.Value?.ToUpper() ?? "")}</p>");

        // ── Forma de pago ────────────────────────────────────────────────────
        var pagosHtml  = BuildPagosHtml(comprobante, pagos, cuotas, moneda);

        // ── Detracción ───────────────────────────────────────────────────────
        var detracHtml = BuildDetraccionHtml(detracciones, moneda, comprobante.TipoComprobante);

        // ── Vales ────────────────────────────────────────────────────────────
        var valesHtml = BuildValesHtml(comprobante, vales);

        // ── Doc. que modifica ─────────────────────────────────────────────────
        string docModifica = "";
        if (comprobante.TipoComprobante is "07" or "08"
            && !string.IsNullOrEmpty(comprobante.TipDocAfectado)
            && !string.IsNullOrEmpty(comprobante.NumDocAfectado))
        {
            var den = comprobante.TipDocAfectado switch { "01" => "FACTURA", "03" => "BOLETA", _ => "COMPROBANTE" };
            docModifica = $"<p class=\"small\">Doc. modifica: {den} {HE(comprobante.NumDocAfectado)}</p>";
        }

        // ── Cliente ──────────────────────────────────────────────────────────
        string labelDoc = comprobante.TipoComprobante == "01" ? "RUC"
            : comprobante.ClienteTipoDoc switch
            {
                "01" => "DNI", "6" => "RUC", "7" => "Pasaporte",
                "4"  => "Carnet Ext.", _ => "Doc."
            };

        string fechaVcto = (comprobante.TipoPago?.ToLower() ?? "") is "credito" or "crédito"
            ? $"<tr><td class=\"lbl\">Fcto. Vcto.:</td><td>{comprobante.FechaVencimiento:dd/MM/yyyy}</td></tr>"
            : "";

        string monedaRow = comprobante.TipoMoneda != "PEN" && comprobante.TipoCambio.HasValue
            ? $"<tr><td class=\"lbl\">Moneda:</td><td>{HE(comprobante.TipoMoneda ?? "")} T.C. S/{comprobante.TipoCambio:F3}</td></tr>"
            : "";

        bool mostrarPersonal = comprobante.EmpresaRuc != "20512134832";

        string cajeroRow = mostrarPersonal && !string.IsNullOrWhiteSpace(comprobante.NombreCajero)
            ? $"<tr><td class=\"lbl\">Cajero:</td><td>{HE(comprobante.NombreCajero!)}</td></tr>"
            : "";

        var nombreTrabajador = detalles
            .Select(d => d.NombreTrabajador?.Trim())
            .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
        string atendidoRow = mostrarPersonal && !string.IsNullOrWhiteSpace(nombreTrabajador)
            ? $"<tr><td class=\"lbl\">Atendido por:</td><td>{HE(nombreTrabajador!)}</td></tr>"
            : "";

        // ── HTML final ───────────────────────────────────────────────────────
        return $@"<!DOCTYPE html>
<html lang=""es"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<title>{HE(tipoNombre)} {HE(comprobante.NumeroCompleto ?? "")}</title>
<style>
  @page {{
    size: {paginaMm} auto;
    margin: {margenMm};
  }}
  * {{ box-sizing: border-box; margin: 0; padding: 0; -webkit-print-color-adjust: exact; print-color-adjust: exact; }}
  body {{
    font-family: Arial, 'Helvetica Neue', sans-serif;
    font-size: 10px;
    width: {anchoMm};
    color: #000;
    background: #fff;
  }}
  .center  {{ text-align: center; }}
  .bold    {{ font-weight: bold; }}
  .small   {{ font-size: 9px; color: #000; }}
  hr       {{ border: none; border-top: 1px solid #000; margin: 3px 0; }}
  .empresa-nombre {{ font-size: 11px; font-weight: bold; text-align: center; }}
  .empresa-sub    {{ font-size: 9px; text-align: center; color: #000; }}

  .badge {{
    font-weight: bold;
    font-size: 10px;
    text-align: center;
    padding: 3px 2px;
    margin: 3px 0;
    border-top: 1px solid #000;
    border-bottom: 1px solid #000;
  }}
  .numero-doc {{ font-size: 10px; font-weight: bold; text-align: center; margin-bottom: 3px; }}

  table.cliente {{ width: 100%; border-collapse: collapse; margin: 3px 0; }}
  table.cliente td {{ font-size: 9px; padding: 1px 0; vertical-align: top; color: #000; }}
  table.cliente td.lbl {{ font-weight: bold; width: 38%; color: #000; white-space: nowrap; }}

  table.items {{ width: 100%; border-collapse: collapse; margin: 3px 0; font-size: 9px; }}
  table.items th {{ background: none; color: #000; padding: 2px; text-align: left; font-weight: bold; border-bottom: 1px solid #000; }}
  table.items td {{ padding: 2px; border-bottom: 1px solid #ccc; vertical-align: top; color: #000; }}
  table.items .tdesc {{ }}
  table.items .tr {{ text-align: right; }}

  table.totales {{ width: 100%; border-collapse: collapse; margin: 3px 0; font-size: 9px; }}
  table.totales tr td {{ padding: 2px 1px; color: #000; }}
  table.totales tr td:last-child {{ text-align: right; font-weight: bold; }}
  table.totales tr.total-final {{ background: none; color: #000; font-weight: bold; font-size: 10px; border-top: 1px solid #000; }}
  table.totales tr.total-final td {{ padding: 3px 2px; }}

  .leyenda {{ font-weight: bold; font-size: 9px; color: #000; margin: 2px 0; }}

  table.pagos {{ width: 100%; border-collapse: collapse; font-size: 9px; }}
  table.pagos td {{ padding: 1px 0; color: #000; }}
  table.pagos td:last-child {{ text-align: right; }}

  .qr {{ text-align: center; margin: 6px 0 3px; }}
  .qr img {{ width: 55mm; height: 55mm; }}

  .footer {{ font-size: 9px; text-align: center; color: #000; margin-top: 3px; }}

  .motivo {{ font-size: 9px; margin: 2px 0; color: #000; }}
  .sec-title {{ font-weight: bold; color: #000; font-size: 9px; margin: 4px 0 1px; }}

  /* Ocultar en pantalla lo que solo es para impresión no es necesario aquí,
     el frontend abre esta URL en ventana nueva y llama window.print() */
</style>
</head>
<body>

{logoHtml}

<p class=""empresa-nombre"">{HE(empresa.NombreComercial ?? empresa.RazonSocial ?? "")}</p>
<p class=""empresa-sub"">{HE(empresa.RazonSocial ?? "")}</p>
{(!string.IsNullOrEmpty(empresa.Direccion) ? $"<p class=\"empresa-sub\">{HE(empresa.Direccion)}</p>" : "")}
{(!string.IsNullOrEmpty(empresa.Telefono)  ? $"<p class=\"empresa-sub\">Tel: {HE(empresa.Telefono)}</p>" : "")}
{(!string.IsNullOrEmpty(empresa.Email)     ? $"<p class=\"empresa-sub\">{HE(empresa.Email)}</p>" : "")}

<hr>

<p class=""center bold"" style=""font-size:10px;"">RUC: {HE(empresa.Ruc ?? "")}</p>
<div class=""badge"">{HE(tipoNombre)}</div>
<p class=""numero-doc"">N° {HE(comprobante.Serie ?? "")}-{comprobante.Correlativo:D8}</p>

<hr>

<table class=""cliente"">
  <tr><td class=""lbl"">Cliente:</td><td>{HE(comprobante.ClienteRazonSocial ?? "-")}</td></tr>
  <tr><td class=""lbl"">{HE(labelDoc)}:</td><td>{HE(comprobante.ClienteNumDoc ?? "-")}</td></tr>
  {(!string.IsNullOrEmpty(comprobante.ClienteDireccion) ? $"<tr><td class=\"lbl\">Dir.:</td><td>{HE(comprobante.ClienteDireccion)}</td></tr>" : "")}
  <tr><td class=""lbl"">Fecha:</td><td>{comprobante.FechaEmision:dd/MM/yyyy} {comprobante.HoraEmision:HH:mm:ss}</td></tr>
  {fechaVcto}
  {monedaRow}
  {cajeroRow}
  {atendidoRow}
</table>

{docModifica}

<hr>

{items}

<hr>

{totales}

{(leyendas.Any() ? "<hr>" + leyendasHtml : "")}

{(comprobante.TipoComprobante is "07" or "08" && !string.IsNullOrEmpty(comprobante.MotivoNota)
    ? $"<p class=\"motivo\"><span class=\"bold\" style=\"color:#1A2B4A;\">MOTIVO: </span>{HE(comprobante.MotivoNota)}</p>"
    : "")}

{(detracciones.Any() ? "<hr>" + detracHtml : "")}

{pagosHtml}

<hr>

<div class=""qr"">
  <img src=""data:image/png;base64,{qrBase64}"" alt=""QR"">
</div>

<p class=""footer"">Representación impresa de {HE(tipoNombre)}<br>Consulte en www.sunat.gob.pe</p>

{valesHtml}

</body>
</html>";
    }

    // ── Sección pagos ────────────────────────────────────────────────────────
    private static string BuildPagosHtml(
        Domain.Entities.Comprobante c,
        List<Domain.Entities.Pago> pagos,
        List<Domain.Entities.Cuota> cuotas,
        string moneda)
    {
        bool esCredito     = (c.TipoPago?.ToLower() ?? "") is "credito" or "crédito";
        bool tieneInicial  = pagos.Any() && cuotas.Any();
        var  sb            = new StringBuilder();

        sb.Append("<p class=\"sec-title\">FORMA DE PAGO</p>");
        sb.Append("<table class=\"pagos\">");

        if (!esCredito)
        {
            sb.Append($"<tr><td>Tipo</td><td>Contado</td></tr>");
            if (pagos.Any())
                foreach (var p in pagos)
                    sb.Append($"<tr><td>{HE(p.MedioPago ?? "Efectivo")}</td><td>{Fmt(p.Monto ?? 0, moneda)}</td></tr>");
            else
                sb.Append($"<tr><td>Efectivo</td><td>{Fmt(c.ImporteTotal ?? 0, moneda)}</td></tr>");
        }
        else if (tieneInicial)
        {
            sb.Append($"<tr><td>Tipo</td><td>Crédito c/ inicial</td></tr>");
            foreach (var p in pagos)
                sb.Append($"<tr><td>Inicial ({HE(p.MedioPago ?? "Efectivo")})</td><td>{Fmt(p.Monto ?? 0, moneda)}</td></tr>");
            sb.Append($"<tr><td colspan=\"2\" class=\"bold\" style=\"padding-top:2px;\">Cuotas:</td></tr>");
            foreach (var cu in cuotas)
                sb.Append($"<tr><td>{HE(cu.NumeroCuota ?? "")}</td><td>{Fmt(cu.Monto ?? 0, moneda)} — {cu.FechaVencimiento:dd/MM/yy}</td></tr>");
        }
        else
        {
            sb.Append($"<tr><td>Tipo</td><td>Crédito</td></tr>");
            sb.Append($"<tr><td>Monto Crédito</td><td>{Fmt(c.MontoCredito ?? 0, moneda)}</td></tr>");
            if (cuotas.Any())
            {
                sb.Append($"<tr><td colspan=\"2\" class=\"bold\" style=\"padding-top:2px;\">Cuotas:</td></tr>");
                foreach (var cu in cuotas)
                    sb.Append($"<tr><td>{HE(cu.NumeroCuota ?? "")}</td><td>{Fmt(cu.Monto ?? 0, moneda)} — {cu.FechaVencimiento:dd/MM/yy}</td></tr>");
            }
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    // ── Sección detracción ───────────────────────────────────────────────────
    private static string BuildDetraccionHtml(
        List<Domain.Entities.Detraccion> detracciones,
        string moneda,
        string? tipoComprobante)
    {
        if (!detracciones.Any()) return "";
        var sb = new StringBuilder();
        sb.Append("<p class=\"sec-title\">DETRACCIÓN</p>");
        sb.Append("<table class=\"pagos\">");
        foreach (var det in detracciones)
        {
            if (tipoComprobante == "01")
                sb.Append($"<tr><td>Cta. BN</td><td>{HE(det.CuentaBancoDetraccion ?? "-")}</td></tr>");
            sb.Append($"<tr><td>% Detrac.</td><td>{det.PorcentajeDetraccion:F2}%</td></tr>");
            sb.Append($"<tr><td>Monto</td><td>{Fmt(det.MontoDetraccion ?? 0, moneda)}</td></tr>");
        }
        sb.Append("</table>");
        return sb.ToString();
    }

    // ── Vales (segunda sección) ──────────────────────────────────────────────
    private static string BuildValesHtml(
        Domain.Entities.Comprobante c,
        List<Domain.Entities.Vale> vales)
    {
        if (!vales.Any()) return "";
        var sb = new StringBuilder();
        foreach (var vale in vales)
        {
            sb.Append("<div style=\"page-break-before:always;\"></div>");
            sb.Append($"<p class=\"center bold\" style=\"font-size:9px;margin-top:6px;\">COD. VALE: {HE(c.NumeroCompleto ?? "")}</p>");
            if (!string.IsNullOrWhiteSpace(vale.Descripcion))
            {
                var desc = vale.Descripcion
                    .Replace("<br><br>", "\n\n", StringComparison.OrdinalIgnoreCase)
                    .Replace("<br />",   "\n",   StringComparison.OrdinalIgnoreCase)
                    .Replace("<br/>",    "\n",   StringComparison.OrdinalIgnoreCase)
                    .Replace("<br>",     "\n",   StringComparison.OrdinalIgnoreCase);
                sb.Append($"<p style=\"font-size:8px;white-space:pre-line;\">{HE(desc)}</p>");
            }
            sb.Append($"<p class=\"small\">Emitido: {c.FechaEmision:dd/MM/yyyy} {c.HoraEmision:HH:mm:ss}</p>");
            sb.Append($"<p class=\"small\">Válido hasta: {c.FechaEmision.AddMonths(1):dd/MM/yyyy}</p>");
        }
        return sb.ToString();
    }

    // ── QR ───────────────────────────────────────────────────────────────────
    private static string GenerarQrBase64(Domain.Entities.Comprobante c)
    {
        var contenido = string.Join("|",
            c.EmpresaRuc ?? "",
            c.TipoComprobante ?? "",
            c.Serie ?? "",
            (c.Correlativo ?? 0).ToString("D8"),
            (c.TotalIGV ?? 0).ToString("F2"),
            (c.ImporteTotal ?? 0).ToString("F2"),
            c.FechaEmision.ToString("yyyy-MM-dd"),
            c.ClienteTipoDoc ?? "0",
            c.ClienteNumDoc ?? "0",
            c.CodigoHashCPE ?? "");

        using var gen  = new QRCodeGenerator();
        using var data = gen.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
        using var code = new PngByteQRCode(data);

        byte[] dark  = [26, 43, 74, 255];
        byte[] light = [255, 255, 255, 255];
        var bytes = code.GetGraphic(20, dark, light);
        return Convert.ToBase64String(bytes);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static void FilaTot(StringBuilder sb, string label, string valor) =>
        sb.Append($"<tr><td>{label}</td><td>{valor}</td></tr>");

    private static string Fmt(decimal monto, string moneda) =>
        moneda == "USD" ? $"$ {monto:F2}" : $"S/ {monto:F2}";

    /// <summary>Escapa HTML básico y codifica @ para evitar que Cloudflare oculte emails.</summary>
    private static string HE(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;")
         .Replace("@", "&#64;");
}
