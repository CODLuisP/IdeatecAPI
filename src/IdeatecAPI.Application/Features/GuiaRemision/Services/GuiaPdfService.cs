using IdeatecAPI.Application.Common.Interfaces.Persistence;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IdeatecAPI.Infrastructure.Services;

public class GuiaPdfService : IGuiaPdfService
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string ColorAzulMarino = "#1A2B4A";
    private static readonly string ColorBlanco = "#FFFFFF";
    private static readonly string ColorGrisClaro = "#F5F7FA";
    private static readonly string ColorGrisBorde = "#D0D7E3";
    private static readonly string ColorTexto = "#1A1A1A";
    private static readonly string ColorTextoSuave = "#4A5568";

    public GuiaPdfService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<byte[]> GenerarPdfAsync(int guiaId)
    {
        var guia = await _unitOfWork.Guias.GetByIdAsync(guiaId)
            ?? throw new KeyNotFoundException($"Guía {guiaId} no encontrada.");

        var empresa = await _unitOfWork.Empresas.GetEmpresaByRucAsync(guia.EmpresaRuc ?? "")
            ?? throw new KeyNotFoundException($"Empresa con RUC '{guia.EmpresaRuc}' no encontrada.");

        var detalles = (await _unitOfWork.GuiaDetalles.GetByGuiaIdAsync(guiaId)).ToList();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(15, Unit.Millimetre);
                page.MarginBottom(15, Unit.Millimetre);
                page.MarginLeft(15, Unit.Millimetre);
                page.MarginRight(15, Unit.Millimetre);
                page.DefaultTextStyle(x =>
                    x.FontFamily("Arial").FontSize(9).FontColor(ColorTexto));

                page.Header().Element(c => BuildHeader(c, guia, empresa));
                page.Content().Element(c => BuildContent(c, guia, detalles));
                page.Footer().Element(c => BuildFooter(c, guia));
            });
        });

        return doc.GeneratePdf();
    }

    // ── HEADER ────────────────────────────────────────────────────────────

    private static void BuildHeader(IContainer container,
        Domain.Entities.GuiaRemision g,
        Domain.Entities.Empresa empresa)
    {
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
                        row.ConstantItem(70).Padding(2).Image(logoBytes).FitArea();
                    }
                    catch { row.ConstantItem(70); }
                }
                else { row.ConstantItem(70); }

                // DATOS EMPRESA
                row.RelativeItem().PaddingLeft(6).Column(emp =>
                {
                    emp.Item().Text(empresa.NombreComercial ?? empresa.RazonSocial)
                        .Bold().FontSize(14).FontColor(ColorAzulMarino);
                    emp.Item().Text(empresa.RazonSocial)
                        .FontSize(9).FontColor(ColorTextoSuave);
                    if (!string.IsNullOrEmpty(empresa.Direccion))
                        emp.Item().Text(empresa.Direccion).FontSize(8).FontColor(ColorTextoSuave);
                    if (!string.IsNullOrEmpty(empresa.Telefono))
                        emp.Item().Text($"Telf: {empresa.Telefono}").FontSize(8).FontColor(ColorTextoSuave);
                    if (!string.IsNullOrEmpty(empresa.Email))
                        emp.Item().Text($"Email: {empresa.Email}").FontSize(8).FontColor(ColorTextoSuave);
                });

                // RECUADRO TIPO / SERIE / NÚMERO
                row.ConstantItem(160).Border(1).BorderColor(ColorAzulMarino).Column(box =>
                {
                    box.Item().Background(ColorGrisClaro).Padding(5).AlignCenter()
                        .Text($"R.U.C. {empresa.Ruc}").Bold().FontSize(9).FontColor(ColorAzulMarino);
                    box.Item().Background(ColorAzulMarino).Padding(5).AlignCenter()
                        .Text(ObtenerNombreTipo(g.TipoDoc))
                        .Bold().FontSize(9).FontColor(ColorBlanco);
                    box.Item().Background(ColorGrisClaro).Padding(5).AlignCenter()
                        .Text($"N° {g.Serie}-{g.Correlativo:D8}")
                        .Bold().FontSize(9).FontColor(ColorAzulMarino);
                });
            });

            col.Item().PaddingTop(4).LineHorizontal(1).LineColor(ColorAzulMarino);
            col.Item().Height(4);
        });
    }

    // ── CONTENT ───────────────────────────────────────────────────────────

    private static void BuildContent(IContainer container,
        Domain.Entities.GuiaRemision g,
        List<Domain.Entities.GuiaRemisionDetalle> detalles)
    {
        container.Column(col =>
        {
            // ── 1. Datos generales del traslado ──────────────────────────
            col.Item().Background(ColorGrisClaro).Border(1).BorderColor(ColorGrisBorde)
                .Padding(6).Column(sec =>
                {
                    sec.Item().Text("Datos del Traslado")
                        .Bold().FontSize(9).FontColor(ColorAzulMarino);
                    sec.Item().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            FilaDato(c, "Fecha Emisión", g.FechaEmision.ToString("dd/MM/yyyy"));
                            FilaDato(c, "Fecha Traslado", g.FecTraslado?.ToString("dd/MM/yyyy") ?? g.FechaEmision.ToString("dd/MM/yyyy"));
                            FilaDato(c, "Motivo", $"{g.CodTraslado} - {g.DesTraslado}");
                            FilaDato(c, "Modalidad", g.ModTraslado == "01" ? "Transporte Público" : "Transporte Privado");
                        });
                        r.RelativeItem().Column(c =>
                        {
                            FilaDato(c, "Peso Total", $"{g.PesoTotal:F2} {g.UndPesoTotal ?? "KGM"}");
                            if (g.IndTransbordo)
                                FilaDato(c, "Transbordo", "Sí");
                            if (g.IndVehiculoM1L)
                                FilaDato(c, "Vehículo", "Categoría M1 o L");
                            if (!string.IsNullOrEmpty(g.RelDocNroDoc))
                                FilaDato(c, "Doc. Relacionado", $"{ObtenerNombreDoc(g.RelDocTipoDoc)} {g.RelDocNroDoc}");
                            if (!string.IsNullOrEmpty(g.Observacion))
                                FilaDato(c, "Observación", g.Observacion);
                        });
                    });
                });

            col.Item().PaddingTop(6);

            // ── 2. Destinatario ───────────────────────────────────────────
            col.Item().Background(ColorGrisClaro).Border(1).BorderColor(ColorGrisBorde)
                .Padding(6).Column(sec =>
                {
                    sec.Item().Text("Destinatario")
                        .Bold().FontSize(9).FontColor(ColorAzulMarino);
                    sec.Item().PaddingTop(4).Column(c =>
                    {
                        FilaDato(c, "Razón Social / Nombre", g.DestinatarioRznSocial ?? "-");
                        FilaDato(c, ObtenerLabelTipoDoc(g.DestinatarioTipoDoc), g.DestinatarioNumDoc ?? "-");
                    });
                });

            col.Item().PaddingTop(6);

            // ── 3. Puntos de partida y llegada ────────────────────────────
            col.Item().Row(row =>
            {
                // Partida
                row.RelativeItem().Background(ColorGrisClaro).Border(1).BorderColor(ColorGrisBorde)
    .Padding(6).Column(sec =>
    {
        sec.Item().Text("Punto de Partida")
            .Bold().FontSize(9).FontColor(ColorAzulMarino);
        sec.Item().PaddingTop(4).Column(c =>
        {
            FilaDato(c, "Ubigeo", g.PartidaUbigeo ?? "-");
            FilaDato(c, "Departamento", g.PartidaDepartamento ?? "-");
            FilaDato(c, "Provincia", g.PartidaProvincia ?? "-");
            FilaDato(c, "Distrito", g.PartidaDistrito ?? "-");
            FilaDato(c, "Dirección", g.PartidaDireccion ?? "-");
        });
    });

                row.ConstantItem(8);

                // Llegada
                row.RelativeItem().Background(ColorGrisClaro).Border(1).BorderColor(ColorGrisBorde)
    .Padding(6).Column(sec =>
    {
        sec.Item().Text("Punto de Llegada")
            .Bold().FontSize(9).FontColor(ColorAzulMarino);
        sec.Item().PaddingTop(4).Column(c =>
        {
            FilaDato(c, "Ubigeo", g.LlegadaUbigeo ?? "-");
            FilaDato(c, "Departamento", g.LlegadaDepartamento ?? "-");
            FilaDato(c, "Provincia", g.LlegadaProvincia ?? "-");
            FilaDato(c, "Distrito", g.LlegadaDistrito ?? "-");
            FilaDato(c, "Dirección", g.LlegadaDireccion ?? "-");
        });
    });
            });

            col.Item().PaddingTop(6);

            // ── 4. Datos de transporte ────────────────────────────────────
            col.Item().Background(ColorGrisClaro).Border(1).BorderColor(ColorGrisBorde)
                .Padding(6).Column(sec =>
                {
                    sec.Item().Text("Datos de Transporte")
                        .Bold().FontSize(9).FontColor(ColorAzulMarino);
                    sec.Item().PaddingTop(4).Row(r =>
                    {
                        // Columna izquierda: transportista / vehículo
                        r.RelativeItem().Column(c =>
                        {
                            if (g.ModTraslado == "01" && !string.IsNullOrEmpty(g.TransportistaNumDoc))
                            {
                                FilaDato(c, "Transportista", g.TransportistaRznSocial ?? "-");
                                FilaDato(c, "RUC Transportista", g.TransportistaNumDoc);
                            }
                            if (!string.IsNullOrEmpty(g.TransportistaPlaca))
                                FilaDato(c, "Placa Principal", g.TransportistaPlaca);
                            if (!string.IsNullOrEmpty(g.PlacaSecundaria1))
                                FilaDato(c, "Placa Secundaria 1", g.PlacaSecundaria1);
                            if (!string.IsNullOrEmpty(g.PlacaSecundaria2))
                                FilaDato(c, "Placa Secundaria 2", g.PlacaSecundaria2);
                            if (!string.IsNullOrEmpty(g.AutorizacionVehiculoNumero))
                                FilaDato(c, "Aut. Especial Vehículo", $"{g.AutorizacionVehiculoEntidad} - {g.AutorizacionVehiculoNumero}");
                        });

                        // Columna derecha: conductor
                        r.RelativeItem().Column(c =>
                        {
                            if (!string.IsNullOrEmpty(g.ChoferDoc))
                            {
                                FilaDato(c, "Conductor", $"{g.ChoferApellidos} {g.ChoferNombres}".Trim());
                                FilaDato(c, "Doc. Conductor", g.ChoferDoc);
                                FilaDato(c, "Licencia", g.ChoferLicencia ?? "-");
                            }
                            if (!string.IsNullOrEmpty(g.ChoferSecundarioDoc))
                            {
                                FilaDato(c, "Conductor 2", $"{g.ChoferSecundarioApellidos} {g.ChoferSecundarioNombres}".Trim());
                                FilaDato(c, "Doc. Conductor 2", g.ChoferSecundarioDoc);
                                FilaDato(c, "Licencia 2", g.ChoferSecundarioLicencia ?? "-");
                            }
                            if (!string.IsNullOrEmpty(g.ChoferSecundario2Doc))
                            {
                                FilaDato(c, "Conductor 3", $"{g.ChoferSecundario2Apellidos} {g.ChoferSecundario2Nombres}".Trim());
                                FilaDato(c, "Doc. Conductor 3", g.ChoferSecundario2Doc);
                                FilaDato(c, "Licencia 3", g.ChoferSecundario2Licencia ?? "-");
                            }
                        });
                    });
                });

            col.Item().PaddingTop(8);

            // ── 5. Tabla de bienes ────────────────────────────────────────
            col.Item().Text("Bienes a Trasladar")
                .Bold().FontSize(9).FontColor(ColorAzulMarino);
            col.Item().PaddingTop(4)
                .Element(c => BuildTablaDetalles(c, detalles));

            // ── 6. Totales ────────────────────────────────────────────────
            col.Item().PaddingTop(6).AlignRight().Width(200)
                .Background(ColorAzulMarino).Padding(6).Row(r =>
                {
                    r.RelativeItem().Text("Peso Bruto Total:")
                        .Bold().FontSize(9).FontColor(ColorBlanco);
                    r.AutoItem().AlignRight()
                        .Text($"{g.PesoTotal:F2} {g.UndPesoTotal ?? "KGM"}")
                        .Bold().FontSize(9).FontColor(ColorBlanco);
                });
        });
    }

    // ── TABLA DETALLES ────────────────────────────────────────────────────

    private static void BuildTablaDetalles(IContainer container,
        List<Domain.Entities.GuiaRemisionDetalle> detalles)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(50);  // Código
                cols.RelativeColumn();    // Descripción
                cols.ConstantColumn(55);  // Cantidad
                cols.ConstantColumn(55);  // Unidad
            });

            void TH(IContainer c, string txt) =>
                c.Background(ColorAzulMarino).Padding(4).AlignCenter()
                 .Text(txt).Bold().FontSize(8).FontColor(ColorBlanco);

            table.Header(h =>
            {
                h.Cell().Element(c => TH(c, "Código"));
                h.Cell().Element(c => TH(c, "Descripción"));
                h.Cell().Element(c => TH(c, "Cantidad"));
                h.Cell().Element(c => TH(c, "Unidad"));
            });

            bool par = false;
            foreach (var d in detalles)
            {
                var bg = par ? ColorBlanco : ColorGrisClaro;
                par = !par;

                void TD(IContainer c, string txt, bool center = false, bool right = false)
                {
                    var el = c.Background(bg).Padding(3);
                    if (center) el.AlignCenter().Text(txt).FontSize(8);
                    else if (right) el.AlignRight().Text(txt).FontSize(8);
                    else el.Text(txt).FontSize(8);
                }

                table.Cell().Element(c => TD(c, d.Codigo ?? "-"));
                table.Cell().Element(c => TD(c, d.Descripcion));
                table.Cell().Element(c => TD(c, d.Cantidad.ToString("F2"), center: true));
                table.Cell().Element(c => TD(c, d.Unidad, center: true));
            }
        });
    }

    // ── FOOTER ────────────────────────────────────────────────────────────

    private static void BuildFooter(IContainer container,
        Domain.Entities.GuiaRemision g)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(1).LineColor(ColorAzulMarino);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem()
                    .Text($"Representación Impresa de {ObtenerNombreTipo(g.TipoDoc)} - Autorizado por SUNAT")
                    .FontSize(7).FontColor(ColorTextoSuave);
                row.AutoItem().AlignRight().Text(txt =>
                {
                    txt.Span("Pág. ").FontSize(7).FontColor(ColorTextoSuave);
                    txt.CurrentPageNumber().FontSize(7).FontColor(ColorTextoSuave);
                });
            });
        });
    }

    // ── HELPERS ───────────────────────────────────────────────────────────

    private static void FilaDato(ColumnDescriptor col, string label, string valor)
    {
        col.Item().Row(r =>
        {
            r.ConstantItem(120).Text(label + " :").Bold().FontSize(8).FontColor(ColorAzulMarino);
            r.RelativeItem().Text(valor).FontSize(8);
        });
    }

    private static string ObtenerNombreTipo(string? tipo) =>
        tipo switch
        {
            "09" => "GUÍA DE REMISIÓN REMITENTE",
            "31" => "GUÍA DE REMISIÓN TRANSPORTISTA",
            _ => "GUÍA DE REMISIÓN"
        };

    private static string ObtenerNombreDoc(string? tipo) =>
        tipo switch
        {
            "01" => "Factura",
            "03" => "Boleta de Venta",
            "09" => "Guía Remisión Remitente",
            "07" => "Nota de Crédito",
            "08" => "Nota de Débito",
            _ => tipo ?? "-"
        };

    private static string ObtenerLabelTipoDoc(string? tipoDoc) =>
        tipoDoc switch
        {
            "1" => "DNI",
            "6" => "RUC",
            "01" => "DNI",
            "06" => "RUC",
            "4" => "Carnet Ext.",
            "7" => "Pasaporte",
            _ => "Documento"
        };
}