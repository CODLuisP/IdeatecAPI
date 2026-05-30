using IdeatecAPI.Application.Common.Interfaces.Persistence.Reportes;
using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Reportes.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IdeatecAPI.Infrastructure.Services;

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

    private static void TH(IContainer c, string txt, bool right = false)
    {
        var el = c.Background(AzulCab).Padding(4);
        if (right) el.AlignRight().Text(txt).Bold().FontSize(8).FontColor(Blanco);
        else       el.Text(txt).Bold().FontSize(8).FontColor(Blanco);
    }

    private static void TD(IContainer c, string txt, string bg, bool right = false, bool red = false)
    {
        var el = c.Background(bg).BorderBottom(1).BorderColor(GrisBorde).Padding(3);
        var t = right ? el.AlignRight().Text(txt).FontSize(8)
                      : el.Text(txt).FontSize(8);
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
                // Landscape A4 (842 × 595)
                page.Size(PageSizes.A4.Height, PageSizes.A4.Width);
                page.MarginHorizontal(12, Unit.Millimetre);
                page.MarginVertical(10, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8).FontColor("#1A1A1A"));

                page.Header().Element(h => BuildCabecera(h, titulo, filtros));

                page.Content().PaddingTop(4).Column(col =>
                {
                    // ── VENTAS DEL PERÍODO ────────────────────────────────────
                    col.Item().Background(AzulCab).Padding(4)
                        .Text("VENTAS DEL PERÍODO").Bold().FontSize(9).FontColor(Blanco);
                    col.Item().Table(table => BuildTablaListado(table, ventas,
                        "TOTAL NETO DEL PERÍODO", "#BDD7EE", Azul));

                    // Leyenda N. Crédito / N. Débito (igual que el Excel)
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Background("#FFF2CC").Padding(3)
                            .Text("N. Crédito (resta al total)").Italic().FontSize(7).FontColor("#C00000");
                        r.RelativeItem().Background("#E2EFDA").Padding(3).AlignRight()
                            .Text("N. Débito (suma al total)").Italic().FontSize(7).FontColor("#375623");
                    });

                    // ── AJUSTES DE OTROS PERÍODOS ─────────────────────────────
                    if (ajustes.Any())
                    {
                        col.Item().PaddingTop(8).Background("#7030A0").Padding(4)
                            .Text("AJUSTES DE OTROS PERÍODOS (no afectan el total anterior)")
                            .Bold().FontSize(9).FontColor(Blanco);
                        col.Item().Table(table => BuildTablaListado(table, ajustes,
                            "TOTAL AJUSTES", "#E2CFEE", "#7030A0"));
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
        var lista = datos.ToList();
        var filtros = Filtros(ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);

        // Separar movimientos del período y ajustes (misma lógica que el Excel)
        var idsDelPeriodo = lista.Select(x => x.ComprobanteId).ToHashSet();
        bool EsNota(ListarComprobanteDTO x) => x.TipoComprobante == "07" || x.TipoComprobante == "08";
        bool AfectaOtro(ListarComprobanteDTO x) =>
            EsNota(x) && x.ComprobanteAfectadoId.HasValue && !idsDelPeriodo.Contains(x.ComprobanteAfectadoId.Value);

        var movimientos = lista.Where(x => !AfectaOtro(x)).ToList();
        var ajustes     = lista.Where(AfectaOtro).ToList();

        var totalMov = movimientos.Where(x => x.TipoComprobante != "07").Sum(x => x.ImporteTotal)
                     - movimientos.Where(x => x.TipoComprobante == "07").Sum(x => x.ImporteTotal);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Landscape A4 (842 × 595) — igual que el Excel que es ancho
                page.Size(PageSizes.A4.Height, PageSizes.A4.Width);
                page.MarginHorizontal(12, Unit.Millimetre);
                page.MarginVertical(10, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8).FontColor("#1A1A1A"));

                page.Header().Element(h => BuildCabecera(h, titulo, filtros));

                page.Content().PaddingTop(4).Column(col =>
                {
                    // ── Movimientos (total incluido dentro de la tabla) ───────
                    col.Item().Background(Azul).Padding(4)
                        .Text("MOVIMIENTOS DEL PERÍODO").Bold().FontSize(9).FontColor(Blanco);
                    col.Item().Table(table => BuildTablaCC(table, movimientos,
                        "TOTAL NETO DEL PERÍODO", "#C6EFCE", Azul));

                    // ── Ajustes ───────────────────────────────────────────────
                    if (ajustes.Any())
                    {
                        col.Item().PaddingTop(8).Background("#7030A0").Padding(4)
                            .Text("AJUSTES DE OTROS PERÍODOS (no afectan el total anterior)")
                            .Bold().FontSize(9).FontColor(Blanco);
                        col.Item().Table(table => BuildTablaCC(table, ajustes,
                            "TOTAL AJUSTES", "#E2CFEE", "#7030A0"));
                        col.Item().PaddingTop(3).Text(
                            "Estas notas afectan comprobantes de otros períodos y no se incluyen en el total.")
                            .Italic().FontSize(8).FontColor("#7030A0");
                    }
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
        string totalLabel = "TOTAL NETO DEL PERÍODO", string totalBg = "#BDD7EE", string totalBorder = "#1A2B4A")
    {
        table.ColumnsDefinition(cols =>
        {
            cols.ConstantColumn(18);   // #
            cols.ConstantColumn(88);   // N° Comprobante
            cols.ConstantColumn(50);   // Tipo
            cols.ConstantColumn(58);   // F. Emisión
            cols.RelativeColumn();     // Cliente
            cols.ConstantColumn(65);   // Doc. Cliente
            cols.ConstantColumn(58);   // Val. Venta
            cols.ConstantColumn(50);   // IGV
            cols.ConstantColumn(62);   // Total
            cols.ConstantColumn(28);   // Mon
            cols.ConstantColumn(58);   // Estado SUNAT
            cols.ConstantColumn(50);   // T.Pago
        });

        table.Header(h =>
        {
            h.Cell().Element(c => TH(c, "#"));
            h.Cell().Element(c => TH(c, "N° Comprobante"));
            h.Cell().Element(c => TH(c, "Tipo"));
            h.Cell().Element(c => TH(c, "F. Emisión"));
            h.Cell().Element(c => TH(c, "Cliente"));
            h.Cell().Element(c => TH(c, "Doc. Cliente"));
            h.Cell().Element(c => TH(c, "Val. Venta", right: true));
            h.Cell().Element(c => TH(c, "IGV", right: true));
            h.Cell().Element(c => TH(c, "Total", right: true));
            h.Cell().Element(c => TH(c, "Mon"));
            h.Cell().Element(c => TH(c, "Estado SUNAT"));
            h.Cell().Element(c => TH(c, "T.Pago"));
        });

        bool par = false; int n = 1;
        foreach (var d in lista)
        {
            var bg = par ? Blanco : GrisClaro; par = !par;
            bool esNC = d.TipoComprobante == "07";
            var tipo = d.TipoComprobante switch
            {
                "01" => "Factura", "03" => "Boleta",
                "07" => "N.Crédito", "08" => "N.Débito", _ => d.TipoComprobante
            };
            var vv  = esNC ? -d.ValorVenta   : d.ValorVenta;
            var igv = esNC ? -d.TotalIGV     : d.TotalIGV;
            var tot = esNC ? -d.ImporteTotal : d.ImporteTotal;

            table.Cell().Element(c => TD(c, (n++).ToString(), bg));
            table.Cell().Element(c => TD(c, d.NumeroCompleto, bg));
            table.Cell().Element(c => TD(c, tipo ?? "", bg));
            table.Cell().Element(c => TD(c, d.FechaEmision.ToString("dd/MM/yyyy"), bg));
            table.Cell().Element(c => TD(c, d.Cliente?.RazonSocial ?? "-", bg));
            table.Cell().Element(c => TD(c, d.Cliente?.NumeroDocumento ?? "-", bg));
            table.Cell().Element(c => TD(c, FmtM(vv,  d.TipoMoneda), bg, right: true, red: esNC));
            table.Cell().Element(c => TD(c, FmtM(igv, d.TipoMoneda), bg, right: true, red: esNC));
            table.Cell().Element(c => TD(c, FmtM(tot, d.TipoMoneda), bg, right: true, red: esNC));
            table.Cell().Element(c => TD(c, d.TipoMoneda ?? "PEN", bg));
            table.Cell().Element(c => TD(c, d.EstadoSunat ?? "-", bg));
            table.Cell().Element(c => TD(c, d.TipoPago ?? "-", bg));
        }

        // ── Fila TOTAL (ColumnSpan 6 + 3 numéricos + ColumnSpan 3) ────────────
        var tvv  = lista.Sum(x => x.TipoComprobante == "07" ? -x.ValorVenta   : x.ValorVenta);
        var tigv = lista.Sum(x => x.TipoComprobante == "07" ? -x.TotalIGV     : x.TotalIGV);
        var timp = lista.Sum(x => x.TipoComprobante == "07" ? -x.ImporteTotal : x.ImporteTotal);

        void TF(IContainer c, string txt, bool right = false) =>
            c.Background(totalBg).BorderTop(1.5f).BorderColor(totalBorder).Padding(3)
             .Column(col2 =>
             {
                 if (right) col2.Item().AlignRight().Text(txt).Bold().FontSize(8);
                 else       col2.Item().Text(txt).Bold().FontSize(8);
             });

        table.Cell().ColumnSpan(6).Element(c => TF(c, totalLabel));
        table.Cell().Element(c => TF(c, $"{tvv:N2}",  right: true));
        table.Cell().Element(c => TF(c, $"{tigv:N2}", right: true));
        table.Cell().Element(c => TF(c, $"{timp:N2}", right: true));
        table.Cell().ColumnSpan(3).Element(c => TF(c, ""));
    }

    private static void BuildTablaCC(TableDescriptor table, List<ListarComprobanteDTO> lista,
        string totalLabel = "TOTAL NETO DEL PERÍODO", string totalBg = "#C6EFCE", string totalBorder = "#1A2B4A")
    {
        // Landscape A4 útil ~774pt — mismas columnas que el Excel
        table.ColumnsDefinition(cols =>
        {
            cols.ConstantColumn(88);  // N° Comprobante
            cols.ConstantColumn(50);  // Tipo
            cols.ConstantColumn(58);  // Fecha
            cols.RelativeColumn();    // Cliente  ← recibe todo el espacio sobrante
            cols.ConstantColumn(65);  // Doc. Cliente
            cols.ConstantColumn(58);  // Val. Venta
            cols.ConstantColumn(50);  // IGV
            cols.ConstantColumn(65);  // Importe Total
            cols.ConstantColumn(30);  // Moneda
            cols.ConstantColumn(65);  // Estado SUNAT
            cols.ConstantColumn(52);  // T.Pago
        });

        table.Header(h =>
        {
            h.Cell().Element(c => TH(c, "N° Comprobante"));
            h.Cell().Element(c => TH(c, "Tipo"));
            h.Cell().Element(c => TH(c, "F. Emisión"));
            h.Cell().Element(c => TH(c, "Cliente"));
            h.Cell().Element(c => TH(c, "Doc. Cliente"));
            h.Cell().Element(c => TH(c, "Val. Venta", right: true));
            h.Cell().Element(c => TH(c, "IGV", right: true));
            h.Cell().Element(c => TH(c, "Importe Total", right: true));
            h.Cell().Element(c => TH(c, "Moneda"));
            h.Cell().Element(c => TH(c, "Estado SUNAT"));
            h.Cell().Element(c => TH(c, "T.Pago"));
        });

        bool par = false; int n = 1;
        foreach (var d in lista)
        {
            var bg = par ? Blanco : GrisClaro; par = !par;
            bool esNC = d.TipoComprobante == "07";
            var tipo = d.TipoComprobante switch
            {
                "01" => "Factura", "03" => "Boleta",
                "07" => "N.Crédito", "08" => "N.Débito", _ => d.TipoComprobante
            };
            var vv  = esNC ? -d.ValorVenta  : d.ValorVenta;
            var igv = esNC ? -d.TotalIGV    : d.TotalIGV;
            var tot = esNC ? -d.ImporteTotal : d.ImporteTotal;

            table.Cell().Element(c => TD(c, d.NumeroCompleto, bg));
            table.Cell().Element(c => TD(c, tipo ?? "", bg));
            table.Cell().Element(c => TD(c, d.FechaEmision.ToString("dd/MM/yyyy"), bg));
            table.Cell().Element(c => TD(c, d.Cliente?.RazonSocial ?? "-", bg));
            table.Cell().Element(c => TD(c, d.Cliente?.NumeroDocumento ?? "-", bg));
            table.Cell().Element(c => TD(c, $"{(vv < 0 ? "-" : "")}{Math.Abs(vv):N2}", bg, right: true, red: esNC));
            table.Cell().Element(c => TD(c, $"{(igv < 0 ? "-" : "")}{Math.Abs(igv):N2}", bg, right: true, red: esNC));
            table.Cell().Element(c => TD(c, $"{(tot < 0 ? "-" : "")}{Math.Abs(tot):N2}", bg, right: true, red: esNC));
            table.Cell().Element(c => TD(c, d.TipoMoneda ?? "PEN", bg));
            table.Cell().Element(c => TD(c, d.EstadoSunat ?? "-", bg));
            table.Cell().Element(c => TD(c, d.TipoPago ?? "-", bg));
        }

        // ── Fila TOTAL ───────────────────────────────────────────────────────
        var tvv  = lista.Sum(x => x.TipoComprobante == "07" ? -x.ValorVenta   : x.ValorVenta);
        var tigv = lista.Sum(x => x.TipoComprobante == "07" ? -x.TotalIGV     : x.TotalIGV);
        var timp = lista.Sum(x => x.TipoComprobante == "07" ? -x.ImporteTotal : x.ImporteTotal);

        void TotalCell(IContainer c, string txt, bool right = false) =>
            c.Background(totalBg).BorderTop(1.5f).BorderColor(totalBorder).Padding(3)
             .Column(col =>
             {
                 if (right) col.Item().AlignRight().Text(txt).Bold().FontSize(8);
                 else       col.Item().Text(txt).Bold().FontSize(8);
             });

        table.Cell().ColumnSpan(5).Element(c => TotalCell(c, totalLabel));
        table.Cell().Element(c => TotalCell(c, $"{tvv:N2}", right: true));
        table.Cell().Element(c => TotalCell(c, $"{tigv:N2}", right: true));
        table.Cell().Element(c => TotalCell(c, $"{timp:N2}", right: true));
        table.Cell().ColumnSpan(3).Element(c => TotalCell(c, ""));
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
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8).FontColor("#1A1A1A"));

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
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8).FontColor("#1A1A1A"));

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
