using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IdeatecAPI.Application.Common.Interfaces.Persistence.Reportes;
using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Application.Features.Reportes.Services;

public class DashboardReportPdfService : IDashboardReportPdfService
{
    // Paleta principal
    private const string CNavy    = "#1A2B4A";
    private const string CNavyMid = "#1E4A7A";
    private const string CWhite   = "#FFFFFF";
    private const string CBorde   = "#E5E7EB";
    private const string CTexto   = "#374151";
    private const string CSubtxt  = "#6B7280";
    private const string CThBg    = "#EEF3FA";
    private const string CChipBg  = "#EFF6FF";
    private const string CChipBrd = "#DBEAFE";
    private const string CKpiBg   = "#2E4468";

    // Ancho útil de página (A4 - márgenes 8mm izq/der) y separación entre tarjetas, en puntos
    private const float PageContentWidthPt = 549.9f;
    private const float RowGapPt = 5f;

    // Colores donut
    private const string DFacturas = "#43A047";
    private const string DBoletas  = "#29B6F6";
    private const string DNC       = "#EF5350";
    private const string DND       = "#1A2B4A";
    private const string DNV       = "#00BFA5";

    public Task<byte[]> ExportarDashboardReportPdfAsync(DashboardReportDto d)
    {
        QuestPDF.Settings.EnableDebugging = true;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(7, Unit.Millimetre);
                page.MarginBottom(7, Unit.Millimetre);
                page.MarginLeft(8, Unit.Millimetre);
                page.MarginRight(8, Unit.Millimetre);
                page.DefaultTextStyle(x =>
                    x.FontFamily(Fonts.Lato).FontSize(7.5f).FontColor(CTexto));
                page.Content().Element(c => BuildDashboard(c, d));
            });
        });

        return Task.FromResult(doc.GeneratePdf());
    }

    // ════════════════════════════════════════════════════════════════════
    // LAYOUT PRINCIPAL
    // ════════════════════════════════════════════════════════════════════
    private static void BuildDashboard(IContainer root, DashboardReportDto d)
    {
        var esSoloDia   = d.FechaDesde.Date == d.FechaHasta.Date;
        var labelVentas = esSoloDia ? "Ventas del Día" : "Ventas del Período";
        var periodo     = esSoloDia
            ? d.FechaDesde.ToString("dd/MM/yyyy")
            : $"{d.FechaDesde:dd/MM/yyyy} al {d.FechaHasta:dd/MM/yyyy}";

        // Distribución de documentos
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

        // Top comprobantes unificado
        var todosComp = d.TopComprobantes.Concat(d.TopNotasVenta)
            .OrderByDescending(c => c.ImporteTotal)
            .Take(5)
            .ToList();

        root.Column(col =>
        {
            // ── 1. HEADER ─────────────────────────────────────────────────
            col.Item().Background(CNavy).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(d.EmpresaRazonSocial ?? "")
                        .Bold().FontSize(12).FontColor(CWhite);
                    left.Item().Text($"R.U.C. {d.EmpresaRuc}")
                        .FontSize(7).FontColor("#BFDBFE");
                });
                row.RelativeItem().Column(right =>
                {
                    right.Item().AlignRight().Text("DASHBOARD VENTAS")
                        .Bold().FontSize(13).FontColor(CWhite);
                    right.Item().AlignRight()
                        .Text(d.FechaGeneracion.ToString("dd/MM/yyyy  HH:mm"))
                        .FontSize(7).FontColor("#BFDBFE");
                });
            });

            col.Item().PaddingTop(5);

            // ── 2. CHIPS (período / sucursal / usuario) ────────────────────
            col.Item()
                .Background(CChipBg).Border(0.5f).BorderColor(CChipBrd)
                .PaddingHorizontal(12).PaddingVertical(4)
                .Row(row =>
                {
                    row.AutoItem().Text(txt =>
                    {
                        txt.Span("Período: ").Bold().FontColor("#1D4ED8");
                        txt.Span(periodo);
                    });
                    if (!string.IsNullOrEmpty(d.NombreSucursal))
                        row.AutoItem().PaddingLeft(18).Text(txt =>
                        {
                            txt.Span("Sucursal: ").Bold().FontColor("#1D4ED8");
                            txt.Span(d.NombreSucursal);
                        });
                    if (!string.IsNullOrEmpty(d.NombreUsuario))
                        row.AutoItem().PaddingLeft(18).Text(txt =>
                        {
                            txt.Span("Usuario: ").Bold().FontColor("#1D4ED8");
                            txt.Span(d.NombreUsuario);
                        });
                });

            col.Item().PaddingTop(5);

            // ── 3. KPI FILA 1 ─────────────────────────────────────────────
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c => BuildKpi(c, labelVentas, Fmt(d.VentasTotal)));
                row.ConstantItem(5);
                row.RelativeItem().Element(c => BuildKpi(c, "Total IGV", Fmt(d.TotalIGV)));
                row.ConstantItem(5);
                row.RelativeItem().Element(c => BuildKpi(c, "Documentos", d.TotalDocumentos.ToString("N0")));
            });

            col.Item().PaddingTop(4);

            // ── 4. KPI FILA 2 ─────────────────────────────────────────────
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(c => BuildKpi(c, "Ganancias", Fmt(d.Ganancias)));
                row.ConstantItem(5);
                row.RelativeItem().Element(c =>
                    BuildKpi(c, "% de Ganancias", d.PorcentajeGanancias.ToString("N1") + "%"));
                row.ConstantItem(5);
                row.RelativeItem().Element(c => BuildKpi(c, "Comisión POS", Fmt(d.TotalComisionPOS)));
            });

            col.Item().PaddingTop(5);

            // ── 5. FILA MEDIA: donut izq. | gráfico o tabla der. ──────────
            col.Item().Row(row =>
            {
                // Donut
                row.RelativeItem(1).Border(0.5f).BorderColor(CBorde).Column(card =>
                {
                    card.Item().Background(CNavyMid)
                        .PaddingHorizontal(10).PaddingVertical(5)
                        .Text("DISTRIBUCIÓN DE DOCUMENTOS")
                        .Bold().FontSize(6.5f).FontColor(CWhite);

                    card.Item().Padding(8).Column(inner =>
                    {
                        inner.Item().AlignCenter()
                            .Width(90)
                            .Svg(BuildDonutSvg(segs, totalDist));

                        inner.Item().PaddingTop(5)
                            .Element(c => BuildLeyendaDonut(c, segs, totalDist));
                    });
                });

                row.ConstantItem(5);

                // Panel derecho
                row.RelativeItem(2).Element(c =>
                {
                    if (esSoloDia)
                    {
                        BuildTableCard(c,
                            "VENTAS POR USUARIO", CNavyMid,
                            new[] { "#", "Usuario", "Cant.", "Total" },
                            new[] { false, false, true, true },
                            new[] { 0.5f, 4f, 1.2f, 2f },
                            d.VentasPorUsuario.Select((u, i) => new[]
                            {
                                (i + 1).ToString(),
                                u.NombreUsuario ?? "—",
                                u.TotalDocumentos.ToString(),
                                Fmt(u.TotalVentas)
                            }).ToList(),
                            (PageContentWidthPt - RowGapPt) * 2f / 3f);
                    }
                    else if (d.Grafico.Count > 1)
                    {
                        c.Border(0.5f).BorderColor(CBorde).Column(card =>
                        {
                            card.Item().Background(CNavyMid)
                                .PaddingHorizontal(10).PaddingVertical(5)
                                .Text("VENTAS POR PERÍODO")
                                .Bold().FontSize(6.5f).FontColor(CWhite);

                            card.Item()
                                .PaddingHorizontal(8).PaddingTop(6)
                                .Svg(BuildBarChartSvg(d.Grafico));

                            card.Item().AlignCenter().PaddingBottom(4)
                                .Text(d.Grafico.Count > 14 ? "Semanas del período" : "Fechas del período")
                                .FontSize(7).FontColor(CSubtxt);
                        });
                    }
                    else
                    {
                        c.Column(_ => { });
                    }
                });
            });

            col.Item().PaddingTop(5);

            // ── 6. VENTAS MÁS ALTAS | MEDIOS DE PAGO ─────────────────────
            col.Item().Row(row =>
            {
                row.RelativeItem(3).Element(c => BuildTableCard(c,
                    "VENTAS MÁS ALTAS", CNavyMid,
                    new[] { "#", "Comprobante", "Cliente", "Fecha", "Importe" },
                    new[] { false, false, false, false, true },
                    new[] { 0.5f, 2.5f, 3.5f, 1.6f, 1.9f },
                    todosComp.Select((comp, i) => new[]
                    {
                        (i + 1).ToString(),
                        comp.NumeroCompleto,
                        comp.ClienteRznSocial ?? "—",
                        comp.FechaEmision.ToString("dd/MM/yyyy"),
                        Fmt(comp.ImporteTotal)
                    }).ToList(),
                    (PageContentWidthPt - RowGapPt) * 3f / 5f));

                row.ConstantItem(5);

                row.RelativeItem(2).Element(c => BuildTableCard(c,
                    "MEDIOS DE PAGO", CNavyMid,
                    new[] { "#", "Medio de Pago", "Usos", "Monto" },
                    new[] { false, false, true, true },
                    new[] { 0.5f, 3f, 1.2f, 2.3f },
                    d.MediosPago.OrderByDescending(m => m.MontoTotal).Take(5)
                        .Select((m, i) => new[]
                        {
                            (i + 1).ToString(),
                            m.MedioPago ?? "Sin especificar",
                            m.VecesUsado.ToString(),
                            Fmt(m.MontoTotal)
                        }).ToList(),
                        (PageContentWidthPt - RowGapPt) * 2f / 5f));
            });

            col.Item().PaddingTop(5);

            // ── 7. CLIENTES | COMISIÓN TARJETA ────────────────────────────
            col.Item().Row(row =>
            {
                row.RelativeItem(3).Element(c => BuildTableCard(c,
                    "CLIENTES CON MÁS VENTAS", CNavyMid,
                    new[] { "#", "Cliente", "Doc.", "Cant.", "Total" },
                    new[] { false, false, false, true, true },
                    new[] { 0.6f, 4.9f, 1.6f, 0.9f, 2f },
                    d.TopClientes.Take(5).Select((cli, i) => new[]
                    {
                        (i + 1).ToString(),
                        cli.ClienteRznSocial ?? "—",
                        cli.ClienteNumDoc,
                        cli.NumDocs.ToString(),
                        Fmt(cli.Total)
                    }).ToList(),
                    (PageContentWidthPt - RowGapPt) * 3f / 5f));

                row.ConstantItem(5);

                row.RelativeItem(2).Element(c => BuildTableCard(c,
                    "COMISIÓN TARJETA", CNavyMid,
                    new[] { "#", "Cliente", "Comisión" },
                    new[] { false, false, true },
                    new[] { 0.5f, 4f, 2f },
                    d.TopComisionTarjeta.Take(5).Select((com, i) => new[]
                    {
                        (i + 1).ToString(),
                        com.ClienteRznSocial ?? "—",
                        Fmt(com.ComisionTarjeta)
                    }).ToList(),
                    (PageContentWidthPt - RowGapPt) * 2f / 5f));
            });

            col.Item().PaddingTop(5);

            // ── 8. PRODUCTOS MÁS VENDIDOS ─────────────────────────────────
            col.Item().Element(c => BuildTableCard(c,
                "PRODUCTOS MÁS VENDIDOS", CNavyMid,
                new[] { "#", "Producto", "Cantidad", "Monto" },
                new[] { false, false, true, true },
                new[] { 0.5f, 7.1f, 0.9f, 2f },
                d.TopProductos.Take(8).Select((p, i) => new[]
                {
                    (i + 1).ToString(),
                    p.Descripcion ?? "—",
                    p.TotalCantidad.ToString("N2"),
                    Fmt(p.TotalMonto)
                }).ToList(),
                PageContentWidthPt));

            // Nota comisión POS
            if (d.TotalComisionPOS > 0)
                col.Item().PaddingTop(4)
                    .Text("* Algunos comprobantes incluyen su comisión de tarjeta en el monto mostrado; esta comisión no es declarada ante SUNAT.")
                    .FontSize(6).FontColor("#9CA3AF").Italic();

            // ── 9. FOOTER ─────────────────────────────────────────────────
            col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(CBorde);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem()
                    .Text($"Generado el {d.FechaGeneracion:dd/MM/yyyy} a las {d.FechaGeneracion:HH:mm:ss} · Powered by Factufly · Ideatec")
                    .FontSize(6).FontColor("#9CA3AF");
                row.AutoItem().AlignCenter()
                    .Text("Reporte Dashboard Consolidado")
                    .FontSize(6).FontColor("#9CA3AF");
                row.RelativeItem().AlignRight()
                    .Text("Pág. 1 de 1")
                    .FontSize(6).FontColor("#9CA3AF");
            });
        });
    }

    // ════════════════════════════════════════════════════════════════════
    // COMPONENTES
    // ════════════════════════════════════════════════════════════════════

    private static void BuildKpi(IContainer container, string label, string value)
    {
        container.Background(CKpiBg).Padding(10).Column(col =>
        {
            col.Item().Text(label.ToUpper())
                .FontSize(6.5f).FontColor("#BFDBFE").Bold();
            col.Item().PaddingTop(3).Text(value)
                .FontSize(16).FontColor(CWhite).Bold();
        });
    }

    private static void BuildTableCard(IContainer container, string title, string accent,
        string[] headers, bool[] right, float[] relWidths, List<string[]> rows, float tableWidthPt)
    {
        var sumWeights = relWidths.Sum();

        container.Border(0.5f).BorderColor(CBorde).Column(card =>
        {
            card.Item().Background(accent)
                .PaddingHorizontal(10).PaddingVertical(5)
                .Text(title).Bold().FontSize(6.5f).FontColor(CWhite);

            card.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    foreach (var w in relWidths)
                        cols.RelativeColumn(w);
                });

                table.Header(h =>
                {
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var idx = i;
                        h.Cell().Background(CThBg)
                            .PaddingHorizontal(idx == 0 ? 2 : 3).PaddingVertical(4)
                            .Element(c =>
                            {
                                if (idx == 0)
                                    c.AlignCenter().Text(headers[idx])
                                        .Bold().FontSize(6.5f).FontColor(accent);
                                else if (right[idx])
                                    c.AlignRight().Text(headers[idx])
                                        .Bold().FontSize(6.5f).FontColor(accent);
                                else
                                    c.Text(headers[idx])
                                        .Bold().FontSize(6.5f).FontColor(accent);
                            });
                    }
                });

                if (rows.Count == 0)
                {
                    table.Cell().ColumnSpan((uint)headers.Length)
                        .Padding(10).AlignCenter()
                        .Text("Sin datos en el período")
                        .FontSize(7).FontColor(CSubtxt).Italic();
                }
                else
                {
                    bool par = false;
                    foreach (var row in rows)
                    {
                        var bg = par ? "#FAFAFA" : CWhite;
                        par = !par;
                        for (int i = 0; i < row.Length; i++)
                        {
                            var idx = i;
                            var padH = idx == 0 ? 2f : 3f;
                            table.Cell().Background(bg)
                                .BorderBottom(0.5f).BorderColor(CBorde)
                                .PaddingHorizontal(padH).PaddingVertical(4)
                                .Element(c =>
                                {
                                    if (idx == 0)
                                    {
                                        c.AlignCenter().Text(row[idx])
                                            .FontSize(7).FontColor(CTexto);
                                    }
                                    else if (right[idx])
                                    {
                                        c.AlignRight().Text(row[idx])
                                            .FontSize(7).FontColor(CTexto);
                                    }
                                    else
                                    {
                                        var colWidthPt = tableWidthPt * relWidths[idx] / sumWeights;
                                        var availTextWidth = colWidthPt - padH * 2;
                                        c.Text(TruncToWidth(row[idx], availTextWidth, 7f))
                                            .FontSize(7).FontColor(CTexto);
                                    }
                                });
                        }
                    }
                }
            });
        });
    }

    private static void BuildLeyendaDonut(IContainer container,
        (string L, int C, string Clr)[] segs, int total)
    {
        container.Column(col =>
        {
            foreach (var s in segs)
            {
                var pct = total > 0 ? (decimal)s.C / total * 100 : 0;
                col.Item().PaddingVertical(1).Row(r =>
                {
                    r.ConstantItem(7).AlignMiddle()
                        .Svg($"<svg viewBox='0 0 6 6' xmlns='http://www.w3.org/2000/svg'>" +
                             $"<rect x='0' y='0' width='6' height='6' rx='1.5' fill='{s.Clr}'/></svg>");

                    r.RelativeItem().PaddingLeft(3).AlignMiddle()
                        .Text(s.L).FontSize(7.5f).FontColor(CTexto);

                    r.ConstantItem(26).AlignRight().AlignMiddle()
                        .Text(s.C.ToString("N0")).FontSize(7).FontColor(CSubtxt);

                    r.ConstantItem(44).AlignRight().AlignMiddle()
                        .Text($"{pct:N1}%").Bold().FontSize(7).FontColor(s.Clr);
                });
            }
        });
    }

    // ════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════

    private static string Fmt(decimal v) => $"S/ {v:N2}";

    // Trunca según el ancho real disponible (pt), no un conteo fijo de caracteres,
    // así se aprovecha todo el ancho de columna y solo se corta cuando de verdad no entra.
    private static string TruncToWidth(string? s, float availableWidthPt, float fontSize)
    {
        if (string.IsNullOrEmpty(s)) return "—";

        const float avgCharWidthFactor = 0.58f; // Lato, texto en mayúsculas incluido
        var maxChars = (int)(availableWidthPt / (fontSize * avgCharWidthFactor));
        if (maxChars < 3) maxChars = 3;

        return s.Length <= maxChars ? s : s[..(maxChars - 1)].TrimEnd() + "…";
    }

    // ════════════════════════════════════════════════════════════════════
    // GENERADORES SVG (misma lógica que la versión anterior)
    // ════════════════════════════════════════════════════════════════════

    private static string BuildDonutSvg((string L, int C, string Clr)[] segs, int total)
    {
        if (total == 0)
            return "<svg viewBox=\"0 0 100 100\" xmlns=\"http://www.w3.org/2000/svg\">" +
                   "<text x=\"50\" y=\"55\" text-anchor=\"middle\" fill=\"#6B7280\" font-size=\"8\">Sin documentos</text></svg>";

        double r = 32, cx = 50, cy = 50, sw = 10;
        var circ = 2 * Math.PI * r;
        var sb = new StringBuilder();
        sb.Append("<svg viewBox=\"0 0 100 100\" xmlns=\"http://www.w3.org/2000/svg\">");
        sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{r:F1}\" fill=\"none\" stroke=\"#F3F4F6\" stroke-width=\"{sw}\"/>");

        var offset = 0.0;
        foreach (var s in segs)
        {
            var len = (double)s.C / total * circ;
            sb.Append($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{r:F1}\" fill=\"none\" stroke=\"{s.Clr}\" " +
                      $"stroke-width=\"{sw}\" stroke-dasharray=\"{len:F2} {circ:F2}\" " +
                      $"stroke-dashoffset=\"{(-offset + circ / 4):F2}\"/>");
            offset += len;
        }

        sb.Append($"<text x=\"{cx}\" y=\"{cy - 3}\" text-anchor=\"middle\" " +
                  $"fill=\"#111827\" font-family=\"sans-serif\" font-size=\"15\" font-weight=\"bold\">{total:N0}</text>");
        sb.Append($"<text x=\"{cx}\" y=\"{cy + 8}\" text-anchor=\"middle\" " +
                  $"fill=\"#6B7280\" font-family=\"sans-serif\" font-size=\"6.5\">DOCUMENTOS</text>");
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string BuildBarChartSvg(List<GraficoBarraDto> grafico)
    {
        if (grafico.Count == 0) return "";

        var agrupar = grafico.Count > 14;
        List<(string Etiqueta, decimal Total)> puntos;

        if (agrupar)
        {
            puntos = new List<(string, decimal)>();
            for (int i = 0; i < grafico.Count; i += 7)
            {
                var chunk = grafico.Skip(i).Take(7).ToList();
                var total = chunk.Sum(c => c.Ventas + c.VentasNV);
                var desde = chunk.First().Etiqueta;
                var hasta = chunk.Last().Etiqueta;
                puntos.Add((desde == hasta ? desde : $"{desde}-{hasta}", total));
            }
        }
        else
        {
            puntos = grafico.TakeLast(20)
                .Select(g => (g.Etiqueta ?? "", g.Ventas + g.VentasNV))
                .ToList();
        }

        var n = puntos.Count;
        if (n == 0) return "";

        var maxV = puntos.Max(p => p.Total);
        if (maxV == 0) maxV = 1;
        var usarK = agrupar || n > 7;

        const int W = 440, H = 118, padL = 4, padR = 4, padT = 20, padB = 28;
        var chartW = W - padL - padR;
        var chartH = H - padT - padB;
        var step   = chartW / n;
        var barW   = Math.Max(6, step - 4);

        var sb = new StringBuilder();
        sb.Append($"<svg viewBox=\"0 0 {W} {H}\" xmlns=\"http://www.w3.org/2000/svg\">");
        sb.Append("<defs><linearGradient id=\"vbg\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\">" +
                  "<stop offset=\"0%\" stop-color=\"#3B5A88\"/>" +
                  "<stop offset=\"100%\" stop-color=\"#2D4A72\"/></linearGradient></defs>");

        for (int g = 1; g <= 3; g++)
        {
            var gy = padT + chartH - (int)(chartH * g / 4.0);
            sb.Append($"<line x1=\"{padL}\" y1=\"{gy}\" x2=\"{W - padR}\" y2=\"{gy}\" " +
                      $"stroke=\"#E5E7EB\" stroke-width=\"0.5\"/>");
        }
        sb.Append($"<line x1=\"{padL}\" y1=\"{padT + chartH}\" x2=\"{W - padR}\" y2=\"{padT + chartH}\" " +
                  $"stroke=\"#D1D5DB\" stroke-width=\"1\"/>");

        for (int i = 0; i < n; i++)
        {
            var total = puntos[i].Total;
            var pct   = (double)total / (double)maxV;
            var barH  = (int)(chartH * pct);
            if (barH < 2 && total > 0) barH = 2;

            var x = padL + i * step + (step - barW) / 2;
            var y = padT + chartH - barH;

            sb.Append($"<rect x=\"{x}\" y=\"{y}\" width=\"{barW}\" height=\"{barH}\" rx=\"2\" fill=\"url(#vbg)\"/>");
            sb.Append($"<text x=\"{x + barW / 2}\" y=\"{H - 10}\" text-anchor=\"middle\" " +
                      $"fill=\"#374151\" font-family=\"sans-serif\" " +
                      $"font-size=\"{(agrupar ? "6.5" : "7.5")}\" font-weight=\"600\">{puntos[i].Etiqueta}</text>");

            if (total > 0)
            {
                var v = usarK
                    ? (total >= 1000 ? $"S/{total / 1000m:N1}K" : $"S/{total:N0}")
                    : Fmt(total);
                sb.Append($"<text x=\"{x + barW / 2}\" y=\"{y - 2}\" text-anchor=\"middle\" " +
                          $"fill=\"#1A2B4A\" font-family=\"sans-serif\" font-size=\"7\" font-weight=\"700\">{v}</text>");
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}
