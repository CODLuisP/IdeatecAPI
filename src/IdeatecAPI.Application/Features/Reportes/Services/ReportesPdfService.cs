using IdeatecAPI.Application.Common.Interfaces.Persistence.Reportes;
using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Reportes.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.RegularExpressions;

namespace IdeatecAPI.Application.Features.Reportes.Services;

public class ReportesPdfService : IReportesPdfService
{
    // ── Colores ───────────────────────────────────────────────────────────────
    private static readonly string Azul      = "#1A2B4A";
    private static readonly string Blanco    = "#FFFFFF";
    private static readonly string GrisClaro = "#F5F7FA";
    private static readonly string GrisBorde = "#D0D7E3";
    private static readonly string AzulCab   = "#2E75B6";
    private static readonly string VerdeFila = "#C6EFCE";

    // ── Helpers compartidos ───────────────────────────────────────────────────
    private static string Filtros(string ruc, string? est, DateTime? desde, DateTime? hasta,
        int? usr, string? cli)
    {
        var partes = new List<string> { $"RUC: {ruc}" };
        if (!string.IsNullOrEmpty(est))        partes.Add($"Establecimiento: {est}");
        if (desde.HasValue)                    partes.Add($"Desde: {desde:dd/MM/yyyy}");
        if (hasta.HasValue)                    partes.Add($"Hasta: {hasta:dd/MM/yyyy}");
        if (usr.HasValue)                      partes.Add($"Usuario: {usr}");
        if (!string.IsNullOrEmpty(cli))        partes.Add($"Cliente: {cli}");
        return string.Join("  |  ", partes);
    }

    private static void BuildCabecera(IContainer c, string titulo, string filtros)
    {
        c.Column(col =>
        {
            col.Item().Background(Azul).Padding(8).AlignCenter()
                .Text(titulo).Bold().FontSize(13).FontColor(Blanco);
            col.Item().Padding(4).AlignCenter()
                .Text(filtros).Italic().FontSize(8).FontColor("#4A5568");
            col.Item().Height(4);
        });
    }

    private static void TH(IContainer c, string txt, bool right = false, int fontSize = 8, int pad = 4)
    {
        var el = c.Background(AzulCab).Padding(pad);
        if (right) el.AlignRight().Text(txt).Bold().FontSize(fontSize).FontColor(Blanco);
        else       el.Text(txt).Bold().FontSize(fontSize).FontColor(Blanco);
    }

    private static void TD(IContainer c, string txt, string bg, bool right = false, bool red = false, int fontSize = 8, int pad = 3)
    {
        var el = c.Background(bg).BorderBottom(1).BorderColor(GrisBorde).Padding(pad);
        var t = right ? el.AlignRight().Text(txt).FontSize(fontSize)
                      : el.Text(txt).FontSize(fontSize);
        if (red) t.FontColor("#C00000");
    }

    private static string Fmt(decimal v) => $"S/ {v:N2}";
    private static string FmtM(decimal v, string m) => m == "USD" ? $"$ {v:N2}" : $"S/ {v:N2}";
    private static byte[] ToBytes(Document doc) => doc.GeneratePdf();

    // ═════════════════════════════════════════════════════════════════════════
    // LISTADO / LIBRO CONTABLE
    // ═════════════════════════════════════════════════════════════════════════
    public Task<byte[]> ExportarListadoPdfAsync(
        string titulo, IEnumerable<ListarComprobanteDTO> datos,
        string ruc, string? codEstablecimiento = null,
        DateTime? fechaDesde = null, DateTime? fechaHasta = null,
        int? usuarioCreacion = null, string? clienteNumDoc = null)
    {
        var lista = datos.ToList();
        var filtros = Filtros(ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);

        // Misma separación que el Excel: ventas del período vs ajustes de otros períodos
        var idsDelPeriodo = lista.Select(x => x.ComprobanteId).ToHashSet();
        bool EsNota(ListarComprobanteDTO x) => x.TipoComprobante == "07" || x.TipoComprobante == "08";
        bool AfectaOtro(ListarComprobanteDTO x) =>
            EsNota(x) && x.ComprobanteAfectadoId.HasValue && !idsDelPeriodo.Contains(x.ComprobanteAfectadoId.Value);

        var ventas  = lista.Where(x => !AfectaOtro(x)).ToList();
        var ajustes = lista.Where(AfectaOtro).ToList();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Portrait A4 (595 × 842) — compacto, font 7pt
                page.Size(PageSizes.A4);
                page.MarginHorizontal(8, Unit.Millimetre);
                page.MarginVertical(8, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily(Fonts.Lato).FontSize(7).FontColor("#1A1A1A"));

                page.Header().Element(h => BuildCabecera(h, titulo, filtros));

                page.Content().PaddingTop(4).Column(col =>
                {
                    // ── VENTAS DEL PERÍODO ────────────────────────────────────
                    col.Item().Background(AzulCab).Padding(3)
                        .Text("VENTAS DEL PERÍODO").Bold().FontSize(8).FontColor(Blanco);
                    col.Item().Table(table => BuildTablaListado(table, ventas,
                        "TOTAL NETO DEL PERÍODO", "#BDD7EE", Azul, fontSize: 7, pad: 2));

                    // Leyenda N. Crédito / N. Débito
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Background("#FFF2CC").Padding(2)
                            .Text("N. Crédito (resta al total)").Italic().FontSize(7).FontColor("#C00000");
                        r.RelativeItem().Background("#E2EFDA").Padding(2).AlignRight()
                            .Text("N. Débito (suma al total)").Italic().FontSize(7).FontColor("#375623");
                    });

                    // ── AJUSTES DE OTROS PERÍODOS ─────────────────────────────
                    if (ajustes.Any())
                    {
                        col.Item().PaddingTop(6).Background("#7030A0").Padding(3)
                            .Text("AJUSTES DE OTROS PERÍODOS (no afectan el total anterior)")
                            .Bold().FontSize(8).FontColor(Blanco);
                        col.Item().Table(table => BuildTablaListado(table, ajustes,
                            "TOTAL AJUSTES", "#E2CFEE", "#7030A0", fontSize: 7, pad: 2));
                        col.Item().PaddingTop(3)
                            .Text("Estas notas afectan comprobantes emitidos en otros períodos y no se incluyen en el total del período.")
                            .Italic().FontSize(7).FontColor("#7030A0");
                    }
                });

                page.Footer().AlignRight().Text(txt =>
                {
                    txt.Span("Pág. ").FontSize(7).FontColor("#4A5568");
                    txt.CurrentPageNumber().FontSize(7).FontColor("#4A5568");
                    txt.Span($"  |  {lista.Count} registros").FontSize(7).FontColor("#4A5568");
                });
            });
        });

        return Task.FromResult(ToBytes(doc));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CONTROL DE CAJA
    // ═════════════════════════════════════════════════════════════════════════
    public Task<byte[]> ExportarControlCajaPdfAsync(
        string titulo, IEnumerable<ListarComprobanteDTO> datos,
        string ruc, string? codEstablecimiento = null,
        DateTime? fechaDesde = null, DateTime? fechaHasta = null,
        int? usuarioCreacion = null, string? clienteNumDoc = null)
    {
        var movimientos = datos.ToList();
        var filtros = Filtros(ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Portrait A4 (595 × 842) — compacto, font 7pt
                page.Size(PageSizes.A4);
                page.MarginHorizontal(8, Unit.Millimetre);
                page.MarginVertical(8, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily(Fonts.Lato).FontSize(7).FontColor("#1A1A1A"));

                page.Header().Element(h => BuildCabecera(h, titulo, filtros));

                page.Content().PaddingTop(4).Column(col =>
                {
                    // ── Movimientos (total incluido dentro de la tabla) ───────
                    col.Item().Background(Azul).Padding(3)
                        .Text("MOVIMIENTOS DEL PERÍODO").Bold().FontSize(8).FontColor(Blanco);
                    col.Item().Table(table => BuildTablaCC(table, movimientos,
                        "TOTAL NETO DEL PERÍODO", "#C6EFCE", Azul, fontSize: 7, pad: 2));
                });

                page.Footer().AlignRight().Text(txt =>
                {
                    txt.Span("Pág. ").FontSize(7).FontColor("#4A5568");
                    txt.CurrentPageNumber().FontSize(7).FontColor("#4A5568");
                });
            });
        });

        return Task.FromResult(ToBytes(doc));
    }

    // ── Tabla Libro Contable (12 col, con # al inicio) ────────────────────────
    private static void BuildTablaListado(TableDescriptor table, List<ListarComprobanteDTO> lista,
        string totalLabel = "TOTAL NETO DEL PERÍODO", string totalBg = "#BDD7EE", string totalBorder = "#1A2B4A",
        int fontSize = 8, int pad = 3)
    {
        // Portrait A4 útil ~549pt — fijas = 454pt → RelativeColumn (Cliente) ~95pt
        table.ColumnsDefinition(cols =>
        {
            cols.ConstantColumn(14);  // #
            cols.ConstantColumn(72);  // N° Comprobante
            cols.ConstantColumn(38);  // Tipo
            cols.ConstantColumn(44);  // F. Emisión
            cols.RelativeColumn();    // Cliente
            cols.ConstantColumn(46);  // Doc. Cliente
            cols.ConstantColumn(46);  // Val. Venta
            cols.ConstantColumn(40);  // IGV
            cols.ConstantColumn(48);  // Total
            cols.ConstantColumn(20);  // Mon
            cols.ConstantColumn(48);  // Estado
            cols.ConstantColumn(38);  // T.Pago
        });

        table.Header(h =>
        {
            h.Cell().Element(c => TH(c, "#",              fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "N° Comprobante", fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Tipo",           fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "F. Emisión",     fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Cliente",        fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Doc. Cliente",   fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Val. Venta",     right: true, fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "IGV",            right: true, fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Total",          right: true, fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Mon",            fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Estado",         fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "T.Pago",         fontSize: fontSize, pad: pad));
        });

        bool par = false; int n = 1;
        foreach (var d in lista)
        {
            var bg = par ? Blanco : GrisClaro; par = !par;
            bool esNC = d.TipoComprobante == "07";
            var tipo = d.TipoComprobante switch
            {
                "01" => "Factura", "03" => "Boleta",
                "07" => "N.Cred",  "08" => "N.Deb", _ => d.TipoComprobante
            };
            var vv  = esNC ? -d.ValorVenta   : d.ValorVenta;
            var igv = esNC ? -d.TotalIGV     : d.TotalIGV;
            var tot = esNC ? -d.ImporteTotal : d.ImporteTotal;

            table.Cell().Element(c => TD(c, (n++).ToString(),                    bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.NumeroCompleto ?? "-",             bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, tipo ?? "",                           bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.FechaEmision.ToString("dd/MM/yy"), bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.Cliente?.RazonSocial ?? "-",       bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.Cliente?.NumeroDocumento ?? "-",   bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, $"{Math.Abs(vv):N2}",  bg, right: true, red: esNC, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, $"{Math.Abs(igv):N2}", bg, right: true, red: esNC, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, $"{Math.Abs(tot):N2}", bg, right: true, red: esNC, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.TipoMoneda ?? "PEN",               bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.EstadoSunat ?? "-",                bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.TipoPago ?? "-",                   bg, fontSize: fontSize, pad: pad));
        }

        // ── Fila TOTAL ────────────────────────────────────────────────────────
        var tvv  = lista.Sum(x => x.TipoComprobante == "07" ? -x.ValorVenta   : x.ValorVenta);
        var tigv = lista.Sum(x => x.TipoComprobante == "07" ? -x.TotalIGV     : x.TotalIGV);
        var timp = lista.Sum(x => x.TipoComprobante == "07" ? -x.ImporteTotal : x.ImporteTotal);

        void TF(IContainer c, string txt, bool right = false) =>
            c.Background(totalBg).BorderTop(1.5f).BorderColor(totalBorder).Padding(pad)
             .Column(col2 =>
             {
                 if (right) col2.Item().AlignRight().Text(txt).Bold().FontSize(fontSize);
                 else       col2.Item().Text(txt).Bold().FontSize(fontSize);
             });

        table.Cell().ColumnSpan(6).Element(c => TF(c, totalLabel));
        table.Cell().Element(c => TF(c, $"{tvv:N2}",  right: true));
        table.Cell().Element(c => TF(c, $"{tigv:N2}", right: true));
        table.Cell().Element(c => TF(c, $"{timp:N2}", right: true));
        table.Cell().ColumnSpan(3).Element(c => TF(c, ""));
    }

    private static void BuildTablaCC(TableDescriptor table, List<ListarComprobanteDTO> lista,
        string totalLabel = "TOTAL NETO DEL PERÍODO", string totalBg = "#C6EFCE", string totalBorder = "#1A2B4A",
        int fontSize = 8, int pad = 3)
    {
        // Portrait A4 útil ~549pt (595 - 8mm×2 márgenes)
        // Columnas fijas = 450pt → RelativeColumn (Cliente) recibe ~99pt
        table.ColumnsDefinition(cols =>
        {
            cols.ConstantColumn(74);  // N° Comprobante
            cols.ConstantColumn(40);  // Tipo
            cols.ConstantColumn(46);  // F. Emisión
            cols.RelativeColumn();    // Cliente
            cols.ConstantColumn(48);  // Doc. Cliente
            cols.ConstantColumn(44);  // Val. Venta
            cols.ConstantColumn(38);  // IGV
            cols.ConstantColumn(50);  // Importe Total
            cols.ConstantColumn(20);  // Mon
            cols.ConstantColumn(50);  // Estado
            cols.ConstantColumn(40);  // T.Pago
        });

        table.Header(h =>
        {
            h.Cell().Element(c => TH(c, "N° Comprobante", fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Tipo",           fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "F. Emisión",     fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Cliente",        fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Doc. Cliente",   fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Val. Venta",     right: true, fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "IGV",            right: true, fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Imp. Total",     right: true, fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Mon",            fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "Estado",         fontSize: fontSize, pad: pad));
            h.Cell().Element(c => TH(c, "T.Pago",         fontSize: fontSize, pad: pad));
        });

        bool par = false;
        foreach (var d in lista)
        {
            bool esRechazado = d.EstadoSunat == "RECHAZADO";
            var bg = esRechazado ? "#F2F2F2" : (par ? Blanco : GrisClaro); par = !par;
            bool esNC = d.TipoComprobante == "07";
            var tipo = d.TipoComprobante switch
            {
                "01" => "Factura", "03" => "Boleta",
                "07" => "N.Cred",  "08" => "N.Deb", _ => d.TipoComprobante
            };
            var vv  = esNC ? -d.ValorVenta   : d.ValorVenta;
            var igv = esNC ? -d.TotalIGV     : d.TotalIGV;
            var tot = esNC ? -d.ImporteTotal  : d.ImporteTotal;

            table.Cell().Element(c => TD(c, d.NumeroCompleto ?? "-",               bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, tipo ?? "",                             bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.FechaEmision.ToString("dd/MM/yyyy"), bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.Cliente?.RazonSocial ?? "-",         bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.Cliente?.NumeroDocumento ?? "-",     bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, $"{(vv  < 0 ? "-" : "")}{Math.Abs(vv):N2}",  bg, right: true, red: esNC, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, $"{(igv < 0 ? "-" : "")}{Math.Abs(igv):N2}", bg, right: true, red: esNC, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, $"{(tot < 0 ? "-" : "")}{Math.Abs(tot):N2}", bg, right: true, red: esNC, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.TipoMoneda ?? "PEN",                bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.EstadoSunat ?? "-",                 bg, fontSize: fontSize, pad: pad));
            table.Cell().Element(c => TD(c, d.TipoPago ?? "-",                    bg, fontSize: fontSize, pad: pad));
        }

        // ── Fila TOTAL ───────────────────────────────────────────────────────
        var tvv  = lista.Sum(x => x.TipoComprobante == "07" ? -x.ValorVenta   : x.ValorVenta);
        var tigv = lista.Sum(x => x.TipoComprobante == "07" ? -x.TotalIGV     : x.TotalIGV);
        var timp = lista.Sum(x => x.TipoComprobante == "07" ? -x.ImporteTotal : x.ImporteTotal);

        void TotalCell(IContainer c, string txt, bool right = false) =>
            c.Background(totalBg).BorderTop(1.5f).BorderColor(totalBorder).Padding(pad)
             .Column(col =>
             {
                 if (right) col.Item().AlignRight().Text(txt).Bold().FontSize(fontSize);
                 else       col.Item().Text(txt).Bold().FontSize(fontSize);
             });

        table.Cell().ColumnSpan(5).Element(c => TotalCell(c, totalLabel));
        table.Cell().Element(c => TotalCell(c, $"{tvv:N2}",  right: true));
        table.Cell().Element(c => TotalCell(c, $"{tigv:N2}", right: true));
        table.Cell().Element(c => TotalCell(c, $"{timp:N2}", right: true));
        table.Cell().ColumnSpan(3).Element(c => TotalCell(c, ""));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // REPORTE DE CAJA — PDF A4 (mismo estilo que comprobante)
    // ═════════════════════════════════════════════════════════════════════════

    public Task<byte[]> ExportarControlCajaTicketPdfAsync(
        string titulo,
        IEnumerable<ControlCajaTicketItemDto> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string nombreResponsable = "",
        string? empresaNombre = null,
        string? empresaDireccion = null,
        string? logoBase64 = null,
        string? nombreUsuario = null)
    {
        // alias locales idénticos al ComprobantePdfService
        const string ColorAzul   = "#1A2B4A";
        const string ColorBlanco = "#FFFFFF";
        const string ColorGris   = "#F5F7FA";
        const string ColorBorde  = "#D0D7E3";
        const string ColorSuave  = "#4A5568";
        const string ColorRojo   = "#C00000";
        const string ColorVerde  = "#375623";
        var movimientos  = datos.ToList();
        var fechaReporte = DateTime.Now;

        // ── Totales ──────────────────────────────────────────────────────────
        var totalPen = movimientos.Where(x => x.TipoMoneda == "PEN")
            .Sum(x => x.TipoComprobante == "07" ? -x.ImporteTotal : x.ImporteTotal);
        var totalUsd = movimientos.Where(x => x.TipoMoneda == "USD")
            .Sum(x => x.TipoComprobante == "07" ? -x.ImporteTotal : x.ImporteTotal);

        // ── Resumen por medio de pago ────────────────────────────────────────
        var resumenPago = movimientos
            .SelectMany(c => c.Pagos.Select(p => new
            {
                Medio  = CajaNormalizarMedio(p.MedioPago),
                c.TipoMoneda,
                p.Monto
            }))
            .GroupBy(x => (x.Medio, x.TipoMoneda))
            .Select(g => (Medio: g.Key.Medio, Moneda: g.Key.TipoMoneda, Total: g.Sum(x => x.Monto)))
            .Where(x => x.Total > 0)
            .OrderBy(x => x.Moneda).ThenBy(x => x.Medio)
            .ToList();

        // ── Logo ─────────────────────────────────────────────────────────────
        byte[]? logoBytes = null;
        if (!string.IsNullOrEmpty(logoBase64))
        {
            var b64 = Regex.IsMatch(logoBase64, @"^data:.+;base64,")
                ? logoBase64.Split(',')[1] : logoBase64;
            try { logoBytes = Convert.FromBase64String(b64); } catch { }
        }

        // ── Período texto ─────────────────────────────────────────────────────
        string periodoStr = fechaDesde.HasValue
            ? (fechaHasta.HasValue && fechaHasta.Value.Date != fechaDesde.Value.Date
                ? $"{fechaDesde:dd/MM/yyyy} al {fechaHasta:dd/MM/yyyy}"
                : fechaDesde.Value.ToString("dd/MM/yyyy"))
            : "Todos los períodos";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(8,     Unit.Millimetre);
                page.MarginBottom(10, Unit.Millimetre);
                page.MarginLeft(12,   Unit.Millimetre);
                page.MarginRight(12,  Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily(Fonts.Lato).FontSize(9).FontColor("#1A1A1A"));

                // ╔══════════════════════════════════════════════════════╗
                // ║  HEADER — banda completa ancho total                  ║
                // ╚══════════════════════════════════════════════════════╝
                page.Header().Column(hdr =>
                {
                    // Banda azul oscuro de extremo a extremo
                    hdr.Item().Background(ColorAzul).Padding(12).Row(row =>
                    {
                        // Logo
                        if (logoBytes is not null)
                            row.ConstantItem(60).AlignMiddle()
                               .MaxHeight(36).Image(logoBytes);
                        else
                            row.ConstantItem(4);

                        // Empresa
                        row.RelativeItem().PaddingLeft(10).AlignMiddle().Column(emp =>
                        {
                            emp.Item().Text(empresaNombre ?? ruc)
                               .Bold().FontSize(13).FontColor(ColorBlanco);
                            emp.Item().Text($"R.U.C. {ruc}")
                               .FontSize(8).FontColor(ColorBorde);
                            if (!string.IsNullOrEmpty(empresaDireccion))
                                emp.Item().Text(empresaDireccion)
                                   .FontSize(7).FontColor(ColorBorde);
                        });

                        // Título del reporte alineado a la derecha
                        row.ConstantItem(170).AlignMiddle().AlignRight().Column(tit =>
                        {
                            tit.Item().AlignRight()
                               .Text(titulo.ToUpper())
                               .Bold().FontSize(14).FontColor(ColorBlanco);
                            tit.Item().PaddingTop(3).AlignRight()
                               .Text(fechaReporte.ToString("dd/MM/yyyy  HH:mm"))
                               .FontSize(8).FontColor(ColorBorde);
                        });
                    });

                    // Barra gris con los filtros — ancho total
                    hdr.Item().Background(ColorGris)
                       .BorderBottom(1).BorderColor(ColorBorde)
                       .PaddingHorizontal(12).PaddingVertical(5)
                       .Row(chips =>
                       {
                           void Chip(RowDescriptor r, string label, string val)
                           {
                               r.AutoItem().PaddingRight(14).Row(inner =>
                               {
                                   inner.AutoItem()
                                        .Text($"{label}: ").Bold().FontSize(7).FontColor(ColorAzul);
                                   inner.AutoItem()
                                        .Text(val).FontSize(7).FontColor("#1A1A1A");
                               });
                           }

                           Chip(chips, "Período",      periodoStr);
                           if (!string.IsNullOrEmpty(codEstablecimiento))
                               Chip(chips, "Estab.",       codEstablecimiento);
                           if (!string.IsNullOrWhiteSpace(nombreResponsable))
                               Chip(chips, "Responsable",  nombreResponsable);
                           if (!string.IsNullOrWhiteSpace(nombreUsuario))
                               Chip(chips, "Cajero",       nombreUsuario);
                           Chip(chips, "Registros",   movimientos.Count.ToString());
                       });

                    hdr.Item().Height(8);
                });

                // ╔══════════════════════════════════════════════════════╗
                // ║  CONTENT                                              ║
                // ╚══════════════════════════════════════════════════════╝
                page.Content().Column(col =>
                {
                    // ── Tabla principal ancho completo ────────────────────
                    col.Item().Element(c => BuildCajaTabla(c, movimientos, ColorAzul, ColorBorde, ColorGris, ColorRojo));

                    // Leyenda NC / ND
                    col.Item().PaddingTop(4).Row(r =>
                    {
                        r.AutoItem().PaddingRight(10)
                         .Text("■ N. Crédito: valor en rojo, resta al total")
                         .Italic().FontSize(7).FontColor(ColorRojo);
                        r.AutoItem()
                         .Text("■ N. Débito: suma al total")
                         .Italic().FontSize(7).FontColor(ColorVerde);
                    });

                    col.Item().PaddingTop(12);

                    // ── Sección inferior: dos columnas ────────────────────
                    col.Item().Row(row =>
                    {
                        // ── Columna izq: resumen medios de pago ──────────
                        row.RelativeItem(5).Column(left =>
                        {
                            // Título sección
                            left.Item()
                                .BorderBottom(2).BorderColor(ColorAzul)
                                .PaddingBottom(3)
                                .Text("Resumen por Medio de Pago")
                                .Bold().FontSize(9).FontColor(ColorAzul);

                            left.Item().PaddingTop(4).Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();    // Medio
                                    c.ConstantColumn(45);  // Moneda
                                    c.ConstantColumn(75);  // Total
                                });

                                // Header
                                void RH(IContainer c, string txt, bool right = false) =>
                                    c.Background(ColorAzul).Padding(4)
                                     .Column(col2 =>
                                     {
                                         if (right) col2.Item().AlignRight().Text(txt).Bold().FontSize(8).FontColor(ColorBlanco);
                                         else       col2.Item().Text(txt).Bold().FontSize(8).FontColor(ColorBlanco);
                                     });

                                t.Header(h =>
                                {
                                    h.Cell().Element(c => RH(c, "Medio de Pago"));
                                    h.Cell().Element(c => RH(c, "Moneda"));
                                    h.Cell().Element(c => RH(c, "Total", right: true));
                                });

                                bool par2 = false;
                                foreach (var mp in resumenPago)
                                {
                                    var bg2     = par2 ? ColorBlanco : ColorGris; par2 = !par2;
                                    var sim     = mp.Moneda == "USD" ? "$" : "S/";

                                    void RC(IContainer c, string txt, bool right = false) =>
                                        c.Background(bg2).BorderBottom(1).BorderColor(ColorBorde).Padding(3)
                                         .Column(col2 =>
                                         {
                                             if (right) col2.Item().AlignRight().Text(txt).FontSize(8);
                                             else       col2.Item().Text(txt).FontSize(8);
                                         });

                                    t.Cell().Element(c => RC(c, mp.Medio));
                                    t.Cell().Element(c => RC(c, mp.Moneda));
                                    t.Cell().Element(c => RC(c, $"{sim} {mp.Total:N2}", right: true));
                                }

                                if (!resumenPago.Any())
                                {
                                    t.Cell().ColumnSpan(3)
                                     .Background(ColorGris).Padding(6)
                                     .Text("Sin movimientos en el período")
                                     .Italic().FontSize(8).FontColor(ColorSuave);
                                }
                            });

                        });

                        row.ConstantItem(12);

                        // ── Columna der: totales ─────────────────────────
                        row.RelativeItem(4).Column(right =>
                        {
                            right.Item()
                                 .BorderBottom(2).BorderColor(ColorAzul)
                                 .PaddingBottom(3)
                                 .Text("Resumen General")
                                 .Bold().FontSize(9).FontColor(ColorAzul);

                            right.Item().PaddingTop(4)
                                 .Border(1).BorderColor(ColorBorde).Column(t =>
                                 {
                                     void FilaTot(string label, string val, bool bold = false) =>
                                         t.Item().Background(bold ? ColorBlanco : ColorGris)
                                          .BorderBottom(1).BorderColor(ColorBorde)
                                          .Padding(4).Row(r =>
                                          {
                                              r.RelativeItem().Text(label).FontSize(8).FontColor(bold ? ColorAzul : "#1A1A1A");
                                              r.ConstantItem(80).AlignRight().Text(val).FontSize(8).FontColor(bold ? ColorAzul : "#1A1A1A");
                                          });

                                     var countNV = movimientos.Count(x => x.TipoComprobante == "NV");
                                     FilaTot("Total comprobantes",   movimientos.Count.ToString());
                                     FilaTot("  Facturas",           movimientos.Count(x => x.TipoComprobante == "01").ToString());
                                     FilaTot("  Boletas",            movimientos.Count(x => x.TipoComprobante == "03").ToString());
                                     FilaTot("  Notas de crédito",   movimientos.Count(x => x.TipoComprobante == "07").ToString());
                                     FilaTot("  Notas de débito",    movimientos.Count(x => x.TipoComprobante == "08").ToString());
                                     if (countNV > 0)
                                         FilaTot("  Notas de venta", countNV.ToString());

                                     // Fila total PEN
                                     if (totalPen != 0)
                                         t.Item().Background(ColorAzul).Padding(5).Row(r =>
                                         {
                                             r.RelativeItem()
                                              .Text("TOTAL NETO (PEN)")
                                              .Bold().FontSize(10).FontColor(ColorBlanco);
                                             r.ConstantItem(90).AlignRight()
                                              .Text($"S/ {totalPen:N2}")
                                              .Bold().FontSize(10).FontColor(ColorBlanco);
                                         });

                                     // Fila total USD
                                     if (totalUsd != 0)
                                         t.Item().Background(ColorAzul).Padding(5).Row(r =>
                                         {
                                             r.RelativeItem()
                                              .Text("TOTAL NETO (USD)")
                                              .Bold().FontSize(10).FontColor(ColorBlanco);
                                             r.ConstantItem(90).AlignRight()
                                              .Text($"$ {totalUsd:N2}")
                                              .Bold().FontSize(10).FontColor(ColorBlanco);
                                         });

                                     // Nota de inclusión de NV
                                     if (countNV > 0)
                                         t.Item().Background("#FEF3C7").BorderBottom(1).BorderColor("#F59E0B")
                                          .Padding(4).Text(txt =>
                                          {
                                              txt.Span("★ ").FontSize(7).FontColor("#D97706");
                                              txt.Span("Incluye notas de venta")
                                                 .FontSize(7).FontColor("#92400E").Italic();
                                          });
                                 });
                        });
                    });
                });

                // ╔══════════════════════════════════════════════════════╗
                // ║  FOOTER                                               ║
                // ╚══════════════════════════════════════════════════════╝
                page.Footer()
                    .BorderTop(1).BorderColor(ColorAzul)
                    .PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem()
                         .Text($"Generado el {fechaReporte:dd/MM/yyyy} a las {fechaReporte:HH:mm:ss}")
                         .Italic().FontSize(7).FontColor(ColorSuave);
                        r.RelativeItem().AlignCenter()
                         .Text("Reporte de Control de Caja")
                         .FontSize(7).FontColor(ColorSuave);
                        r.RelativeItem().AlignRight().Text(txt =>
                        {
                            txt.Span("Pág. ").FontSize(7).FontColor(ColorSuave);
                            txt.CurrentPageNumber().FontSize(7).FontColor(ColorSuave);
                            txt.Span(" de ").FontSize(7).FontColor(ColorSuave);
                            txt.TotalPages().FontSize(7).FontColor(ColorSuave);
                        });
                    });
            });
        });

        return Task.FromResult(ToBytes(doc));
    }

    // ── Tabla comprobantes de caja (igual a BuildTablaDetalles del comprobante) ──
    private static void BuildCajaTabla(IContainer container, List<ControlCajaTicketItemDto> lista,
        string colorHeader, string colorBorde, string colorGris, string colorRojo)
    {
        // Ancho útil A4 con márgenes 12mm c/u ≈ 527pt
        // Fijos: 18+90+44+62+64+30 = 308 → RelativeColumn ≈ 219pt para Medios de Pago
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(18);  // #
                cols.ConstantColumn(90);  // N° Comprobante  "B003-00021834"
                cols.ConstantColumn(44);  // Tipo            "N.Crédito"
                cols.ConstantColumn(62);  // F. Emisión      "01/06/2026" sin cortar
                cols.ConstantColumn(64);  // Importe         "S/ 1,550.00"
                cols.ConstantColumn(30);  // Mon.            "PEN"
                cols.RelativeColumn();    // Medios de Pago
            });

            void TH(IContainer c, string txt, bool right = false) =>
                c.Background(colorHeader).PaddingVertical(5).PaddingHorizontal(4)
                 .Column(col2 =>
                 {
                     if (right) col2.Item().AlignRight().Text(txt).Bold().FontSize(8).FontColor(Blanco);
                     else       col2.Item().Text(txt).Bold().FontSize(8).FontColor(Blanco);
                 });

            table.Header(h =>
            {
                h.Cell().Element(c => TH(c, "#"));
                h.Cell().Element(c => TH(c, "N° Comprobante"));
                h.Cell().Element(c => TH(c, "Tipo"));
                h.Cell().Element(c => TH(c, "F. Emisión"));
                h.Cell().Element(c => TH(c, "Importe", right: true));
                h.Cell().Element(c => TH(c, "Mon."));
                h.Cell().Element(c => TH(c, "Medios de Pago"));
            });

            bool par = false; int n = 1;
            foreach (var d in lista)
            {
                bool esRechazado = d.EstadoSunat == "RECHAZADO";
                var bg    = esRechazado ? "#F2F2F2" : (par ? Blanco : colorGris); par = !par;
                bool esNC = d.TipoComprobante == "07";
                decimal monto = esNC ? -d.ImporteTotal : d.ImporteTotal;

                var tipo = d.TipoComprobante switch
                {
                    "01" => "Factura",    "03" => "Boleta",
                    "07" => "N.Crédito",  "08" => "N.Débito",
                    _    => d.TipoComprobante ?? "-"
                };
                var medios = d.Pagos.Any()
                    ? string.Join("  |  ", d.Pagos.Select(p =>
                        $"{CajaNormalizarMedio(p.MedioPago)} {(d.TipoMoneda == "USD" ? "$" : "S/")} {p.Monto:N2}"))
                    : "-";

                void TD(IContainer c, string txt, bool right = false, bool red = false)
                {
                    var cell = c.Background(bg).BorderBottom(1).BorderColor(colorBorde)
                                .PaddingVertical(4).PaddingHorizontal(4);
                    string color = red ? colorRojo : "#1A1A1A";
                    if (right) cell.AlignRight().Text(txt).FontSize(8).FontColor(color);
                    else       cell.Text(txt).FontSize(8).FontColor(color);
                }

                table.Cell().Element(c => TD(c, (n++).ToString()));
                table.Cell().Element(c => TD(c, d.NumeroCompleto));
                table.Cell().Element(c => TD(c, tipo, red: esNC));
                table.Cell().Element(c => TD(c, d.FechaEmision.ToString("dd/MM/yyyy")));
                table.Cell().Element(c => TD(c,
                    d.TipoMoneda == "USD" ? $"$ {monto:N2}" : $"S/ {monto:N2}",
                    right: true, red: esNC));
                table.Cell().Element(c => TD(c, d.TipoMoneda ?? "PEN"));
                table.Cell().Element(c => TD(c, medios));
            }
        });
    }

    private static string CajaNormalizarMedio(string? medio)
    {
        if (string.IsNullOrWhiteSpace(medio)) return "Efectivo";
        return medio.Trim().ToLower() switch
        {
            "efectivo"                                              => "Efectivo",
            "yape"                                                  => "Yape",
            "plin"                                                  => "Plin",
            "transferencia" or "transferencia bancaria"             => "Transferencia",
            "tarjeta" or "tarjeta de crédito" or "tarjeta de débito"
                or "tarjeta de debito"                              => "Tarjeta",
            "depósito" or "deposito" or "depósito bancario"
                or "deposito bancario"                              => "Depósito",
            "pos"                                                   => "POS",
            "cheque"                                                => "Cheque",
            var s => char.ToUpper(s[0]) + s[1..]
        };
    }

    // ═════════════════════════════════════════════════════════════════════════
    // TOP PRODUCTOS
    // ═════════════════════════════════════════════════════════════════════════
    public Task<byte[]> ExportarProductosTopPdfAsync(
        string titulo, IEnumerable<ProductoTopDTO> datos,
        string ruc, string? codEstablecimiento = null,
        DateTime? fechaDesde = null, DateTime? fechaHasta = null,
        int? usuarioCreacion = null, string? clienteNumDoc = null)
    {
        var lista = datos.ToList();
        var filtros = Filtros(ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(12, Unit.Millimetre);
                page.MarginVertical(12, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily(Fonts.Lato).FontSize(8).FontColor("#1A1A1A"));

                page.Header().Element(h => BuildCabecera(h, titulo, filtros));

                page.Content().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(22);  // #
                        cols.ConstantColumn(60);  // Código
                        cols.RelativeColumn();    // Descripción
                        cols.ConstantColumn(55);  // Cant.
                        cols.ConstantColumn(65);  // Total Monto
                        cols.ConstantColumn(55);  // IGV
                        cols.ConstantColumn(65);  // Precio Prom.
                        cols.ConstantColumn(50);  // # Ventas
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(c => TH(c, "#"));
                        h.Cell().Element(c => TH(c, "Código"));
                        h.Cell().Element(c => TH(c, "Descripción"));
                        h.Cell().Element(c => TH(c, "Cant.", right: true));
                        h.Cell().Element(c => TH(c, "Total", right: true));
                        h.Cell().Element(c => TH(c, "IGV", right: true));
                        h.Cell().Element(c => TH(c, "Precio Prom.", right: true));
                        h.Cell().Element(c => TH(c, "# Ventas", right: true));
                    });

                    bool par = false; int n = 1;
                    foreach (var d in lista)
                    {
                        var bg = par ? Blanco : GrisClaro; par = !par;
                        table.Cell().Element(c => TD(c, (n++).ToString(), bg));
                        table.Cell().Element(c => TD(c, d.Codigo ?? "-", bg));
                        table.Cell().Element(c => TD(c, d.Descripcion ?? "-", bg));
                        table.Cell().Element(c => TD(c, d.TotalCantidad.ToString("N2"), bg, right: true));
                        table.Cell().Element(c => TD(c, Fmt(d.TotalMonto), bg, right: true));
                        table.Cell().Element(c => TD(c, Fmt(d.TotalIGV), bg, right: true));
                        table.Cell().Element(c => TD(c, Fmt(d.PrecioPromedio), bg, right: true));
                        table.Cell().Element(c => TD(c, d.VecesVendido.ToString(), bg, right: true));
                    }
                });

                page.Footer().AlignRight().Text(txt =>
                {
                    txt.Span("Pág. ").FontSize(7).FontColor("#4A5568");
                    txt.CurrentPageNumber().FontSize(7).FontColor("#4A5568");
                    txt.Span($"  |  {lista.Count} productos").FontSize(7).FontColor("#4A5568");
                });
            });
        });

        return Task.FromResult(ToBytes(doc));
    }

    // ═════════════════════════════════════════════════════════════════════════
    // MEDIOS DE PAGO
    // ═════════════════════════════════════════════════════════════════════════
    public Task<byte[]> ExportarMediosPagoPdfAsync(
        string titulo, IEnumerable<MedioPagoTopDTO> datos,
        string ruc, string? codEstablecimiento = null,
        DateTime? fechaDesde = null, DateTime? fechaHasta = null,
        int? usuarioCreacion = null, string? clienteNumDoc = null)
    {
        var lista = datos.ToList();
        var filtros = Filtros(ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(12, Unit.Millimetre);
                page.MarginVertical(12, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily(Fonts.Lato).FontSize(8).FontColor("#1A1A1A"));

                page.Header().Element(h => BuildCabecera(h, titulo, filtros));

                page.Content().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(25);  // #
                        cols.RelativeColumn();    // Medio de Pago
                        cols.ConstantColumn(70);  // Veces Usado
                        cols.ConstantColumn(80);  // Monto Total
                        cols.ConstantColumn(80);  // Promedio
                    });

                    table.Header(h =>
                    {
                        h.Cell().Element(c => TH(c, "#"));
                        h.Cell().Element(c => TH(c, "Medio de Pago"));
                        h.Cell().Element(c => TH(c, "Veces Usado", right: true));
                        h.Cell().Element(c => TH(c, "Monto Total", right: true));
                        h.Cell().Element(c => TH(c, "Promedio", right: true));
                    });

                    bool par = false; int n = 1;
                    foreach (var d in lista)
                    {
                        var bg = par ? Blanco : GrisClaro; par = !par;
                        table.Cell().Element(c => TD(c, (n++).ToString(), bg));
                        table.Cell().Element(c => TD(c, d.MedioPago ?? "-", bg));
                        table.Cell().Element(c => TD(c, d.VecesUsado.ToString(), bg, right: true));
                        table.Cell().Element(c => TD(c, Fmt(d.MontoTotal), bg, right: true));
                        table.Cell().Element(c => TD(c, Fmt(d.PromedioMonto), bg, right: true));
                    }
                });

                // Total row
                var total = lista.Sum(x => x.MontoTotal);
                page.Content().Column(col =>
                    col.Item().Background(VerdeFila).Border(1).BorderColor(Azul).Padding(4).Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL").Bold().FontSize(9);
                        r.ConstantItem(80).AlignRight().Text(Fmt(total)).Bold().FontSize(9);
                    }));

                page.Footer().AlignRight().Text(txt =>
                {
                    txt.Span("Pág. ").FontSize(7).FontColor("#4A5568");
                    txt.CurrentPageNumber().FontSize(7).FontColor("#4A5568");
                });
            });
        });

        return Task.FromResult(ToBytes(doc));
    }
}
