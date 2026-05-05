using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Comprobante.Services;
using IdeatecAPI.Application.Features.Empresas.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IdeatecAPI.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IComprobantePdfService"/> usando QuestPDF.
/// Registrar en DI como Scoped.
/// dotnet add package QuestPDF
/// QuestPDF.Settings.License = LicenseType.Community; en Program.cs
/// </summary>
public class ComprobantePdfService : IComprobantePdfService
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string ColorAzulMarino = "#1A2B4A";
    private static readonly string ColorBlanco     = "#FFFFFF";
    private static readonly string ColorGrisClaro  = "#F5F7FA";
    private static readonly string ColorGrisBorde  = "#D0D7E3";
    private static readonly string ColorTexto      = "#1A1A1A";
    private static readonly string ColorTextoSuave = "#4A5568";

    public ComprobantePdfService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<byte[]> GenerarPdfAsync(int comprobanteId, TamanoPdf tamano = TamanoPdf.A4)
    {
        var comprobante = await _unitOfWork.Comprobantes.GetByIdAsync(comprobanteId)
            ?? throw new KeyNotFoundException($"Comprobante {comprobanteId} no encontrado.");

        var empresaEntidad = await _unitOfWork.Empresas.GetEmpresaByRucAsync(comprobante.EmpresaRuc ?? "")
            ?? throw new KeyNotFoundException($"Empresa con RUC '{comprobante.EmpresaRuc}' no encontrada.");

        var empresa = new EmpresaDto
        {
            Ruc             = empresaEntidad.Ruc,
            RazonSocial     = empresaEntidad.RazonSocial,
            NombreComercial = empresaEntidad.NombreComercial,
            Direccion       = empresaEntidad.Direccion,
            Telefono        = empresaEntidad.Telefono,
            Email           = empresaEntidad.Email,
            LogoBase64      = empresaEntidad.LogoBase64,
        };

        var datos = await _unitOfWork.Comprobantes.GetDatosCompletosByComprobanteIdAsync(comprobanteId);
        
        var detalles     = datos.Detalles.ToList();
        var pagos        = datos.Pagos.ToList();
        var cuotas       = datos.Cuotas.ToList();
        var leyendas     = datos.Leyendas.ToList();
        var guias        = datos.Guias.ToList();
        var detracciones = datos.Detracciones.ToList();

        bool esTicket = tamano == TamanoPdf.Ticket80mm || tamano == TamanoPdf.Ticket58mm;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                if (esTicket)
                {
                    page.ContinuousSize(ResolverAnchoTicket(tamano), Unit.Point);
                }
                else
                {
                    page.Size(ResolverTamano(tamano));
                }
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
                    page.Footer().Element(c => BuildFooter(c, comprobante, empresa));
                }
            });
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

                    const float maxAlto  = 28f;
                    const float maxAncho = 55f;

                    float alto  = maxAlto;
                    float ancho = alto * ratio;

                    if (ancho > maxAncho)
                    {
                        ancho = maxAncho;
                        alto  = ancho / ratio;
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
            col.Item().PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(22); // Cod
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
                    h.Cell().Element(tc => TH(tc, "Cod"));
                    h.Cell().Element(tc => TH(tc, "Cant"));
                    h.Cell().Element(tc => TH(tc, "Desc"));
                    h.Cell().Element(tc => TH(tc, "P.Vent"));
                    h.Cell().Element(tc => TH(tc, "Total"));
                });

                bool par = false;
                foreach (var d in detalles)
                {
                    var bg = par ? ColorBlanco : ColorGrisClaro;
                    par = !par;
                    bool esGratuito = d.TipoAfectacionIGV is "11" or "21" or "31";

                    void TD(IContainer tc, string txt, bool right = false)
                    {
                        var el = tc.Background(bg).Padding(2);
                        if (right) el.AlignRight().Text(txt).FontSize(6);
                        else       el.Text(txt).FontSize(6);
                    }

                    table.Cell().Element(tc => TD(tc, d.Codigo ?? "-"));
                    table.Cell().Element(tc => TD(tc, d.Cantidad.ToString("F2")));
                    table.Cell().Element(tc =>
                    {
                        var desc = d.Descripcion ?? "-";
                        if (esGratuito) desc += " (GR)";
                        TD(tc, desc);
                    });
                    table.Cell().Element(tc => TD(tc, (d.PrecioVenta ?? 0).ToString("F2")));
                    table.Cell().Element(tc => TD(tc, (esGratuito ? 0 : d.TotalVentaItem ?? 0).ToString("F2")));
                }
            });

            col.Item().PaddingTop(3).LineHorizontal(0.5f).LineColor(ColorAzulMarino);
            col.Item().Height(6);

            // 6. TOTALES
            col.Item().PaddingTop(3).Column(tot =>
            {
                TicketFilaTotal(tot, "Op. Gravadas",   FormatearMoneda(c.TotalOperacionesGravadas   ?? 0, moneda));
                TicketFilaTotal(tot, "Op. Exoneradas", FormatearMoneda(c.TotalOperacionesExoneradas ?? 0, moneda));
                TicketFilaTotal(tot, "Op. Inafectas",  FormatearMoneda(c.TotalOperacionesInafectas  ?? 0, moneda));
                TicketFilaTotal(tot, $"I.G.V. ({detalles.FirstOrDefault(d => (d.PorcentajeIGV ?? 0) > 0)?.PorcentajeIGV ?? 18:G29}%)", FormatearMoneda(c.TotalIGV ?? 0, moneda));

                if ((c.TotalIcbper ?? 0) > 0)
                    TicketFilaTotal(tot, "ICBPER", FormatearMoneda(c.TotalIcbper ?? 0, moneda));

                if ((c.TotalDescuentos ?? 0) > 0)
                    TicketFilaTotal(tot, "Descuentos", $"-{FormatearMoneda(c.TotalDescuentos ?? 0, moneda)}");

                TicketFilaTotal(tot, "Sub-Total", FormatearMoneda(c.SubTotal ?? 0, moneda));

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
            col.Item().PaddingTop(6).AlignCenter()
                .Width(45).Height(45)
                .Background(ColorGrisClaro).Border(0.5f).BorderColor(ColorGrisBorde)
                .AlignCenter().AlignMiddle()
                .Text("QR").FontSize(7).FontColor(ColorTextoSuave);

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

            if (!esCredito)
            {
                col.Item().Text("Tipo de Pago: Contado").Bold().FontSize(6).FontColor(ColorAzulMarino);
                foreach (var p in pagos)
                    TicketFilaTotal(col, p.MedioPago ?? "Efectivo", FormatearMoneda(p.Monto ?? 0, moneda));
            }
            else if (tieneInicialYCuotas)
            {
                col.Item().Text("Tipo de Pago: Crédito con inicial").Bold().FontSize(7).FontColor(ColorAzulMarino);
                foreach (var p in pagos)
                    TicketFilaTotal(col, $"Inicial ({p.MedioPago ?? "Efectivo"})", FormatearMoneda(p.Monto ?? 0, moneda));

                col.Item().PaddingTop(2).Text("Cuotas:").Bold().FontSize(6);
                foreach (var cu in cuotas)
                    TicketFilaTotal(col, $"{cu.NumeroCuota} {cu.FechaVencimiento:dd/MM/yy}", FormatearMoneda(cu.Monto ?? 0, moneda));
            }
            else
            {
                col.Item().Text("Tipo de Pago: Crédito").Bold().FontSize(7).FontColor(ColorAzulMarino);
                TicketFilaTotal(col, "Monto Crédito", FormatearMoneda(c.MontoCredito ?? 0, moneda));

                if (cuotas.Any())
                {
                    col.Item().PaddingTop(2).Text("Cuotas:").Bold().FontSize(6);
                    foreach (var cu in cuotas)
                        TicketFilaTotal(col, $"{cu.NumeroCuota} {cu.FechaVencimiento:dd/MM/yy}", FormatearMoneda(cu.Monto ?? 0, moneda));
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
    private static void BuildHeader(IContainer container, Domain.Entities.Comprobante c, EmpresaDto empresa){
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                // LOGO
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

                        // Límites para A4
                        const float maxAlto  = 70f;
                        const float maxAncho = 110f;

                        float alto, ancho;

                        if (ratio > 1.5f)
                        {
                            // Rectangular horizontal (800x400) → escala desde ancho máximo
                            ancho = maxAncho;
                            alto  = ancho / ratio; // ~55pt
                        }
                        else if (ratio < 0.75f)
                        {
                            // Vertical (400x600) → escala desde alto máximo
                            alto  = maxAlto;
                            ancho = alto * ratio; // ~46pt
                        }
                        else
                        {
                            // Cuadrado (400x400) → escala desde alto máximo
                            alto  = maxAlto;
                            ancho = alto * ratio; // ~70pt
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

                // DATOS EMPRESA — con padding derecho para no invadir el recuadro
                row.RelativeItem().PaddingLeft(6).PaddingRight(10).AlignMiddle().Column(emp =>
                {
                    emp.Item().Text(empresa.NombreComercial ?? empresa.RazonSocial)
                        .Bold().FontSize(14).FontColor(ColorAzulMarino);
                    emp.Item().Text(empresa.RazonSocial)
                        .FontSize(9).FontColor(ColorTextoSuave);

                    if (!string.IsNullOrEmpty(empresa.Direccion))
                        emp.Item().PaddingRight(20).Text(empresa.Direccion)
                        .FontSize(8).FontColor(ColorTextoSuave);
                    if (!string.IsNullOrEmpty(empresa.Telefono))
                        emp.Item().Text($"Telf: {empresa.Telefono}")
                            .FontSize(8).FontColor(ColorTextoSuave);
                    if (!string.IsNullOrEmpty(empresa.Email))
                        emp.Item().Text($"Email: {empresa.Email}")
                            .FontSize(8).FontColor(ColorTextoSuave);

                    emp.Item().Text("Web: www.ideatec.pe")
                        .FontSize(8).FontColor(ColorTextoSuave);
                });

                // RECUADRO COMPROBANTE
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
        .Padding(6)
        .Row(row =>
        {
            // Cliente (lado izquierdo)
            row.RelativeItem(6).Column(rec =>
            {
                BuildFilaDato(rec, "Cliente", c.ClienteRazonSocial ?? "-");
                BuildFilaDato(rec, tipoDocLabel, c.ClienteNumDoc ?? "-");

                if (!string.IsNullOrEmpty(c.ClienteDireccion))
                    BuildFilaDato(rec, "Dirección",
                        $"{c.ClienteDireccion}, {c.ClienteDistrito} {c.ClienteProvincia} {c.ClienteDepartamento}".Trim());

                BuildFilaDato(rec, "Fecha Emisión", $"{c.FechaEmision:dd/MM/yyyy} {c.HoraEmision:HH:mm:ss}");
                if ((c.TipoPago?.ToLower() ?? "") is "credito" or "crédito")
                    BuildFilaDato(rec, "Fecha Vencimiento", $"{c.FechaVencimiento:dd/MM/yyyy}");

                if (c.TipoMoneda != "PEN" && c.TipoCambio.HasValue)
                    BuildFilaDato(rec, "Moneda", $"{c.TipoMoneda} (T.C. S/ {c.TipoCambio:F3})");
            });

            // Documento que modifica (lado derecho)
            if (c.TipoComprobante is "07" or "08"
                && !string.IsNullOrEmpty(c.TipDocAfectado)
                && !string.IsNullOrEmpty(c.NumDocAfectado))
            {
                row.RelativeItem(4)
                    .PaddingLeft(10)
                    .Element(ct => BuildDocumentoModifica(ct, c));
            }
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
            _    => "COMPROBANTE"
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
            _    => "NOTA"
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

            // Espacio de ~3 líneas después de los ítems
            col.Item().PaddingTop(36);

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
                    left.Item().Element(lc => BuildSeccionPagos(lc, c, pagos, cuotas, moneda));

                    if (detracciones.Any())
                        left.Item().PaddingTop(6)
                            .Element(lc => BuildSeccionDetraccion(lc, detracciones, moneda, c.TipoComprobante));

                    // QR
                    left.Item().PaddingTop(8).Width(70).Height(70)
                        .Background(ColorGrisClaro).Border(1).BorderColor(ColorGrisBorde)
                        .AlignCenter().AlignMiddle()
                        .Text("QR").FontSize(9).FontColor(ColorTextoSuave);
                });

                row.ConstantItem(10);

                // Derecha: Totales
                row.RelativeItem(4).Element(rc => BuildSeccionTotales(rc, c,detalles, moneda));
            });
        });
    }

    private static void BuildTablaDetalles(IContainer container,
        List<Domain.Entities.ComprobanteDetalle> detalles, string moneda)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(45);
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
                h.Cell().Element(c => TH(c, "Código"));
                h.Cell().Element(c => TH(c, "Cant."));
                h.Cell().Element(c => TH(c, "Unid."));
                h.Cell().Element(c => TH(c, "Descripción"));
                h.Cell().Element(c => TH(c, "V.Unit."));
                h.Cell().Element(c => TH(c, "P.Vent."));
                h.Cell().Element(c => TH(c, "Total"));
            });

            bool par = false;
            foreach (var d in detalles)
            {
                var bg = par ? ColorBlanco : ColorGrisClaro;
                par = !par;
                bool esGratuito = d.TipoAfectacionIGV is "11" or "21" or "31";

                void TD(IContainer c, string txt, bool right = false)
                {
                    var el = c.Background(bg).Padding(3);
                    if (right) el.AlignRight().Text(txt).FontSize(8);
                    else       el.Text(txt).FontSize(8);
                }

                table.Cell().Element(c => TD(c, d.Codigo ?? "-"));
                table.Cell().Element(c => TD(c, d.Cantidad.ToString("F2")));
                table.Cell().Element(c => TD(c, d.UnidadMedida ?? "NIU"));
                table.Cell().Element(c =>
                {
                    var desc = d.Descripcion ?? "-";
                    if (esGratuito) desc += " (GRATUITO)";
                    TD(c, desc);
                });
                table.Cell().Element(c => TD(c, FormatearMoneda(d.PrecioUnitario, moneda)));
                table.Cell().Element(c => TD(c, FormatearMoneda(d.PrecioVenta ?? 0, moneda)));
                table.Cell().Element(c => TD(c, FormatearMoneda(esGratuito ? 0 : d.TotalVentaItem ?? 0, moneda)));
            }
        });
    }

    private static void BuildSeccionPagos(IContainer container,
        Domain.Entities.Comprobante c,
        List<Domain.Entities.Pago> pagos,
        List<Domain.Entities.Cuota> cuotas,
        string moneda)
    {
        container.Column(col =>
        {
            col.Item().Text("Condición de Pago").Bold().FontSize(9).FontColor(ColorAzulMarino);
            col.Item().PaddingTop(2)
                .Background(ColorGrisClaro).Border(1).BorderColor(ColorGrisBorde)
                .Padding(5).Column(p =>
                {
                    bool tieneInicialYCuotas = pagos.Any() && cuotas.Any();
                    bool esCredito = (c.TipoPago?.ToLower() ?? "") is "credito" or "crédito";

                    if (!esCredito)
                    {
                        p.Item().Text("Tipo: Contado").Bold().FontSize(8);
                        foreach (var pago in pagos)
                            FilaPago(p, pago.MedioPago ?? "Efectivo", FormatearMoneda(pago.Monto ?? 0, moneda));
                    }
                    else if (tieneInicialYCuotas)
                    {
                        p.Item().Text("Tipo: Crédito con inicial").Bold().FontSize(8);
                        foreach (var pago in pagos)
                            FilaPago(p, $"Inicial ({pago.MedioPago ?? "Efectivo"})", FormatearMoneda(pago.Monto ?? 0, moneda));

                        p.Item().PaddingTop(3).Text("Cuotas:").Bold().FontSize(8);
                        foreach (var cu in cuotas)
                            FilaPago(p, $"{cu.NumeroCuota} - Vcto: {cu.FechaVencimiento:dd/MM/yyyy}", FormatearMoneda(cu.Monto ?? 0, moneda));
                    }
                    else
                    {
                        p.Item().Text("Tipo: Crédito").Bold().FontSize(8);
                        FilaPago(p, "Monto Crédito", FormatearMoneda(c.MontoCredito ?? 0, moneda));

                        if (cuotas.Any())
                        {
                            p.Item().PaddingTop(3).Text("Cuotas:").Bold().FontSize(8);
                            foreach (var cu in cuotas)
                                FilaPago(p, $"{cu.NumeroCuota} - Vcto: {cu.FechaVencimiento:dd/MM/yyyy}", FormatearMoneda(cu.Monto ?? 0, moneda));
                        }
                    }
                });
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
                FilaTotal(t, "Op. Gravadas",   FormatearMoneda(c.TotalOperacionesGravadas   ?? 0, moneda));
                FilaTotal(t, "Op. Exoneradas", FormatearMoneda(c.TotalOperacionesExoneradas ?? 0, moneda));
                FilaTotal(t, "Op. Inafectas",  FormatearMoneda(c.TotalOperacionesInafectas  ?? 0, moneda));
                FilaTotal(t, $"I.G.V. ({d.FirstOrDefault(d => (d.PorcentajeIGV ?? 0) > 0)?.PorcentajeIGV ?? 18:G29}%)", FormatearMoneda(c.TotalIGV ?? 0, moneda));

                if ((c.TotalOperacionesGratuitas ?? 0) > 0)
                    FilaTotal(t, "Op. Gratuitas", FormatearMoneda(c.TotalOperacionesGratuitas ?? 0, moneda));
                if ((c.TotalIcbper ?? 0) > 0)
                    FilaTotal(t, "ICBPER", FormatearMoneda(c.TotalIcbper ?? 0, moneda));
                if ((c.TotalDescuentos ?? 0) > 0)
                    FilaTotal(t, "Descuentos", $"-{FormatearMoneda(c.TotalDescuentos ?? 0, moneda)}");
                if ((c.TotalOtrosCargos ?? 0) > 0)
                    FilaTotal(t, "Otros Cargos", FormatearMoneda(c.TotalOtrosCargos ?? 0, moneda));

                FilaTotal(t, "Sub-Total", FormatearMoneda(c.SubTotal ?? 0, moneda));

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

    private static void BuildFooter(IContainer container,
        Domain.Entities.Comprobante c, EmpresaDto empresa)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(ColorAzulMarino);
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

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════
    private static void BuildFilaDato(ColumnDescriptor col, string label, string valor)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(85).Text(label + " :").Bold().FontSize(8).FontColor(ColorAzulMarino);
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
            _    => "COMPROBANTE ELECTRÓNICO"
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
            _   => "Documento"
    };

    private static string ObtenerNombreGuia(string? tipoGuia) =>
        tipoGuia switch
        {
            "09" => "Guía Remisión Remitente",
            "31" => "Guía Remisión Transportista",
            _    => tipoGuia ?? "-"
        };

    private static PageSize ResolverTamano(TamanoPdf tamano) =>
        tamano switch
        {
            TamanoPdf.A4         => PageSizes.A4,
            TamanoPdf.Carta      => PageSizes.Letter,
            TamanoPdf.MediaCarta => new PageSize(595, 396),
            TamanoPdf.Ticket80mm => new PageSize(227, float.MaxValue),
            TamanoPdf.Ticket58mm => new PageSize(165, float.MaxValue),
            _                    => PageSizes.A4
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
        float ratio  = (float)pw / ph;

        return ratio switch
        {
            > 1.5f  => (110f, 55f),  // Rectangular horizontal (ej. 800x400)
            < 0.75f => (45f,  80f),  // Vertical               (ej. 400x600)
            _       => (60f,  60f)   // Cuadrado               (ej. 400x400)
        };
    }
}
