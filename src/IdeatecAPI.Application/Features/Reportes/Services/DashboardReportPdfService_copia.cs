// ARCHIVO DE RESPALDO — versión original con HTML + PuppeteerSharp.
// Usar si el servidor de producción tiene instaladas las dependencias de Chromium:
//   apt-get install -y libglib2.0-0 libnss3 libatk1.0-0 libatk-bridge2.0-0
//                      libcups2 libdrm2 libxkbcommon0 libxcomposite1
//                      libxdamage1 libxfixes3 libxrandr2 libgbm1 libasound2
//
// Para activar: renombrar esta clase a DashboardReportPdfService,
// registrarla en DI en lugar de la versión QuestPDF actual,
// y asegurarse de que PuppeteerSharp esté referenciado en el proyecto.

using System.Text;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using IdeatecAPI.Application.Common.Interfaces.Persistence.Reportes;
using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Application.Features.Reportes.Services;

public class DashboardReportPdfServiceHtml
{
    private static readonly SemaphoreSlim _browserLock = new(1, 1);
    private static IBrowser? _browser;

    // Colores de headers de tabla (familia navy brand)
    private const string CFacturas = "#1A2B4A";
    private const string CBoletas  = "#1E4A7A";
    private const string CNC       = "#1A3A60";
    private const string CND       = "#1C5080";
    private const string CNV       = "#2B6B9A";
    private const string CComision = "#1A3D5C";

    // Colores exclusivos del donut
    private const string DFacturas = "#43A047";   // verde
    private const string DBoletas  = "#29B6F6";   // azul cielo
    private const string DNC       = "#EF5350";   // rojo — notas de crédito
    private const string DND       = "#1A2B4A";   // navy brand — 5° tipo
    private const string DNV       = "#00BFA5";   // teal — notas de venta

    public async Task<byte[]> ExportarDashboardReportPdfAsync(DashboardReportDto d)
    {
        var html = GenerarHtml(d);
        return await ConvertirAPdf(html);
    }

    private static string Fmt(decimal v) => $"S/ {v:N2}";
    private static string Esc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "—");
    private static string Trunc(string? s, int max) =>
        s == null ? "—" : s.Length > max ? s[..max] + "…" : s;

    private string GenerarHtml(DashboardReportDto d)
    {
        var esSoloDia   = d.FechaDesde.Date == d.FechaHasta.Date;
        var labelVentas = esSoloDia ? "Ventas del Día" : "Ventas del Período";

        var periodo = esSoloDia
            ? d.FechaDesde.ToString("dd/MM/yyyy")
            : $"{d.FechaDesde:dd/MM/yyyy} al {d.FechaHasta:dd/MM/yyyy}";

        // Distribución — ordenada de mayor a menor para leyenda y donut
        var dist = d.Distribucion;
        var segs = new (string L, int C, string Clr)[]
        {
            ("Facturas",   dist.Facturas,     DFacturas),
            ("Boletas",    dist.Boletas,      DBoletas),
            ("N. Crédito", dist.NotasCredito, DNC),
            ("N. Débito",  dist.NotasDebito,  DND),
            ("N. Venta",   dist.NotasVenta,   DNV),
        }
        .Where(s => s.C > 0)
        .OrderByDescending(s => s.C)
        .ToArray();
        var totalDist = segs.Sum(s => s.C);

        var donutSvg    = BuildDonut(segs, totalDist);
        var donutLegend = BuildDonutLegend(segs, totalDist);

        // Top comprobantes unificado (facturas + boletas + NV) — sin truncar número
        var todosComp = d.TopComprobantes.Concat(d.TopNotasVenta)
            .OrderByDescending(c => c.ImporteTotal)
            .Take(5)
            .ToList();
        var compR = todosComp.Select((c, i) => new[]
        {
            (i + 1).ToString(),
            c.NumeroCompleto,
            Trunc(c.ClienteRznSocial, 32),
            c.FechaEmision.ToString("dd/MM/yyyy"),
            Fmt(c.ImporteTotal)
        }).ToList();

        var cliR = d.TopClientes.Take(5).Select((c, i) => new[]
        {
            (i + 1).ToString(), Trunc(c.ClienteRznSocial, 40), c.ClienteNumDoc, c.NumDocs.ToString(), Fmt(c.Total)
        }).ToList();

        var medR = d.MediosPago.OrderByDescending(m => m.MontoTotal).Take(5).Select((m, i) => new[]
        {
            (i + 1).ToString(), m.MedioPago ?? "Sin especificar",
            m.VecesUsado.ToString(), Fmt(m.MontoTotal)
        }).ToList();

        var comR = d.TopComisionTarjeta.Take(5).Select((c, i) => new[]
        {
            (i + 1).ToString(),
            Trunc(c.ClienteRznSocial, 40), Fmt(c.ComisionTarjeta)
        }).ToList();

        var proR = d.TopProductos.Take(8).Select((p, i) => new[]
        {
            (i + 1).ToString(), Trunc(p.Descripcion, 80),
            p.TotalCantidad.ToString("N2"), Fmt(p.TotalMonto)
        }).ToList();

        var usrR = d.VentasPorUsuario.Select((u, i) => new[]
        {
            (i + 1).ToString(), Esc(u.NombreUsuario),
            u.TotalDocumentos.ToString(), Fmt(u.TotalVentas)
        }).ToList();

        // Sección central derecha
        string rightPanel;
        if (esSoloDia)
        {
            rightPanel = BuildTableCard("Ventas por Usuario", CBoletas,
                new[] { "#", "Usuario", "Cant.", "Total" },
                new[] { "20px", "", "40px", "70px" },
                new[] { false, false, true, true }, usrR);
        }
        else if (d.Grafico.Count > 1)
        {
            rightPanel = BuildVerticalBarChart(d.Grafico);
        }
        else
        {
            rightPanel = "";
        }

        var notaComision = d.TotalComisionPOS > 0
            ? "<div style=\"font-size:6.5pt;color:#9CA3AF;font-style:italic;padding:0 2px\">* Algunos comprobantes incluyen su comisión de tarjeta en el monto mostrado; esta comisión no es declarada ante SUNAT.</div>"
            : "";

        var sucC   = string.IsNullOrEmpty(d.NombreSucursal) ? "" : $"<span><b>Sucursal:</b> {Esc(d.NombreSucursal)}</span>";
        var usrC   = string.IsNullOrEmpty(d.NombreUsuario)  ? "" : $"<span><b>Usuario:</b> {Esc(d.NombreUsuario)}</span>";
        var pctStr = d.PorcentajeGanancias.ToString("N1") + "%";

        return $$"""
<!DOCTYPE html><html><head><meta charset="utf-8"><style>
*{margin:0;padding:0;box-sizing:border-box}
body{font-family:'Segoe UI',system-ui,sans-serif;font-size:7.5pt;color:#111827;background:#FFFFFF}
@page{size:210mm 297mm;margin:0}
.pg{width:210mm;min-height:297mm;display:flex;flex-direction:column;background:#FFFFFF;padding:7mm 8mm}
.hdr{background:#1A2B4A;padding:10px 14px;display:flex;justify-content:space-between;align-items:center;border-radius:8px;margin-bottom:6px}
.hdr .co{font-size:12pt;font-weight:700;color:#FFF;letter-spacing:.3px}
.hdr .ruc{font-size:7pt;color:#BFDBFE;margin-top:2px}
.hdr .ti{font-size:13pt;font-weight:700;color:#FFF;text-align:right;letter-spacing:.3px}
.hdr .dt{font-size:7pt;color:#BFDBFE;text-align:right;margin-top:2px}
.chips{background:#EFF6FF;border:1px solid #DBEAFE;border-radius:6px;padding:4px 12px;display:flex;gap:18px;font-size:7pt;color:#374151;margin-bottom:7px}
.chips b{color:#1D4ED8;margin-right:3px}
.ct{flex:1;display:flex;flex-direction:column;gap:9px}
.card{background:#FFFFFF;border:1px solid #E5E7EB;border-radius:8px;overflow:hidden}
.card-h{padding:5px 10px;font-size:7.5pt;font-weight:700;color:#FFFFFF;display:flex;align-items:center;gap:6px;text-transform:uppercase;letter-spacing:.4px}
.dot{width:8px;height:8px;border-radius:50%;flex-shrink:0}
.row3{display:grid;grid-template-columns:repeat(3,1fr);gap:6px}
.kpi-c{border-radius:8px;padding:10px 13px;overflow:hidden}
.kpi-c .lb{font-size:6.5pt;text-transform:uppercase;letter-spacing:.4px;opacity:.85;font-weight:600;color:#fff}
.kpi-c .vl{font-size:16pt;font-weight:700;margin-top:3px;white-space:nowrap;color:#fff}
.row-mid{display:grid;grid-template-columns:1fr 2fr;gap:6px}
.row2{display:grid;grid-template-columns:3fr 2fr;gap:6px}
.donut-wrap{display:flex;flex-direction:column;align-items:center;padding:8px 8px 6px}
.donut{display:block}
.lg-item{display:flex;align-items:center;gap:5px;font-size:7.5pt;line-height:1.85;padding:0 2px}
.lg-dot{width:7px;height:7px;border-radius:2px;flex-shrink:0}
.lg-lbl{flex:1;color:#374151}.lg-cnt{width:26px;text-align:right;color:#6B7280}
.lg-pct{width:56px;text-align:right;font-weight:700}
table{width:100%;border-collapse:collapse;table-layout:fixed;font-size:7pt}
th{padding:4px 6px;font-weight:700;font-size:6.5pt;text-transform:uppercase;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;letter-spacing:.3px}
td{padding:5px 6px;border-bottom:1px solid #F3F4F6;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;color:#374151}
tr:last-child td{border-bottom:none}
tr:nth-child(even) td{background:#FAFAFA}
.empty-td{text-align:center;font-style:italic;color:#9CA3AF;padding:10px!important}
.vchart-wrap{padding:8px 10px 2px}
.vchart-lbl{text-align:center;font-size:7pt;color:#6B7280;padding-bottom:4px}
.vchart-svg{display:block;width:100%;height:auto}
.ftr{margin-top:auto;padding-top:6px;border-top:1px solid #E5E7EB;display:flex;justify-content:space-between;font-size:6pt;color:#9CA3AF}
</style></head><body>
<div class="pg">
<div class="hdr">
 <div><div class="co">{{Esc(d.EmpresaRazonSocial)}}</div><div class="ruc">R.U.C. {{d.EmpresaRuc}}</div></div>
 <div><div class="ti">DASHBOARD VENTAS</div><div class="dt">{{d.FechaGeneracion:dd/MM/yyyy  HH:mm}}</div></div>
</div>
<div class="chips"><span><b>Período:</b> {{periodo}}</span>{{sucC}}{{usrC}}</div>
<div class="ct">
 <div class="row3">
  {{BuildKpiColor(labelVentas,    Fmt(d.VentasTotal),              "#2D4A72","#3B5A88")}}
  {{BuildKpiColor("Total IGV",    Fmt(d.TotalIGV),                 "#294468","#375480")}}
  {{BuildKpiColor("Documentos",   d.TotalDocumentos.ToString("N0"),"#243D62","#324F7A")}}
 </div>
 <div class="row3">
  {{BuildKpiColor("Ganancias",      Fmt(d.Ganancias),             "#2A4570","#3A5888")}}
  {{BuildKpiColor("% de Ganancias", pctStr,                       "#284268","#365078")}}
  {{BuildKpiColor("Comisión POS",   Fmt(d.TotalComisionPOS),      "#2C4870","#3C5A84")}}
 </div>
 <div class="row-mid">
  <div class="card">
   <div class="card-h" style="background:{{CBoletas}}">Distribución de Documentos</div>
   <div class="donut-wrap">{{donutSvg}}<div style="width:100%;margin-top:5px">{{donutLegend}}</div></div>
  </div>
  {{rightPanel}}
 </div>
 <div class="row2">
  {{BuildTableCard("Ventas más Altas", CBoletas,
      new[]{"#","Comprobante","Cliente","Fecha","Importe"},
      new[]{"20px","90px","","60px","78px"},
      new[]{false,false,false,false,true}, compR)}}
  {{BuildTableCard("Medios de Pago", CBoletas,
      new[]{"#","Medio de Pago","Usos","Monto"},
      new[]{"20px","110px","42px","78px"},
      new[]{false,false,true,true}, medR)}}
 </div>
 <div class="row2">
  {{BuildTableCard("Clientes con más Ventas", CBoletas,
      new[]{"#","Cliente","Doc.","Cant.","Total"},
      new[]{"20px","","68px","40px","78px"},
      new[]{false,false,false,true,true}, cliR)}}
  {{BuildTableCard("Comisión Tarjeta", CBoletas,
      new[]{"#","Cliente","Comisión"},
      new[]{"20px","","78px"},
      new[]{false,false,true}, comR)}}
 </div>
 {{BuildTableCard("Productos más Vendidos", CBoletas,
     new[]{"#","Producto","Cantidad","Monto"},
     new[]{"20px","","72px","78px"},
     new[]{false,false,true,true}, proR)}}
 {{notaComision}}
</div>
<div class="ftr">
 <span>Generado el {{d.FechaGeneracion:dd/MM/yyyy}} a las {{d.FechaGeneracion:HH:mm:ss}} · Powered by Factufly · Ideatec</span>
 <span>Reporte Dashboard Consolidado</span>
 <span>Pág. 1 de 1</span>
</div>
</div></body></html>
""";
    }

    private static string BuildKpiColor(string label, string value, string bg1, string bg2)
    {
        return $"<div class=\"kpi-c\" style=\"background:linear-gradient(135deg,{bg1},{bg2})\"><div class=\"lb\">{Esc(label)}</div><div class=\"vl\">{Esc(value)}</div></div>";
    }

    private static string BuildVerticalBarChart(List<GraficoBarraDto> grafico)
    {
        if (grafico.Count == 0) return "";

        var agruparSemanas = grafico.Count > 14;
        List<(string Etiqueta, decimal Total)> puntos;
        if (agruparSemanas)
        {
            puntos = new List<(string, decimal)>();
            for (int i = 0; i < grafico.Count; i += 7)
            {
                var chunk = grafico.Skip(i).Take(7).ToList();
                var total = chunk.Sum(c => c.Ventas + c.VentasNV);
                var desde = chunk.First().Etiqueta;
                var hasta = chunk.Last().Etiqueta;
                var lbl   = desde == hasta ? desde : $"{desde}-{hasta}";
                puntos.Add((lbl, total));
            }
        }
        else
        {
            puntos = grafico.TakeLast(20).Select(g => (g.Etiqueta ?? "", g.Ventas + g.VentasNV)).ToList();
        }

        var n = puntos.Count;
        if (n == 0) return "";

        var maxV  = puntos.Max(p => p.Total);
        if (maxV == 0) maxV = 1;
        var usarK = agruparSemanas || n > 7;

        const int W = 440, H = 118;
        const int padL = 4, padR = 4, padT = 20, padB = 28;
        var chartW = W - padL - padR;
        var chartH = H - padT - padB;
        var step   = chartW / n;
        var barW   = Math.Max(6, step - 4);

        var sb = new StringBuilder();
        sb.Append($"<svg viewBox=\"0 0 {W} {H}\" class=\"vchart-svg\" xmlns=\"http://www.w3.org/2000/svg\">");
        sb.Append("<defs><linearGradient id=\"vbg\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\">" +
                  "<stop offset=\"0%\" stop-color=\"#3B5A88\"/>" +
                  "<stop offset=\"100%\" stop-color=\"#2D4A72\"/>" +
                  "</linearGradient></defs>");

        for (int g = 1; g <= 3; g++)
        {
            var gy = padT + chartH - (int)(chartH * g / 4.0);
            sb.Append($"<line x1=\"{padL}\" y1=\"{gy}\" x2=\"{W - padR}\" y2=\"{gy}\" stroke=\"#E5E7EB\" stroke-width=\"0.5\"/>");
        }
        sb.Append($"<line x1=\"{padL}\" y1=\"{padT + chartH}\" x2=\"{W - padR}\" y2=\"{padT + chartH}\" stroke=\"#D1D5DB\" stroke-width=\"1\"/>");

        for (int i = 0; i < n; i++)
        {
            var total = puntos[i].Total;
            var pct   = (double)total / (double)maxV;
            var barH  = (int)(chartH * pct);
            if (barH < 2 && total > 0) barH = 2;

            var x = padL + i * step + (step - barW) / 2;
            var y = padT + chartH - barH;

            sb.Append($"<rect x=\"{x}\" y=\"{y}\" width=\"{barW}\" height=\"{barH}\" rx=\"2\" fill=\"url(#vbg)\"/>");

            var lbl = puntos[i].Etiqueta;
            sb.Append($"<text x=\"{x + barW / 2}\" y=\"{H - 10}\" text-anchor=\"middle\" fill=\"#374151\" font-family=\"Segoe UI\" font-size=\"{(agruparSemanas ? 6.5 : 7.5)}\" font-weight=\"600\">{Esc(lbl)}</text>");

            if (total > 0)
            {
                var v = usarK
                    ? (total >= 1000 ? $"S/{total / 1000m:N1}K" : $"S/{total:N0}")
                    : Fmt(total);
                sb.Append($"<text x=\"{x + barW / 2}\" y=\"{y - 2}\" text-anchor=\"middle\" fill=\"#1A2B4A\" font-family=\"Segoe UI\" font-size=\"7\" font-weight=\"700\">{Esc(v)}</text>");
            }
        }

        sb.Append("</svg>");

        var pieLbl = agruparSemanas ? "Semanas del período" : "Fechas del período";
        return $"<div class=\"card\"><div class=\"card-h\" style=\"background:{CBoletas}\">Ventas por Período</div>" +
               $"<div class=\"vchart-wrap\">{sb}</div>" +
               $"<div class=\"vchart-lbl\">{pieLbl}</div></div>";
    }

    private static string BuildDonut((string L, int C, string Clr)[] segs, int total)
    {
        if (total == 0) return "<div style=\"padding:20px;text-align:center;color:#6B7280;font-style:italic\">Sin documentos</div>";
        double r = 32, cx = 50, cy = 50, sw = 10;
        var circ = 2 * Math.PI * r;
        var sb   = new StringBuilder();
        sb.Append("<svg viewBox=\"0 0 100 100\" class=\"donut\" width=\"90\" height=\"90\">");
        sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{r:F1}\" fill=\"none\" stroke=\"#F3F4F6\" stroke-width=\"{sw}\"/>");
        var offset = 0.0;
        foreach (var s in segs)
        {
            var len = (double)s.C / total * circ;
            sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{r:F1}\" fill=\"none\" stroke=\"{s.Clr}\" stroke-width=\"{sw}\" stroke-dasharray=\"{len:F2} {circ:F2}\" stroke-dashoffset=\"{(-offset + circ / 4):F2}\" style=\"transform-origin:center\"/>");
            offset += len;
        }
        sb.Append($"<text x=\"{cx}\" y=\"{cy - 3}\" text-anchor=\"middle\" fill=\"#111827\" font-family=\"Segoe UI\" font-size=\"15\" font-weight=\"700\">{total:N0}</text>");
        sb.Append($"<text x=\"{cx}\" y=\"{cy + 8}\" text-anchor=\"middle\" fill=\"#6B7280\" font-family=\"Segoe UI\" font-size=\"6.5\">DOCUMENTOS</text>");
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string BuildDonutLegend((string L, int C, string Clr)[] segs, int total)
    {
        var sb = new StringBuilder();
        foreach (var s in segs)
        {
            var pct = total > 0 ? (decimal)s.C / total * 100 : 0;
            sb.Append($"<div class=\"lg-item\"><span class=\"lg-dot\" style=\"background:{s.Clr}\"></span><span class=\"lg-lbl\">{Esc(s.L)}</span><span class=\"lg-cnt\">{s.C:N0}</span><span class=\"lg-pct\" style=\"color:{s.Clr}\">{pct:N1}%</span></div>");
        }
        return sb.ToString();
    }

    private static string BuildTableCard(string title, string accent, string[] headers, string[] colW, bool[] right, List<string[]> rows)
    {
        var thBg = accent + "26";
        var sb = new StringBuilder();
        sb.Append($"<div class=\"card\"><div class=\"card-h\" style=\"background:{accent}\">{Esc(title)}</div>");
        sb.Append("<table><colgroup>");
        foreach (var w in colW) sb.Append(string.IsNullOrEmpty(w) ? "<col>" : $"<col style=\"width:{w}\">");
        sb.Append("</colgroup><thead><tr>");
        for (int i = 0; i < headers.Length; i++)
        {
            var al = right[i] ? "text-align:right;" : "";
            sb.Append($"<th style=\"{al}background:{thBg};color:{accent}\">{Esc(headers[i])}</th>");
        }
        sb.Append("</tr></thead><tbody>");
        if (rows.Count == 0)
            sb.Append($"<tr><td colspan=\"{headers.Length}\" class=\"empty-td\">Sin datos en el período</td></tr>");
        else
            foreach (var row in rows)
            {
                sb.Append("<tr>");
                for (int c = 0; c < row.Length; c++)
                {
                    var al = right[c] ? " style=\"text-align:right\"" : "";
                    sb.Append($"<td{al}>{Esc(row[c])}</td>");
                }
                sb.Append("</tr>");
            }
        sb.Append("</tbody></table></div>");
        return sb.ToString();
    }

    private static async Task<byte[]> ConvertirAPdf(string html)
    {
        var browser = await GetBrowserAsync();
        await using var page = await browser.NewPageAsync();
        try
        {
            await page.SetContentAsync(html, new SetContentOptions
            {
                WaitUntil = [WaitUntilNavigation.Load]
            });
            return await page.PdfDataAsync(new PdfOptions
            {
                Width  = "210mm",
                Height = "297mm",
                PrintBackground = true,
                MarginOptions = new MarginOptions
                {
                    Top = "0mm", Bottom = "0mm", Left = "0mm", Right = "0mm"
                }
            });
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private static async Task<IBrowser> GetBrowserAsync()
    {
        if (_browser is { IsClosed: false }) return _browser;
        await _browserLock.WaitAsync();
        try
        {
            if (_browser is { IsClosed: false }) return _browser;
            var fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync();
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-gpu", "--disable-dev-shm-usage"]
            });
            return _browser;
        }
        finally
        {
            _browserLock.Release();
        }
    }
}
