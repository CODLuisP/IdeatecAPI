using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Comprobante.Services;
using IdeatecAPI.Application.Features.Empresas.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace IdeatecAPI.Infrastructure.Services;

/// Implementación de <see cref="IComprobantePdfService"/> usando QuestPDF.
public class ComprobantePdfService : IComprobantePdfService
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string ColorAzulMarino = "#1A2B4A";
    private static readonly string ColorBlanco = "#FFFFFF";
    private static readonly string ColorGrisClaro = "#F5F7FA";
    private static readonly string ColorGrisBorde = "#D0D7E3";
    private static readonly string ColorTexto = "#1A1A1A";
    private static readonly string ColorTextoSuave = "#4A5568";

    public ComprobantePdfService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<byte[]> GenerarPdfAsync(int comprobanteId, TamanoPdf tamano = TamanoPdf.A4)
    {
        var swPdfInterno = System.Diagnostics.Stopwatch.StartNew();

        var comprobante = await _unitOfWork.Comprobantes.GetByIdAsync(comprobanteId)
            ?? throw new KeyNotFoundException($"Comprobante {comprobanteId} no encontrado.");

        var empresaEntidad = await _unitOfWork.Empresas.GetEmpresaByRucAsync(comprobante.EmpresaRuc ?? "")
            ?? throw new KeyNotFoundException($"Empresa con RUC '{comprobante.EmpresaRuc}' no encontrada.");

        var datos = await _unitOfWork.Comprobantes.GetDatosCompletosByComprobanteIdAsync(comprobanteId);

        swPdfInterno.Stop();

        var swRender = System.Diagnostics.Stopwatch.StartNew();

        var empresa = new EmpresaDto
        {
            Ruc = empresaEntidad.Ruc,
            RazonSocial = empresaEntidad.RazonSocial,
            NombreComercial = empresaEntidad.NombreComercial,
            Direccion = empresaEntidad.Direccion,
            Telefono = empresaEntidad.Telefono,
            Email = empresaEntidad.Email,
            LogoBase64 = empresaEntidad.LogoBase64,
        };

        var detalles    = datos.Detalles.ToList();
        var pagos       = datos.Pagos.ToList();
        var cuotas      = datos.Cuotas.ToList();
        var leyendas    = datos.Leyendas.ToList();
        var guias       = datos.Guias.ToList();
        var detracciones = datos.Detracciones.ToList();

        // Vales — solo se usan en ticket; vacío si no tiene
        bool esTicket = tamano == TamanoPdf.Ticket80mm || tamano == TamanoPdf.Ticket58mm;
        var vales = esTicket
            ? (await _unitOfWork.Comprobantes.GetValesFullByComprobanteIdAsync(comprobanteId)).ToList()
            : new List<Domain.Entities.Vale>();

        var doc = Document.Create(container =>
        {
            // ── Página 1: comprobante ────────────────────────────────────────
            container.Page(page =>
            {
                if (esTicket)
                    page.ContinuousSize(ResolverAnchoTicket(tamano), Unit.Point);
                else
                    page.Size(ResolverTamano(tamano));

                page.MarginTop(esTicket ? 8 : 15, Unit.Millimetre);
                page.MarginBottom(esTicket ? 8 : 15, Unit.Millimetre);
                page.MarginLeft(esTicket ? 3 : 15, Unit.Millimetre);
                page.MarginRight(esTicket ? 3 : 15, Unit.Millimetre);
                page.DefaultTextStyle(x =>
                    x.FontFamily("Arial").FontSize(esTicket ? 7 : 9).FontColor(ColorTexto));

                if (esTicket)
                {
                    page.Content().Element(c =>
                        BuildTicket(c, comprobante, empresa, detalles, pagos, cuotas,
                                    leyendas, guias, detracciones));
                }
                else
                {
                    page.Header().Element(c => BuildHeader(c, comprobante, empresa));
                    page.Content().Element(c =>
                        BuildContent(c, comprobante, empresa, detalles, pagos, cuotas,
                                     leyendas, guias, detracciones));
                }
            });

            // ── Página 2: vale (solo en ticket y si tiene vales) ─────────────
            if (esTicket && vales.Any())
            {
                foreach (var vale in vales)
                {
                    container.Page(page =>
                    {
                        page.ContinuousSize(ResolverAnchoTicket(tamano), Unit.Point);
                        page.MarginTop(4, Unit.Millimetre);
                        page.MarginBottom(8, Unit.Millimetre);
                        page.MarginLeft(3, Unit.Millimetre);
                        page.MarginRight(3, Unit.Millimetre);
                        page.DefaultTextStyle(x =>
                            x.FontFamily("Arial").FontSize(7).FontColor(ColorTexto));

                        page.Content().Element(c => BuildValeTicket(c, comprobante, vale));
                    });
                }
            }
        });

        return doc.GeneratePdf();
    }

    // ════════════════════════════════════════════════════════════════════════
    // LAYOUT TICKET — columna única
    // ════════════════════════════════════════════════════════════════════════
    private void BuildTicket(IContainer container,
        Domain.Entities.Comprobante c, EmpresaDto empresa,
        List<Domain.Entities.ComprobanteDetalle> detalles,
        List<Domain.Entities.Pago> pagos,
        List<Domain.Entities.Cuota> cuotas,
        List<Domain.Entities.NoteLegend> leyendas,
        List<Domain.Entities.GuiaComprobante> guias,
        List<Domain.Entities.Detraccion> detracciones)
    {
        var moneda = c.TipoMoneda ?? "PEN";

        container.Column(col =>
        {
            // 1. LOGO centrado
            if (!string.IsNullOrEmpty(empresa.LogoBase64))
            {
                try
                {
                    var logoBytes = Convert.FromBase64String(
                        empresa.LogoBase64.Contains(",")
                            ? empresa.LogoBase64.Split(',')[1]
                            : empresa.LogoBase64);

                    var (pw, ph) = LeerDimensionesImagen(logoBytes);
                    float ratio = (float)pw / ph;

                    const float maxAlto = 28f;
                    const float maxAncho = 55f;

                    float alto = maxAlto;
                    float ancho = alto * ratio;

                    if (ancho > maxAncho)
                    {
                        ancho = maxAncho;
                        alto = ancho / ratio;
                    }

                    col.Item().AlignCenter()
                        .Width(ancho)
                        .Height(alto)
                        .Image(logoBytes)
                        .FitArea();

                    col.Item().PaddingTop(3);
                }
                catch { }
            }

            // 2. DATOS EMPRESA centrados
            col.Item().AlignCenter()
                .Text(empresa.NombreComercial ?? empresa.RazonSocial)
                .Bold().FontSize(9).FontColor(ColorAzulMarino);

            col.Item().AlignCenter().Text(empresa.RazonSocial)
                .FontSize(7).FontColor(ColorTextoSuave);

            if (!string.IsNullOrEmpty(empresa.Direccion))
                col.Item().AlignCenter().Text(empresa.Direccion)
                    .FontSize(6).FontColor(ColorTextoSuave);
            if (!string.IsNullOrEmpty(empresa.Email))
                col.Item().AlignCenter().Text($"Email: {empresa.Email}")
                    .FontSize(6).FontColor(ColorTextoSuave);

            col.Item().Height(6);
            col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(ColorAzulMarino);

            // 3. RUC / TIPO / NÚMERO
            col.Item().PaddingTop(3).AlignCenter()
                .Text($"RUC: {empresa.Ruc}").Bold().FontSize(8).FontColor(ColorAzulMarino);

            col.Item().PaddingTop(1).Background(ColorAzulMarino).Padding(3).AlignCenter()
                .Text(ObtenerNombreTipoComprobante(c.TipoComprobante))
                .Bold().FontSize(8).FontColor(ColorBlanco);

            col.Item().PaddingTop(1).AlignCenter()
                .Text($"N° {c.Serie}-{c.Correlativo:D8}")
                .Bold().FontSize(8).FontColor(ColorAzulMarino);

            col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(ColorAzulMarino);
            col.Item().Height(6);

            // 4. DATOS CLIENTE
            col.Item().PaddingTop(3).Column(cli =>
            {
                TicketFila(cli, "Cliente", c.ClienteRazonSocial ?? "-");
                TicketFila(cli, ObtenerLabelTipoDoc(c.ClienteTipoDoc), c.ClienteNumDoc ?? "-");
                if (!string.IsNullOrEmpty(c.ClienteDireccion))
                    TicketFila(cli, "Dir.", c.ClienteDireccion);
                TicketFila(cli, "Fecha", $"{c.FechaEmision:dd/MM/yyyy} {c.HoraEmision:HH:mm:ss}");
                if ((c.TipoPago?.ToLower() ?? "") is "credito" or "crédito")
                    TicketFila(cli, "Fecha Vcto.", $"{c.FechaVencimiento:dd/MM/yyyy}");

                if (c.TipoMoneda != "PEN" && c.TipoCambio.HasValue)
                    TicketFila(cli, "Moneda", $"{c.TipoMoneda} T.C. S/{c.TipoCambio:F3}");
            });

            //documento que modifca
            col.Item().Height(6);
            if (c.TipoComprobante is "07" or "08"
            && !string.IsNullOrEmpty(c.TipDocAfectado)
            && !string.IsNullOrEmpty(c.NumDocAfectado))
            {
                col.Item().PaddingTop(3)
                    .Element(ct => BuildDocumentoModifica(ct, c, true));
            }

            col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(ColorAzulMarino);
            col.Item().Height(6);

            // 5. TABLA DETALLES reducida
            bool mostrarCodigo = detalles.Any(d => !string.IsNullOrWhiteSpace(d.Codigo));

            col.Item().PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(10); // #
                    if (mostrarCodigo) cols.ConstantColumn(27); // Cod
                    cols.ConstantColumn(18); // Cant
                    cols.RelativeColumn();   // Desc
                    cols.ConstantColumn(28); // P.Unit
                    cols.ConstantColumn(28); // Total
                });

                void TH(IContainer tc, string txt) =>
                    tc.Background(ColorAzulMarino).Padding(2).AlignLeft()
                      .Text(txt).Bold().FontSize(6).FontColor(ColorBlanco);

                table.Header(h =>
                {
                    h.Cell().Element(tc => TH(tc, "#"));
                    if (mostrarCodigo) h.Cell().Element(tc => TH(tc, "Cod"));
                    h.Cell().Element(tc => TH(tc, "Cant"));
                    h.Cell().Element(tc => TH(tc, "Desc"));
                    h.Cell().Element(tc => TH(tc, "P.Vent"));
                    h.Cell().Element(tc => TH(tc, "Total"));
                });

                bool par = false;
                int itemIndex = 1;
                foreach (var d in detalles)
                {
                    var bg = par ? ColorBlanco : ColorGrisClaro;
                    par = !par;
                    bool esGratuito = d.TipoAfectacionIGV is "11" or "21" or "31";

                    void TD(IContainer tc, string txt, bool right = false)
                    {
                        var el = tc.Background(bg).Padding(2);
                        if (right) el.AlignRight().Text(txt).FontSize(6);
                        else el.Text(txt).FontSize(6);
                    }

                    table.Cell().Element(tc => TD(tc, (itemIndex++).ToString()));
                    if (mostrarCodigo) table.Cell().Element(tc => TD(tc, d.Codigo ?? "-"));
                    table.Cell().Element(tc => TD(tc, d.Cantidad.ToString("F2")));

                    // Descripción — limpia, sin descuento
                    table.Cell().Element(tc =>
                    {
                        var desc = d.Descripcion ?? "-";
                        if (esGratuito) desc += " (GR)";
                        TD(tc, desc);
                    });

                    // P.Vent — precio original + descuento debajo si aplica
                    table.Cell().Element(tc =>
                    {
                        var pVentOriginal = (d.DescuentoTotal ?? 0) > 0
                            ? Math.Round((d.PrecioVenta ?? 0) + (d.DescuentoUnitario ?? 0), 2)
                            : (d.PrecioVenta ?? 0);

                        var dsctoConIgv = (d.DescuentoUnitario ?? 0) > 0
                            ? Math.Round(d.DescuentoUnitario ?? 0, 2)
                            : 0;

                        if (dsctoConIgv > 0)
                        {
                            tc.Background(bg).Padding(2).Column(col =>
                            {
                                col.Item().AlignRight()
                                    .Text(pVentOriginal.ToString("F2"))
                                    .FontSize(6);
                                col.Item().AlignRight()
                                    .Text($"-{dsctoConIgv:F2}")
                                    .FontSize(5).FontColor(ColorTextoSuave);
                            });
                        }
                        else
                        {
                            TD(tc, pVentOriginal.ToString("F2"), right: true);
                        }
                    });

                    // Total
                    table.Cell().Element(tc => TD(tc,
                        (esGratuito ? 0 : d.TotalVentaItem ?? 0).ToString("F2"), right: true));
                }
            });

            col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(ColorAzulMarino);
            col.Item().Height(6);

            // 6. TOTALES
            col.Item().PaddingTop(3).Column(tot =>
            {
                TicketFilaTotal(tot, "Op. Gravadas", FormatearMoneda(c.TotalOperacionesGravadas ?? 0, moneda));
                TicketFilaTotal(tot, "Op. Exoneradas", FormatearMoneda(c.TotalOperacionesExoneradas ?? 0, moneda));
                TicketFilaTotal(tot, "Op. Inafectas", FormatearMoneda(c.TotalOperacionesInafectas ?? 0, moneda));
                TicketFilaTotal(tot, $"I.G.V. ({detalles.FirstOrDefault(d => (d.PorcentajeIGV ?? 0) > 0)?.PorcentajeIGV ?? 18:G29}%)", FormatearMoneda(c.TotalIGV ?? 0, moneda));

                if ((c.TotalIcbper ?? 0) > 0)
                    TicketFilaTotal(tot, "ICBPER", FormatearMoneda(c.TotalIcbper ?? 0, moneda));

                if ((c.TotalDescuentos ?? 0) > 0)
                    TicketFilaTotal(tot, "Descuentos", $"-{FormatearMoneda(c.TotalDescuentos ?? 0, moneda)}");

                if ((c.DescuentoGlobal ?? 0) > 0)
                    TicketFilaTotal(tot, "Dscto. Global", $"-{FormatearMoneda(c.DescuentoGlobal ?? 0, moneda)}");

                tot.Item().PaddingTop(2).Background(ColorAzulMarino).Padding(3).Row(r =>
                {
                    r.RelativeItem().Text("IMPORTE TOTAL").Bold().FontSize(8).FontColor(ColorBlanco);
                    r.AutoItem().AlignRight()
                        .Text(FormatearMoneda(c.ImporteTotal ?? 0, moneda))
                        .Bold().FontSize(8).FontColor(ColorBlanco);
                });
            });

            //col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(ColorAzulMarino);

            // 7. LEYENDAS
            if (leyendas.Any())
            {
                col.Item().PaddingTop(3).Column(ley =>
                {
                    foreach (var l in leyendas)
                        ley.Item().Text(l.Value.ToUpper()).Bold().FontSize(6).FontColor(ColorAzulMarino);
                });
                col.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(ColorAzulMarino);
            }

            //Leyenda Motivo Nota
            if (c.TipoComprobante is "07" or "08")
            {
                col.Item().Element(ct => BuildMotivoNota(ct, c));
            }

            // 8. TIPO DE PAGO
            col.Item().PaddingTop(3)
                .Element(pc => BuildSeccionPagosTicket(pc, c, pagos, cuotas, moneda));

            col.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(ColorAzulMarino);
            col.Item().Height(6);

            // 9. DETRACCIÓN
            if (detracciones.Any())
            {
                col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(ColorAzulMarino);
                col.Item().PaddingTop(3)
                    .Element(dc => BuildSeccionDetraccionTicket(dc, detracciones, moneda, c.TipoComprobante));
                col.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(ColorAzulMarino);
                col.Item().Height(6);

            }

            // 10. QR
            if (!string.IsNullOrEmpty(empresa.Ruc))
            {
                var qrContent = BuildQrContent(c);
                var qrBytes = GenerateQrCode(qrContent);
                if (qrBytes.Length > 0)
                {
                    col.Item().PaddingTop(6).AlignCenter()
                        .Width(60).Height(60)
                        .Image(qrBytes).FitArea();
                }
            }

            col.Item().PaddingTop(3).AlignCenter()
                .Text($"Representación impresa de {ObtenerNombreTipoComprobante(c.TipoComprobante)}")
                .FontSize(5).FontColor(ColorTextoSuave);
        });
    }

    private static void BuildSeccionPagosTicket(IContainer container,
        Domain.Entities.Comprobante c,
        List<Domain.Entities.Pago> pagos,
        List<Domain.Entities.Cuota> cuotas,
        string moneda)
    {
        container.Column(col =>
        {
            bool tieneInicialYCuotas = pagos.Any() && cuotas.Any();
            bool esCredito = (c.TipoPago?.ToLower() ?? "") is "credito" or "crédito";

            col.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(ColorAzulMarino);
            col.Item().PaddingTop(3).Text("FORMA DE PAGO").Bold().FontSize(6).FontColor(ColorAzulMarino);

            if (!esCredito)
            {
                TicketFilaTotal(col, "Tipo", "Contado");
                if (pagos.Any())
                    foreach (var p in pagos)
                        TicketFilaTotal(col, p.MedioPago ?? "Efectivo", FormatearMoneda(p.Monto ?? 0, moneda));
                else
                    TicketFilaTotal(col, "Efectivo", FormatearMoneda(c.ImporteTotal ?? 0, moneda));
            }
            else if (tieneInicialYCuotas)
            {
                TicketFilaTotal(col, "Tipo", "Crédito con inicial");
                foreach (var p in pagos)
                    TicketFilaTotal(col, $"Inicial ({p.MedioPago ?? "Efectivo"})", FormatearMoneda(p.Monto ?? 0, moneda));

                col.Item().PaddingTop(2).Text("Cuotas:").Bold().FontSize(6).FontColor(ColorAzulMarino);
                foreach (var cu in cuotas)
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(cu.NumeroCuota ?? "").FontSize(6);
                        r.RelativeItem().AlignCenter().Text(FormatearMoneda(cu.Monto ?? 0, moneda)).FontSize(6);
                        r.RelativeItem().AlignRight().Text($"{cu.FechaVencimiento:dd/MM/yy}").FontSize(6);
                    });
            }
            else
            {
                TicketFilaTotal(col, "Tipo", "Crédito");
                TicketFilaTotal(col, "Monto Crédito", FormatearMoneda(c.MontoCredito ?? 0, moneda));

                if (cuotas.Any())
                {
                    col.Item().PaddingTop(2).Text("Cuotas:").Bold().FontSize(6).FontColor(ColorAzulMarino);
                    foreach (var cu in cuotas)
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text(cu.NumeroCuota ?? "").FontSize(6);
                            r.RelativeItem().AlignCenter().Text(FormatearMoneda(cu.Monto ?? 0, moneda)).FontSize(6);
                            r.RelativeItem().AlignRight().Text($"{cu.FechaVencimiento:dd/MM/yy}").FontSize(6);
                        });
                }
            }
        });
    }

    private static void BuildSeccionDetraccionTicket(IContainer container,
        List<Domain.Entities.Detraccion> detracciones, string moneda, string? tipoComprobante)
    {
        container.Column(col =>
        {
            col.Item().Text("DETRACCIÓN").Bold().FontSize(7).FontColor(ColorAzulMarino);
            foreach (var det in detracciones)
            {
                if (tipoComprobante == "01")
                    TicketFilaTotal(col, "Cta. BN", det.CuentaBancoDetraccion ?? "-");

                TicketFilaTotal(col, "% Detrac.", $"{det.PorcentajeDetraccion:F2}%");
                TicketFilaTotal(col, "Monto", FormatearMoneda(det.MontoDetraccion ?? 0, moneda));
            }
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // LAYOUT A4 / CARTA / MEDIA CARTA
    // ════════════════════════════════════════════════════════════════════════
    private static void BuildHeader(IContainer container, Domain.Entities.Comprobante c, EmpresaDto empresa)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // LOGO (sin cambios)
                if (!string.IsNullOrEmpty(empresa.LogoBase64))
                {
                    try
                    {
                        var logoBytes = Convert.FromBase64String(
                            empresa.LogoBase64.Contains(",")
                                ? empresa.LogoBase64.Split(',')[1]
                                : empresa.LogoBase64);

                        var (pw, ph) = LeerDimensionesImagen(logoBytes);
                        float ratio = (float)pw / ph;

                        const float maxAlto = 70f;
                        const float maxAncho = 110f;

                        float alto, ancho;

                        if (ratio > 1.5f)
                        {
                            ancho = maxAncho;
                            alto = ancho / ratio;
                        }
                        else if (ratio < 0.75f)
                        {
                            alto = maxAlto;
                            ancho = alto * ratio;
                        }
                        else
                        {
                            alto = maxAlto;
                            ancho = alto * ratio;
                        }

                        row.ConstantItem(ancho)
                            .AlignMiddle()
                            .AlignCenter()
                            .Height(alto)
                            .Image(logoBytes)
                            .FitArea();
                    }
                    catch { row.ConstantItem(70); }
                }
                else { row.ConstantItem(70); }

                // DATOS EMPRESA (sin cambios)
                row.RelativeItem().PaddingLeft(6).PaddingRight(10).AlignMiddle().Column(emp =>
                {
                    emp.Item().Text(empresa.RazonSocial)
                        .Bold().FontSize(14).FontColor(ColorAzulMarino);
                    if (!string.IsNullOrEmpty(empresa.Direccion))
                        emp.Item().PaddingRight(20).Text(empresa.Direccion)
                        .FontSize(8).FontColor(ColorTextoSuave);
                    if (!string.IsNullOrEmpty(empresa.Telefono))
                        emp.Item().Text($"Telf: {empresa.Telefono}")
                            .FontSize(8).FontColor(ColorTextoSuave);
                    if (!string.IsNullOrEmpty(empresa.Email))
                        emp.Item().Text($"Email: {empresa.Email}")
                            .FontSize(8).FontColor(ColorTextoSuave);
                });

                // RECUADRO COMPROBANTE (sin cambios - sin Orden Servicio)
                row.ConstantItem(160).AlignMiddle().Border(1).BorderColor(ColorAzulMarino).Column(box =>
                {
                    box.Item().Background(ColorGrisClaro).Padding(5).AlignCenter()
                        .Text($"R.U.C. {empresa.Ruc}").Bold().FontSize(9).FontColor(ColorAzulMarino);
                    box.Item().Background(ColorAzulMarino).Padding(5).AlignCenter()
                        .Text(ObtenerNombreTipoComprobante(c.TipoComprobante))
                        .Bold().FontSize(9).FontColor(ColorBlanco);
                    box.Item().Background(ColorGrisClaro).Padding(5).AlignCenter()
                        .Text($"N° {c.Serie}-{c.Correlativo:D8}")
                        .Bold().FontSize(9).FontColor(ColorAzulMarino);
                });
            });

            col.Item().Height(6);

            // === NUEVO: Cuadro independiente para Orden de Servicio ===
            if (!string.IsNullOrWhiteSpace(c.OrdenServicio))
            {
                col.Item().PaddingTop(1).Row(row =>
            {
                // Espacio para alinear con el recuadro del comprobante
                row.ConstantItem(70); // Mismo ancho que el área del logo
                row.RelativeItem(); // Espacio de datos empresa
                row.ConstantItem(160).AlignMiddle().Border(1).BorderColor(ColorAzulMarino)
                    .Background(ColorGrisClaro).Padding(5).AlignCenter()
                    .Text($"ORDEN DE SERVICIO: {c.OrdenServicio}")
                    .Bold().FontSize(9).FontColor(ColorAzulMarino);
            });
            }
            // ========================================================

            col.Item().Height(6);
        });
    }

    // Datos cliente — solo primera página, va al inicio del Content
    private static void BuildDatosCliente(IContainer container,
    Domain.Entities.Comprobante c)
    {
        var tipoDocLabel = c.TipoComprobante == "01"
            ? "RUC"
            : ObtenerLabelTipoDoc(c.ClienteTipoDoc);

        container.Background(ColorGrisClaro).Border(1).BorderColor(ColorGrisBorde)
        .Padding(3)
        .Row(row =>
        {
            // ── Izquierda: Cliente, Documento, Dirección ──
            row.RelativeItem(6).Column(rec =>
    {
        BuildFilaDatoSpaced(rec, "Cliente", c.ClienteRazonSocial ?? "-", labelWidth: 55f);
        BuildFilaDatoSpaced(rec, tipoDocLabel, c.ClienteNumDoc ?? "-", labelWidth: 55f);

        if (!string.IsNullOrEmpty(c.ClienteDireccion))
            BuildFilaDatoSpaced(rec, "Dirección",
                $"{c.ClienteDireccion}, {c.ClienteDistrito} {c.ClienteProvincia} {c.ClienteDepartamento}".Trim(),
                labelWidth: 55f);
    });

            // ── Derecha: Fecha Emisión, Tipo Pago, Fecha Vencimiento ──
            row.RelativeItem(4).PaddingLeft(10).Column(right =>
    {
        BuildFilaDatoSpaced(right, "Fecha Emisión", $"{c.FechaEmision:dd/MM/yyyy} {c.HoraEmision:HH:mm:ss}");
        BuildFilaDatoSpaced(right, "Tipo Pago", c.TipoPago ?? "-");
        BuildFilaDatoSpaced(right, "Fecha Vencimiento", c.FechaVencimiento.ToString("dd/MM/yyyy"));

        if (c.TipoMoneda != "PEN" && c.TipoCambio.HasValue)
            BuildFilaDatoSpaced(right, "Moneda", $"{c.TipoMoneda} (T.C. S/ {c.TipoCambio:F3})");

        if (c.TipoComprobante is "07" or "08"
            && !string.IsNullOrEmpty(c.TipDocAfectado)
            && !string.IsNullOrEmpty(c.NumDocAfectado))
        {
            right.Item().PaddingTop(4)
                .Element(ct => BuildDocumentoModifica(ct, c));
        }
    });
        });
    }

    //documento que modifica
    private static void BuildDocumentoModifica(
        IContainer container,
        Domain.Entities.Comprobante c,
        bool esTicket = false)
    {
        if (string.IsNullOrEmpty(c.TipDocAfectado))
            return;

        var denominacion = c.TipDocAfectado switch
        {
            "01" => "FACTURA",
            "03" => "BOLETA",
            _ => "COMPROBANTE"
        };

        if (esTicket)
        {
            // 🎟 FORMATO COMPACTO
            container.PaddingTop(3).Text(
                $"Doc. Modifica: {denominacion} {c.NumDocAfectado}"
            )
            .FontSize(6)
            .FontColor(ColorTexto);
        }
        else
        {
            // 🧾 FORMATO A4
            container.PaddingTop(3).Column(col =>
            {
                col.Item().Text("DOCUMENTO QUE MODIFICA")
                    .Bold()
                    .FontSize(8)
                    .FontColor(ColorAzulMarino);

                col.Item().PaddingTop(2);

                col.Item().Text($"Denominación: {denominacion}")
                    .FontSize(8);

                col.Item().Text($"Número: {c.NumDocAfectado}")
                    .FontSize(8);
            });
        }
    }

    //Leyenda Motivo Nota
    private static void BuildMotivoNota(IContainer container, Domain.Entities.Comprobante c)
    {
        if (string.IsNullOrEmpty(c.MotivoNota))
            return;

        var tipoNota = c.TipoComprobante switch
        {
            "07" => "NOTA DE CRÉDITO",
            "08" => "NOTA DE DÉBITO",
            _ => "NOTA"
        };

        container.PaddingTop(4).Column(col =>
        {
            col.Item().Text(txt =>
            {
                txt.Span($"MOTIVO DE LA {tipoNota}: ")
                    .Bold().FontColor(ColorAzulMarino);

                txt.Span(c.MotivoNota)
                    .FontColor(ColorTexto);
            });

            col.Item().PaddingBottom(6);
        });
    }

    private static void BuildContent(IContainer container,
        Domain.Entities.Comprobante c,
        EmpresaDto empresa,
        List<Domain.Entities.ComprobanteDetalle> detalles,
        List<Domain.Entities.Pago> pagos,
        List<Domain.Entities.Cuota> cuotas,
        List<Domain.Entities.NoteLegend> leyendas,
        List<Domain.Entities.GuiaComprobante> guias,
        List<Domain.Entities.Detraccion> detracciones)
    {
        var moneda = c.TipoMoneda ?? "PEN";

        container.Column(col =>
        {
            // Datos cliente solo en primera página
            col.Item().Element(ct => BuildDatosCliente(ct, c));
            col.Item().PaddingTop(4);

            // Guías de remisión
            if (guias.Any())
            {
                col.Item().PaddingBottom(4)
                    .Background(ColorGrisClaro).Border(1).BorderColor(ColorGrisBorde)
                    .Padding(5).Column(g =>
                    {
                        g.Item().Text("Guías de Remisión Enlazadas")
                            .Bold().FontSize(8).FontColor(ColorAzulMarino);
                        foreach (var guia in guias)
                            g.Item().Text($"[{ObtenerNombreGuia(guia.GuiaTipoDoc)}] {guia.GuiaNumeroCompleto}")
                                .FontSize(8);
                    });
            }

            // Tabla detalles
            col.Item().Height(6);
            col.Item().Element(tc => BuildTablaDetalles(tc, detalles, moneda));

            // Espacio después de los ítems
            col.Item().PaddingTop(5);

            // Leyendas
            if (leyendas.Any())
            {
                col.Item().Column(ley =>
                {
                    foreach (var l in leyendas)
                        ley.Item().Text(l.Value.ToUpper()).Bold().FontSize(8).FontColor(ColorAzulMarino);
                });
                col.Item().PaddingTop(6);
            }

            //Leyenda Motivo Nota
            if (c.TipoComprobante is "07" or "08")
            {
                col.Item().Element(ct => BuildMotivoNota(ct, c));
            }

            // Fila inferior: Pagos+QR | Totales
            col.Item().Row(row =>
            {
                // Izquierda
                row.RelativeItem(5).Column(left =>
                {
                    if (detracciones.Any())
                        left.Item().PaddingTop(6)
                            .Element(lc => BuildSeccionDetraccion(lc, detracciones, moneda, c.TipoComprobante));

                    // Medios de pago — encima del QR (solo si NO es el RUC 20512134832)
                    if (empresa.Ruc != "20512134832")
                    {
                        left.Item().PaddingTop(6)
                            .Element(lc => BuildSeccionMediosPago(lc, c, pagos, cuotas, moneda));
                    }

                    // SPOT para RUC 20512134832 cuando Spot es true
                    if (empresa.Ruc == "20512134832" && c.Spot == true)
                    {
                        var montoSpot = (c.ImporteTotal ?? 0) * 0.10m;

                        left.Item().PaddingTop(6).Column(spotCol =>
                        {
                            spotCol.Item().PaddingTop(2)
                                .Background(ColorGrisClaro).Border(1).BorderColor(ColorGrisBorde)
                                .Padding(5).Column(d =>
                                {
                                    d.Item().Text("Leyenda: Operación sujeta al SPOT con el Gobierno Central")
                                        .FontSize(9);
                                    d.Item().Text("Bien o Servicio: 019 Arrendamiento de bienes")
                                        .FontSize(9);
                                    d.Item().Text("Medio de pago: 001 Depósito en cuenta")
                                        .FontSize(9);
                                    d.Item().Text("N° Cta. Banco de la Nación: 00068273250")
                                        .FontSize(9);
                                    d.Item().Text("Porcentaje de detracción: 10%")
                                        .FontSize(9);
                                    d.Item().Text($"Monto detracción: {FormatearMoneda(montoSpot, moneda)}")
                                        .FontSize(9).Bold();
                                });
                        });
                    }

                    // QR
                    if (!string.IsNullOrEmpty(empresa.Ruc))
                    {
                        var qrContent = BuildQrContent(c);
                        var qrBytes = GenerateQrCode(qrContent);
                        if (qrBytes.Length > 0)
                        {
                            left.Item().PaddingTop(4).PaddingLeft(-5).Row(r =>
                            {
                                r.AutoItem().Width(110).Height(110)
                                    .Image(qrBytes).FitArea();
                                r.RelativeItem();
                            });
                        }
                    }
                });

                row.ConstantItem(10);

                // Derecha: Totales
                row.RelativeItem(4).Element(rc => BuildSeccionTotales(rc, c, detalles, moneda));
            });

            // Pie de página pegado al contenido
            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(ColorAzulMarino);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem()
                    .Text($"Representación Impresa de {ObtenerNombreTipoComprobante(c.TipoComprobante)} - Autorizado por SUNAT")
                    .FontSize(7).FontColor(ColorTextoSuave);
                row.AutoItem().AlignRight().Text(txt =>
                {
                    txt.Span("Pág. ").FontSize(7).FontColor(ColorTextoSuave);
                    txt.CurrentPageNumber().FontSize(7).FontColor(ColorTextoSuave);
                });
            });
        });
    }

    private static void BuildTablaDetalles(IContainer container,
        List<Domain.Entities.ComprobanteDetalle> detalles, string moneda)
    {
        bool mostrarCodigo = detalles.Any(d => !string.IsNullOrWhiteSpace(d.Codigo));

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(25);
                if (mostrarCodigo) cols.ConstantColumn(45);
                cols.ConstantColumn(38);
                cols.ConstantColumn(32);
                cols.RelativeColumn();
                cols.ConstantColumn(50);
                cols.ConstantColumn(50);
                cols.ConstantColumn(55);
            });

            void TH(IContainer c, string txt) =>
                c.Background(ColorAzulMarino).Padding(4).AlignLeft()
                 .Text(txt).Bold().FontSize(8).FontColor(ColorBlanco);

            table.Header(h =>
            {
                h.Cell().Element(c => TH(c, "Item"));
                if (mostrarCodigo) h.Cell().Element(c => TH(c, "Código"));
                h.Cell().Element(c => TH(c, "Cant."));
                h.Cell().Element(c => TH(c, "Unid."));
                h.Cell().Element(c => TH(c, "Descripción"));
                h.Cell().Element(c => TH(c, "V.Unit."));
                h.Cell().Element(c => TH(c, "P.Vent."));
                h.Cell().Element(c => TH(c, "Total"));
            });

            bool par = false;
            int itemIndex = 1;
            foreach (var d in detalles)
            {
                var bg = par ? ColorBlanco : ColorGrisClaro;
                par = !par;
                bool esGratuito = d.TipoAfectacionIGV is "11" or "21" or "31";

                void TD(IContainer c, string txt, bool right = false)
                {
                    var el = c.Background(bg).Padding(3);
                    if (right) el.AlignRight().Text(txt).FontSize(8);
                    else el.Text(txt).FontSize(8);
                }

                table.Cell().Element(c => TD(c, (itemIndex++).ToString()));
                if (mostrarCodigo) table.Cell().Element(c => TD(c, d.Codigo ?? "-"));
                table.Cell().Element(c => TD(c, d.Cantidad.ToString("F2")));
                table.Cell().Element(c => TD(c, d.UnidadMedida ?? "NIU"));
                table.Cell().Element(c =>
                {
                    var desc = d.Descripcion ?? "-";
                    if (esGratuito) desc += " (GRATUITO)";
                    TD(c, desc);
                });
                // V.Unit — sin cambio
                table.Cell().Element(c => TD(c, FormatearMoneda(d.PrecioUnitario, moneda)));

                // P.Vent — precio original + descuento debajo si aplica
                table.Cell().Element(c =>
                {
                    var pVentOriginal = (d.DescuentoTotal ?? 0) > 0
                        ? Math.Round((d.PrecioVenta ?? 0) + (d.DescuentoUnitario ?? 0), 2)
                        : (d.PrecioVenta ?? 0);

                    var dsctoConIgv = (d.DescuentoUnitario ?? 0) > 0
                        ? Math.Round(d.DescuentoUnitario ?? 0, 2)
                        : 0;

                    if (dsctoConIgv > 0)
                    {
                        c.Background(bg).Padding(3).Column(col =>
                        {
                            col.Item().AlignRight()
                                .Text(FormatearMoneda(pVentOriginal, moneda))
                                .FontSize(8);
                            col.Item().AlignRight()
                                .Text($"-{FormatearMoneda(dsctoConIgv, moneda)}")
                                .FontSize(7).FontColor(ColorTextoSuave);
                        });
                    }
                    else
                    {
                        TD(c, FormatearMoneda(pVentOriginal, moneda), right: true);
                    }
                });

                // Total
                table.Cell().Element(c => TD(c, FormatearMoneda(esGratuito ? 0 : d.TotalVentaItem ?? 0, moneda)));
            }
        });
    }

    private static void BuildSeccionDetraccion(IContainer container,
        List<Domain.Entities.Detraccion> detracciones, string moneda, string? tipoComprobante)
    {
        container.Column(col =>
        {
            col.Item().Text("Detracción").Bold().FontSize(9).FontColor(ColorAzulMarino);
            col.Item().PaddingTop(2)
                .Background(ColorGrisClaro).Border(1).BorderColor(ColorGrisBorde)
                .Padding(5).Column(d =>
                {
                    foreach (var det in detracciones)
                    {
                        if (tipoComprobante == "01")
                            FilaPago(d, "Cta. Banco de la Nación", det.CuentaBancoDetraccion ?? "-");

                        FilaPago(d, "% Detracción", $"{det.PorcentajeDetraccion:F2}%");
                        FilaPago(d, "Monto Detracción", FormatearMoneda(det.MontoDetraccion ?? 0, moneda));

                        if (!string.IsNullOrEmpty(det.Observacion))
                            d.Item().Text($"Obs.: {det.Observacion}").FontSize(7).FontColor(ColorTextoSuave);
                    }
                });
        });
    }

    private static void BuildSeccionMediosPago(
        IContainer container,
        Domain.Entities.Comprobante c,
        List<Domain.Entities.Pago> pagos,
        List<Domain.Entities.Cuota> cuotas,
        string moneda)
    {
        bool esCredito = (c.TipoPago?.ToLower() ?? "") is "credito" or "crédito";
        bool tieneInicial = pagos.Any() && cuotas.Any();

        container.Column(col =>
        {
            col.Item().Text("Forma de Pago").Bold().FontSize(9).FontColor(ColorAzulMarino);

            // Cajón de ancho fijo (~140pt ≈ la mitad de la columna izquierda)
            col.Item().PaddingTop(2).Row(r =>
            {
                r.ConstantItem(200)
                    .Background(ColorGrisClaro).Border(1).BorderColor(ColorGrisBorde)
                    .Padding(5).Column(d =>
                    {
                        void FilaMedio(ColumnDescriptor col, string label, string valor) =>
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text(label).FontSize(8);
                                r.AutoItem().Text(valor).FontSize(8);
                            });

                        void FilaCuota(ColumnDescriptor col, Domain.Entities.Cuota cu) =>
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text(cu.NumeroCuota ?? "").FontSize(8);
                                r.RelativeItem().AlignCenter().Text(FormatearMoneda(cu.Monto ?? 0, moneda)).FontSize(8);
                                r.RelativeItem().AlignRight().Text($"{cu.FechaVencimiento:dd/MM/yyyy}").FontSize(8);
                            });

                        if (!esCredito)
                        {
                            if (pagos.Any())
                                foreach (var p in pagos)
                                    FilaMedio(d, p.MedioPago ?? "Efectivo", FormatearMoneda(p.Monto ?? 0, moneda));
                            else
                                FilaMedio(d, "Efectivo", FormatearMoneda(c.ImporteTotal ?? 0, moneda));
                        }
                        else if (tieneInicial)
                        {
                            foreach (var p in pagos)
                                FilaMedio(d, $"Inicial ({p.MedioPago ?? "Efectivo"})", FormatearMoneda(p.Monto ?? 0, moneda));

                            d.Item().PaddingTop(3).Text("Cuotas:").Bold().FontSize(8).FontColor(ColorAzulMarino);
                            foreach (var cu in cuotas)
                                FilaCuota(d, cu);
                        }
                        else
                        {
                            FilaMedio(d, "Monto Crédito", FormatearMoneda(c.MontoCredito ?? 0, moneda));
                            if (cuotas.Any())
                            {
                                d.Item().PaddingTop(3).Text("Cuotas:").Bold().FontSize(8).FontColor(ColorAzulMarino);
                                foreach (var cu in cuotas)
                                    FilaCuota(d, cu);
                            }
                        }
                    });
                r.RelativeItem(); // espacio vacío a la derecha
            });
        });
    }

    private static void BuildSeccionTotales(IContainer container,
        Domain.Entities.Comprobante c,
        List<Domain.Entities.ComprobanteDetalle> d,
        string moneda)
    {
        container.Column(col =>
        {
            col.Item().Text("Resumen").Bold().FontSize(9).FontColor(ColorAzulMarino);
            col.Item().PaddingTop(2).Border(1).BorderColor(ColorAzulMarino).Column(t =>
            {
                FilaTotal(t, "Op. Gravadas", FormatearMoneda(c.TotalOperacionesGravadas ?? 0, moneda));
                FilaTotal(t, "Op. Exoneradas", FormatearMoneda(c.TotalOperacionesExoneradas ?? 0, moneda));
                FilaTotal(t, "Op. Inafectas", FormatearMoneda(c.TotalOperacionesInafectas ?? 0, moneda));
                FilaTotal(t, $"I.G.V. ({d.FirstOrDefault(d => (d.PorcentajeIGV ?? 0) > 0)?.PorcentajeIGV ?? 18:G29}%)", FormatearMoneda(c.TotalIGV ?? 0, moneda));

                if ((c.TotalOperacionesGratuitas ?? 0) > 0)
                    FilaTotal(t, "Op. Gratuitas", FormatearMoneda(c.TotalOperacionesGratuitas ?? 0, moneda));
                if ((c.TotalIcbper ?? 0) > 0)
                    FilaTotal(t, "ICBPER", FormatearMoneda(c.TotalIcbper ?? 0, moneda));
                if ((c.TotalDescuentos ?? 0) > 0)
                    FilaTotal(t, "Descuentos", $"-{FormatearMoneda(c.TotalDescuentos ?? 0, moneda)}");
                if ((c.TotalOtrosCargos ?? 0) > 0)
                    FilaTotal(t, "Otros Cargos", FormatearMoneda(c.TotalOtrosCargos ?? 0, moneda));

                if ((c.DescuentoGlobal ?? 0) > 0)
                    FilaTotal(t, "Descuento Global", $"-{FormatearMoneda(c.DescuentoGlobal ?? 0, moneda)}");

                t.Item().Background(ColorAzulMarino).Padding(5).Row(r =>
                {
                    r.RelativeItem().Text("IMPORTE TOTAL").Bold().FontSize(9).FontColor(ColorBlanco);
                    r.ConstantItem(80).AlignRight()
                        .Text(FormatearMoneda(c.ImporteTotal ?? 0, moneda))
                        .Bold().FontSize(9).FontColor(ColorBlanco);
                });
            });
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // ════════════════════════════════════════════════════════════════════════
    // VALE — segunda página del ticket
    // ════════════════════════════════════════════════════════════════════════
    // Convierte un DateTime UTC almacenado en BD → hora de Lima (UTC-5)
    private static DateTime AHoraLima(DateTime dt)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Lima");
        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(dt, DateTimeKind.Utc), tz);
    }

    private static void BuildValeTicket(
        IContainer container,
        Domain.Entities.Comprobante c,
        Domain.Entities.Vale vale)
    {
        // c.HoraEmision ya viene en hora Lima desde el frontend → usar directo
        var emitido     = $"{c.FechaEmision:dd/MM/yyyy} {c.HoraEmision:HH:mm:ss}";
        var validoHasta = c.FechaEmision.AddMonths(1);

        container.Column(col =>
        {
            col.Item().Height(10); // 2 saltos antes del contenido

            // COD. VALE — centrado, negrita
            col.Item().AlignCenter()
                .Text($"COD. VALE: {c.NumeroCompleto}")
                .Bold().FontSize(8).FontColor(ColorAzulMarino);

            col.Item().Height(6);

            // Descripción — convierte <br> HTML a saltos de línea reales
            if (!string.IsNullOrWhiteSpace(vale.Descripcion))
            {
                var desc = vale.Descripcion
                    .Replace("<br><br>", "\n\n", StringComparison.OrdinalIgnoreCase)
                    .Replace("<br />",   "\n",   StringComparison.OrdinalIgnoreCase)
                    .Replace("<br/>",    "\n",   StringComparison.OrdinalIgnoreCase)
                    .Replace("<br>",     "\n",   StringComparison.OrdinalIgnoreCase);

                col.Item().Text(desc).FontSize(7).Justify();
            }

            col.Item().Height(6);

            // Emitido
            col.Item().Row(r =>
            {
                r.ConstantItem(40).Text("Emitido:").Bold().FontSize(6).FontColor(ColorAzulMarino);
                r.RelativeItem().Text(emitido).FontSize(6);
            });

            // Válido hasta
            col.Item().Row(r =>
            {
                r.ConstantItem(40).Text("Válido hasta:").Bold().FontSize(6).FontColor(ColorAzulMarino);
                r.RelativeItem().Text(validoHasta.ToString("dd/MM/yyyy")).FontSize(6);
            });
        });
    }

    // HELPERS
    // ════════════════════════════════════════════════════════════════════════
    private static void BuildFilaDato(ColumnDescriptor col, string label, string valor)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(85).Text(label + ":").Bold().FontSize(8).FontColor(ColorAzulMarino);
            r.RelativeItem().Text(valor).FontSize(8);
        });
    }

    private static void FilaPago(ColumnDescriptor col, string label, string valor)
    {
        col.Item().Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(8);
            r.ConstantItem(70).AlignRight().Text(valor).FontSize(8);
        });
    }

    private static void FilaTotal(ColumnDescriptor col, string label, string valor)
    {
        col.Item()
            .Background(ColorGrisClaro).BorderBottom(1).BorderColor(ColorGrisBorde)
            .Padding(3).Row(r =>
            {
                r.RelativeItem().Text(label).FontSize(8);
                r.ConstantItem(80).AlignRight().Text(valor).FontSize(8);
            });
    }

    private static void TicketFila(ColumnDescriptor col, string label, string valor)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(38).Text(label + ":").Bold().FontSize(6).FontColor(ColorAzulMarino);
            r.RelativeItem().Text(valor).FontSize(6);
        });
    }

    private static void TicketFilaTotal(ColumnDescriptor col, string label, string valor)
    {
        col.Item().Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(6);
            r.AutoItem().AlignRight().Text(valor).FontSize(6);
        });
    }

    private static string FormatearMoneda(decimal monto, string moneda) =>
        moneda == "USD" ? $"$ {monto:F2}" : $"S/ {monto:F2}";

    private static string ObtenerNombreTipoComprobante(string? tipo) =>
        tipo switch
        {
            "01" => "FACTURA ELECTRÓNICA",
            "03" => "BOLETA ELECTRÓNICA",
            "07" => "NOTA DE CRÉDITO",
            "08" => "NOTA DE DÉBITO",
            _ => "COMPROBANTE ELECTRÓNICO"
        };

    private static string ObtenerLabelTipoDoc(string? tipoDoc) =>
        tipoDoc switch
        {
            "0" => "Doc.",
            "01" => "DNI",
            "4" => "Carnet Ext.",
            "6" => "RUC",
            "7" => "Pasaporte",
            "A" => "C.S. Diplomatico",
            _ => "Documento"
        };

    private static string ObtenerNombreGuia(string? tipoGuia) =>
        tipoGuia switch
        {
            "09" => "Guía Remisión Remitente",
            "31" => "Guía Remisión Transportista",
            _ => tipoGuia ?? "-"
        };

    private static PageSize ResolverTamano(TamanoPdf tamano) =>
        tamano switch
        {
            TamanoPdf.A4 => PageSizes.A4,
            TamanoPdf.Carta => PageSizes.Letter,
            TamanoPdf.MediaCarta => new PageSize(595, 396),
            TamanoPdf.Ticket80mm => new PageSize(227, float.MaxValue),
            TamanoPdf.Ticket58mm => new PageSize(165, float.MaxValue),
            _ => PageSizes.A4
        };

    private static float ResolverAnchoTicket(TamanoPdf tamano) =>
    tamano == TamanoPdf.Ticket58mm ? 165f : 227f;

    private static (int w, int h) LeerDimensionesImagen(byte[] data)
    {
        // PNG: ancho y alto en bytes 16-23
        if (data.Length > 24 && data[0] == 0x89 && data[1] == 0x50)
        {
            int w = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
            int h = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
            return (w, h);
        }
        // JPEG: buscar marcador SOF0 o SOF2
        if (data.Length > 4 && data[0] == 0xFF && data[1] == 0xD8)
        {
            for (int i = 0; i < data.Length - 8; i++)
            {
                if (data[i] == 0xFF && data[i + 1] is 0xC0 or 0xC2)
                {
                    int h = (data[i + 5] << 8) | data[i + 6];
                    int w = (data[i + 7] << 8) | data[i + 8];
                    return (w, h);
                }
            }
        }
        return (400, 400); // fallback: asume cuadrado
    }

    private static (float w, float h) ResolverTamanoLogo(byte[] logoBytes)
    {
        var (pw, ph) = LeerDimensionesImagen(logoBytes);
        float ratio = (float)pw / ph;

        return ratio switch
        {
            > 1.5f => (110f, 55f),  // Rectangular horizontal (ej. 800x400)
            < 0.75f => (45f, 80f),  // Vertical               (ej. 400x600)
            _ => (60f, 60f)   // Cuadrado               (ej. 400x400)
        };
    }

    private static void BuildFilaDatoSpaced(ColumnDescriptor col, string label, string valor, float spacing = 2f, float labelWidth = 85f)
    {
        col.Item().PaddingBottom(spacing).Row(r =>
        {
            r.ConstantItem(labelWidth).Text(label + ":").Bold().FontSize(8).FontColor(ColorAzulMarino);
            r.RelativeItem().Text(valor).FontSize(8);
        });
    }

    private static byte[] GenerateQrCode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<byte>();

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        // Color Azul Marino: #1A2B4A -> R:26, G:43, B:74, A:255
        byte[] darkColor = new byte[] { 26, 43, 74, 255 };
        // Fondo Blanco -> R:255, G:255, B:255, A:255
        byte[] lightColor = new byte[] { 255, 255, 255, 255 };

        return qrCode.GetGraphic(20, darkColor, lightColor);
    }

    private static string BuildQrContent(Domain.Entities.Comprobante c)
    {
        // Formato SUNAT: RUC | TIPO | SERIE | CORRELATIVO | IGV | TOTAL | FECHA | TIPO_DOC_REC | NUM_DOC_REC | HASH
        return string.Join("|",
            c.EmpresaRuc ?? "",
            c.TipoComprobante ?? "",
            c.Serie ?? "",
            (c.Correlativo ?? 0).ToString("D8"),
            (c.TotalIGV ?? 0).ToString("F2"),
            (c.ImporteTotal ?? 0).ToString("F2"),
            c.FechaEmision.ToString("yyyy-MM-dd"),
            c.ClienteTipoDoc ?? "0",
            c.ClienteNumDoc ?? "0",
            c.CodigoHashCPE ?? ""
        );
    }
}
