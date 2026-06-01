using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Common.Interfaces.Persistence.Reportes;
using IdeatecAPI.Application.Features.Reportes.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace IdeatecAPI.Infrastructure.Services;

public class ControlCajaTicketPdfService : IControlCajaTicketPdfService
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string Azul      = "#1A2B4A";
    private static readonly string Blanco    = "#FFFFFF";
    private static readonly string GrisClaro = "#F5F7FA";
    private static readonly string GrisBorde = "#D0D7E3";

    public ControlCajaTicketPdfService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<byte[]> GenerarAsync(
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

        // Totales por medio de pago (solo movimientos, solo PEN y USD separados)
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

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.ContinuousSize(227, Unit.Point); // 80mm
                page.MarginTop(6, Unit.Millimetre);
                page.MarginBottom(8, Unit.Millimetre);
                page.MarginLeft(3, Unit.Millimetre);
                page.MarginRight(3, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontFamily(Fonts.Lato).FontSize(7).FontColor("#1A1A1A"));

                page.Content().Column(col =>
                {
                    // ── Logo ────────────────────────────────────────────────
                    if (empresa != null && !string.IsNullOrEmpty(empresa.LogoBase64))
                    {
                        try
                        {
                            var lb = Convert.FromBase64String(
                                empresa.LogoBase64.Contains(",")
                                    ? empresa.LogoBase64.Split(',')[1]
                                    : empresa.LogoBase64);
                            col.Item().AlignCenter().Width(55).Height(28).Image(lb).FitArea();
                            col.Item().Height(3);
                        }
                        catch { }
                    }

                    // ── Datos empresa ───────────────────────────────────────
                    col.Item().AlignCenter()
                        .Text(empresa?.NombreComercial ?? empresa?.RazonSocial ?? ruc)
                        .Bold().FontSize(9).FontColor(Azul);
                    col.Item().AlignCenter()
                        .Text($"RUC: {ruc}").FontSize(7).FontColor(Azul);
                    if (!string.IsNullOrEmpty(empresa?.Direccion))
                        col.Item().AlignCenter().Text(empresa.Direccion).FontSize(6);

                    col.Item().Height(3);
                    col.Item().LineHorizontal(0.5f).LineColor(Azul);
                    col.Item().Height(2);

                    // ── Título y cabecera ───────────────────────────────────
                    col.Item().AlignCenter().Text(titulo.ToUpper())
                        .Bold().FontSize(8).FontColor(Azul);

                    col.Item().Height(2);

                    // Fecha
                    col.Item().Row(r =>
                    {
                        r.ConstantItem(35).Text("Fecha:").Bold().FontSize(6).FontColor(Azul);
                        r.RelativeItem().Text(fechaReporte.ToString("dd/MM/yyyy HH:mm:ss")).FontSize(6);
                    });

                    // Rango filtrado
                    if (fechaDesde.HasValue)
                    {
                        var rango = fechaHasta.HasValue && fechaHasta.Value.Date != fechaDesde.Value.Date
                            ? $"{fechaDesde:dd/MM/yyyy} - {fechaHasta:dd/MM/yyyy}"
                            : fechaDesde.Value.ToString("dd/MM/yyyy");
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(35).Text("Período:").Bold().FontSize(6).FontColor(Azul);
                            r.RelativeItem().Text(rango).FontSize(6);
                        });
                    }

                    // Responsable
                    if (!string.IsNullOrWhiteSpace(nombreResponsable))
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(55).Text("Responsable:").Bold().FontSize(6).FontColor(Azul);
                            r.RelativeItem().Text(nombreResponsable).FontSize(6);
                        });

                    col.Item().Height(3);
                    col.Item().LineHorizontal(0.5f).LineColor(Azul);
                    col.Item().Height(2);

                    // ── Tabla de comprobantes ───────────────────────────────
                    col.Item().Height(4);  // salto antes de DETALLE
                    col.Item().Text("DETALLE").Bold().FontSize(7).FontColor(Azul);
                    col.Item().Height(1);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(16);  // # (16pt: soporta hasta 3 dígitos sin salto)
                            cols.ConstantColumn(24);  // Serie
                            cols.ConstantColumn(26);  // Num
                            cols.ConstantColumn(52);  // Monto
                            cols.ConstantColumn(20);  // Mon
                            cols.RelativeColumn();    // T.Pago (~72pt: suficiente p/ 3 medios)
                        });

                        void TH(IContainer c, string t, bool r = false)
                        {
                            var el = c.Background(Azul).Padding(2);
                            if (r) el.AlignRight().Text(t).Bold().FontSize(5.5f).FontColor(Blanco);
                            else el.Text(t).Bold().FontSize(5.5f).FontColor(Blanco);
                        }
                        void TD(IContainer c, string t, bool r = false, bool red = false)
                        {
                            var el = c.BorderBottom(0.3f).BorderColor(GrisBorde).Padding(2);
                            var tx = r ? el.AlignRight().Text(t).FontSize(6) : el.Text(t).FontSize(6);
                            if (red) tx.FontColor("#C00000");
                        }

                        table.Header(h =>
                        {
                            h.Cell().Element(c => TH(c, "#"));
                            h.Cell().Element(c => TH(c, "Serie"));
                            h.Cell().Element(c => TH(c, "Num."));
                            h.Cell().Element(c => TH(c, "Monto", r: true));
                            h.Cell().Element(c => TH(c, "Mon"));
                            h.Cell().Element(c => TH(c, "T.Pago"));
                        });

                        int n = 1;
                        foreach (var d in movimientos)
                        {
                            bool esNC = d.TipoComprobante == "07";
                            decimal monto = esNC ? -d.ImporteTotal : d.ImporteTotal;
                            string medios = d.Pagos.Any()
                                ? string.Join("/", d.Pagos.Select(p => AbreviarMedio(p.MedioPago)))
                                : "-";

                            table.Cell().Element(c => TD(c, (n++).ToString()));
                            table.Cell().Element(c => TD(c, d.Serie));
                            table.Cell().Element(c => TD(c, (d.Correlativo ?? 0).ToString()));
                            table.Cell().Element(c => TD(c, FmtTicket(monto, d.TipoMoneda), r: true, red: esNC));
                            table.Cell().Element(c => TD(c, d.TipoMoneda == "USD" ? "USD" : "PEN"));
                            table.Cell().Element(c => TD(c, medios));
                        }
                    });

                    col.Item().Height(3);
                    col.Item().LineHorizontal(0.5f).LineColor(Azul);
                    col.Item().Height(2);

                    // ── Totales del período ────────────────────────────────
                    if (totalPen != 0)
                        FilaResumen(col, "TOTAL (PEN)", $"S/ {totalPen:N2}", bold: true);
                    if (totalUsd != 0)
                        FilaResumen(col, "TOTAL (USD)", $"$ {totalUsd:N2}", bold: true);

                    col.Item().Height(3);
                    col.Item().LineHorizontal(0.5f).LineColor(Azul);
                    col.Item().Height(2);

                    // ── Resumen por medio de pago ──────────────────────────
                    col.Item().Text("RESUMEN").Bold().FontSize(7).FontColor(Azul);
                    col.Item().Height(1);
                    foreach (var r in resumenPago)
                    {
                        var simbolo = r.Moneda == "USD" ? "$" : "S/";
                        FilaResumen(col, $"{r.Medio} ({r.Moneda})", $"{simbolo} {r.Total:N2}");
                    }

                    // ── Ajustes de otros períodos ──────────────────────────
                    if (ajustes.Any())
                    {
                        col.Item().Height(6);  // salto antes de AJUSTES
                        col.Item().LineHorizontal(0.5f).LineColor("#7030A0");
                        col.Item().Height(3);

                        col.Item().Text("AJUSTES DE OTROS PERÍODOS").Bold().FontSize(7).FontColor("#7030A0");
                        col.Item().Text("(no afectan el total anterior)").Italic().FontSize(5.5f).FontColor("#7030A0");
                        col.Item().Height(1);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(16);
                                cols.ConstantColumn(24);
                                cols.ConstantColumn(26);
                                cols.ConstantColumn(52);
                                cols.ConstantColumn(20);
                                cols.RelativeColumn();
                            });

                            void TH2(IContainer c, string t, bool r = false)
                            {
                                var el = c.Background("#7030A0").Padding(2);
                                if (r) el.AlignRight().Text(t).Bold().FontSize(5.5f).FontColor(Blanco);
                                else el.Text(t).Bold().FontSize(5.5f).FontColor(Blanco);
                            }
                            void TD2(IContainer c, string t, bool r = false, bool red = false)
                            {
                                var el = c.BorderBottom(0.3f).BorderColor(GrisBorde).Padding(2);
                                var tx = r ? el.AlignRight().Text(t).FontSize(6) : el.Text(t).FontSize(6);
                                if (red) tx.FontColor("#C00000");
                            }

                            table.Header(h =>
                            {
                                h.Cell().Element(c => TH2(c, "#"));
                                h.Cell().Element(c => TH2(c, "Serie"));
                                h.Cell().Element(c => TH2(c, "Num."));
                                h.Cell().Element(c => TH2(c, "Monto", r: true));
                                h.Cell().Element(c => TH2(c, "Mon"));
                                h.Cell().Element(c => TH2(c, "T.Pago"));
                            });

                            int n = 1;
                            foreach (var d in ajustes)
                            {
                                bool esNC = d.TipoComprobante == "07";
                                decimal monto = esNC ? -d.ImporteTotal : d.ImporteTotal;
                                string medios = d.Pagos.Any()
                                    ? string.Join("/", d.Pagos.Select(p => AbreviarMedio(p.MedioPago)))
                                    : "-";

                                table.Cell().Element(c => TD2(c, (n++).ToString()));
                                table.Cell().Element(c => TD2(c, d.Serie));
                                table.Cell().Element(c => TD2(c, (d.Correlativo ?? 0).ToString()));
                                table.Cell().Element(c => TD2(c, FmtTicket(monto, d.TipoMoneda), r: true, red: esNC));
                                table.Cell().Element(c => TD2(c, d.TipoMoneda == "USD" ? "USD" : "PEN"));
                                table.Cell().Element(c => TD2(c, medios));
                            }
                        });
                    }

                    col.Item().Height(6);
                    col.Item().AlignCenter()
                        .Text($"Generado: {fechaReporte:dd/MM/yyyy HH:mm:ss}")
                        .Italic().FontSize(5.5f).FontColor("#4A5568");
                });
            });
        });

        return doc.GeneratePdf();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static void FilaResumen(ColumnDescriptor col, string label, string valor, bool bold = false)
    {
        col.Item().Row(r =>
        {
            var lbl = r.RelativeItem().Text(label).FontSize(6);
            if (bold) lbl.Bold();
            var val = r.AutoItem().Text(valor).FontSize(6);
            if (bold) val.Bold();
        });
    }

    private static string FmtTicket(decimal v, string? moneda)
        => moneda == "USD" ? $"$ {v:N2}" : $"S/ {v:N2}";

    private static string NormalizarMedio(string? medio)
    {
        if (string.IsNullOrWhiteSpace(medio)) return "Efectivo";
        return medio.Trim().ToLower() switch
        {
            "efectivo" => "Efectivo",
            "yape"     => "Yape",
            "plin"     => "Plin",
            "transferencia" or "transferencia bancaria" => "Transferencia",
            "tarjeta" or "tarjeta de crédito" or "tarjeta de débito" or "tarjeta de debito" => "Tarjeta",
            "depósito" or "deposito" or "depósito bancario" or "deposito bancario" => "Depósito",
            "pos"      => "POS",
            "cheque"   => "Cheque",
            var s      => char.ToUpper(s[0]) + s[1..]
        };
    }

    private static string AbreviarMedio(string? medio)
    {
        if (string.IsNullOrWhiteSpace(medio)) return "Efec.";
        return medio.Trim().ToLower() switch
        {
            "efectivo" => "Efec.",
            "yape"     => "Yape",
            "plin"     => "Plin",
            "transferencia" or "transferencia bancaria" => "Trans.",
            "tarjeta" or "tarjeta de crédito" or "tarjeta de débito" or "tarjeta de debito" => "Tarj.",
            "depósito" or "deposito" or "depósito bancario" or "deposito bancario" => "Dep.",
            "pos"      => "POS",
            "cheque"   => "Cheq.",
            var s      => s.Length > 5 ? s[..5] + "." : s
        };
    }
}
