using System.Text;
using System.Xml;
using System.Xml.Linq;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Notas.Services;

public interface IXmlNoteBuilderService
{
    string BuildXml(Note note, List<NoteDetail> details, List<NoteLegend> legends);
}

public class XmlNoteBuilderService : IXmlNoteBuilderService
{
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";

    public string BuildXml(Note note, List<NoteDetail> details, List<NoteLegend> legends)
    {
        var ns = note.TipoDoc == "07"
            ? XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2")
            : XNamespace.Get("urn:oasis:names:specification:ubl:schema:xsd:DebitNote-2");
        var rootName = note.TipoDoc == "07" ? "CreditNote" : "DebitNote";
        var lineName = note.TipoDoc == "07" ? "CreditNoteLine" : "DebitNoteLine";
        var qtyName = note.TipoDoc == "07" ? "CreditedQuantity" : "DebitedQuantity";

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null));

        var root = new XElement(ns + rootName,
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "ext", Ext),

            new XElement(Ext + "UBLExtensions",
                new XElement(Ext + "UBLExtension",
                    new XElement(Ext + "ExtensionContent"))),

            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "2.0"),
            new XElement(Cbc + "ID", $"{note.Serie}-{note.Correlativo:D8}"),
            new XElement(Cbc + "IssueDate", note.FechaEmision.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueTime", note.FechaEmision.ToString("HH:mm:ss"))
        );

        foreach (var legend in legends)
        {
            root.Add(new XElement(Cbc + "Note",
                new XAttribute("languageLocaleID", legend.Code),
                legend.Value));
        }

        root.Add(new XElement(Cbc + "DocumentCurrencyCode", note.TipoMoneda));
        root.Add(new XElement(Cbc + "LineCountNumeric", details.Count));

        root.Add(
            new XElement(Cac + "DiscrepancyResponse",
                new XElement(Cbc + "ReferenceID", note.NumDocAfectado),
                new XElement(Cbc + "ResponseCode", note.TipoNotaCreditoDebito),
                new XElement(Cbc + "Description", note.MotivoNota)),
            new XElement(Cac + "BillingReference",
                new XElement(Cac + "InvoiceDocumentReference",
                    new XElement(Cbc + "ID", note.NumDocAfectado),
                    new XElement(Cbc + "DocumentTypeCode", note.TipDocAfectado)))
        );

        root.Add(BuildSignatureSection(note));
        root.Add(BuildSupplierParty(note));
        root.Add(BuildCustomerParty(note));

        // ── 7. TaxTotal global — un solo bloque con todos los subtotales ──────
        var totalIcbper = details.Sum(d => d.Icbper ?? 0);
        var totalBolsas = details.Where(d => d.Icbper > 0).Sum(d => d.Cantidad);
        var totalIgv = details.Sum(d => d.Igv);

        var taxTotalElement = new XElement(Cac + "TaxTotal",
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", note.TipoMoneda),
                (totalIgv + totalIcbper).ToString("F2")));

        foreach (var grupo in details.GroupBy(d => d.TipoAfectacionIGV))
        {
            var tipoAfe = grupo.Key;
            var baseGrupo = grupo.Sum(d => d.MtoBaseIgv);
            var igvGrupo = grupo.Sum(d => d.Igv);
            var pctGrupo = grupo.First().PorcentajeIGV;

            var (schemeId, schemeName, taxTypeCode, categoryId) = tipoAfe switch
            {
                "20" => ("9997", "EXO", "VAT", "E"),
                "30" => ("9998", "INA", "FRE", "O"),
                "40" => ("9995", "EXP", "FRE", "G"),
                _ => ("1000", "IGV", "VAT", "S")
            };

            taxTotalElement.Add(new XElement(Cac + "TaxSubtotal",
                new XElement(Cbc + "TaxableAmount",
                    new XAttribute("currencyID", note.TipoMoneda),
                    baseGrupo.ToString("F2")),
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", note.TipoMoneda),
                    igvGrupo.ToString("F2")),
                new XElement(Cac + "TaxCategory",
                    new XElement(Cbc + "ID", categoryId),
                    new XElement(Cbc + "Percent",
                        tipoAfe == "10" ? pctGrupo.ToString("F0") : "0"),
                    new XElement(Cbc + "TaxExemptionReasonCode", tipoAfe),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", schemeId),
                        new XElement(Cbc + "Name", schemeName),
                        new XElement(Cbc + "TaxTypeCode", taxTypeCode)))));
        }

        if (totalIcbper > 0)
        {
            taxTotalElement.Add(new XElement(Cac + "TaxSubtotal",
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", note.TipoMoneda),
                    totalIcbper.ToString("F2")),
                new XElement(Cac + "TaxCategory",
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", "7152"),
                        new XElement(Cbc + "Name", "ICBPER"),
                        new XElement(Cbc + "TaxTypeCode", "OTH")))));
        }

        root.Add(taxTotalElement);

        // ── 8. Total monetario ────────────────────────────────────────────────
        var totalMonetarioTag = note.TipoDoc == "07" ? "LegalMonetaryTotal" : "RequestedMonetaryTotal";
        root.Add(new XElement(Cac + totalMonetarioTag,
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", note.TipoMoneda),
                note.ValorVenta.ToString("F2")),
            new XElement(Cbc + "TaxInclusiveAmount",
                new XAttribute("currencyID", note.TipoMoneda),
                note.MtoImpVenta.ToString("F2")),
            new XElement(Cbc + "PayableAmount",
                new XAttribute("currencyID", note.TipoMoneda),
                note.MtoImpVenta.ToString("F2"))));

        // ── 9. Líneas de detalle ──────────────────────────────────────────────
        for (int i = 0; i < details.Count; i++)
            root.Add(BuildDetailLine(details[i], i + 1, lineName, qtyName, note.TipoMoneda));

        doc.Add(root);

        using var ms = new MemoryStream();
        using var xw = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true
        });
        doc.Save(xw);
        xw.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static XElement BuildSignatureSection(Note note) =>
        new(Cac + "Signature",
            new XElement(Cbc + "ID", "IDSignKG"),
            new XElement(Cac + "SignatoryParty",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID", note.EmpresaRuc)),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", note.EmpresaNombreComercial ?? note.EmpresaRazonSocial))),
            new XElement(Cac + "DigitalSignatureAttachment",
                new XElement(Cac + "ExternalReference",
                    new XElement(Cbc + "URI", "#SignatureKG"))));

    private static XElement BuildSupplierParty(Note note) =>
        new(Cac + "AccountingSupplierParty",
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", "6"),
                        note.EmpresaRuc)),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", note.EmpresaNombreComercial ?? note.EmpresaRazonSocial)),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", note.EmpresaRazonSocial),
                    new XElement(Cac + "RegistrationAddress",
                        new XElement(Cbc + "ID", note.EmpresaUbigeo ?? "150101"),
                        new XElement(Cbc + "AddressTypeCode", "0000"),
                        new XElement(Cbc + "CitySubdivisionName", "NONE"),
                        new XElement(Cbc + "CityName", note.EmpresaProvincia ?? "LIMA"),
                        new XElement(Cbc + "CountrySubentity", note.EmpresaDepartamento ?? "LIMA"),
                        new XElement(Cbc + "District", note.EmpresaDistrito ?? "LIMA"),
                        new XElement(Cac + "AddressLine",
                            new XElement(Cbc + "Line", note.EmpresaDireccion ?? "-")),
                        new XElement(Cac + "Country",
                            new XElement(Cbc + "IdentificationCode", "PE"))))));

    private static XElement BuildCustomerParty(Note note)
    {
        var schemeId = note.ClienteTipoDoc switch
        {
            "0" => "0",
            "01" => "1",
            "1" => "1",
            "04" => "4",
            "4" => "4",
            "06" => "6",
            "6" => "6",
            "07" => "7",
            "7" => "7",
            "0A" => "A",
            _ => note.ClienteTipoDoc
        };

        return new(Cac + "AccountingCustomerParty",
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", schemeId),
                        note.ClienteNumDoc)),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", note.ClienteRznSocial))));
    }

    private static XElement BuildDetailLine(NoteDetail d, int item,
        string lineName, string qtyName, string moneda)
    {
        var (schemeId, schemeName, taxTypeCode, categoryId) = d.TipoAfectacionIGV switch
        {
            "20" => ("9997", "EXO", "VAT", "E"),
            "30" => ("9998", "INA", "FRE", "O"),
            "40" => ("9995", "EXP", "FRE", "G"),
            _ => ("1000", "IGV", "VAT", "S")
        };
        var percent = d.TipoAfectacionIGV == "10"
            ? d.PorcentajeIGV.ToString("F0")
            : "0";

        var taxTotalLine = new XElement(Cac + "TaxTotal",
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", moneda),
                (d.Igv + (d.Icbper ?? 0)).ToString("F2")),
            new XElement(Cac + "TaxSubtotal",
                new XElement(Cbc + "TaxableAmount",
                    new XAttribute("currencyID", moneda),
                    d.MtoBaseIgv.ToString("F2")),
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", moneda),
                    d.Igv.ToString("F2")),
                new XElement(Cac + "TaxCategory",
                    new XElement(Cbc + "ID", categoryId),
                    new XElement(Cbc + "Percent", percent),
                    new XElement(Cbc + "TaxExemptionReasonCode", d.TipoAfectacionIGV),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", schemeId),
                        new XElement(Cbc + "Name", schemeName),
                        new XElement(Cbc + "TaxTypeCode", taxTypeCode)))));

        if (d.Icbper > 0)
        {
            taxTotalLine.Add(new XElement(Cac + "TaxSubtotal",
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", moneda),
                    d.Icbper?.ToString("F2") ?? "0.00"),
                new XElement(Cbc + "BaseUnitMeasure",
                    new XAttribute("unitCode", d.Unidad),
                    ((int)d.Cantidad).ToString()),
                new XElement(Cac + "TaxCategory",    // ← dentro del TaxSubtotal
                    new XElement(Cbc + "PerUnitAmount",
                        new XAttribute("currencyID", moneda),
                        d.FactorIcbper?.ToString("F2") ?? "0.00"),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", "7152"),
                        new XElement(Cbc + "Name", "ICBPER"),
                        new XElement(Cbc + "TaxTypeCode", "OTH")))));
        }

        var lineElement = new XElement(Cac + lineName,
            new XElement(Cbc + "ID", item.ToString()),
            new XElement(Cbc + qtyName,
                new XAttribute("unitCode", d.Unidad),
                d.Cantidad.ToString("F2")),
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", moneda),
                d.MtoValorVenta.ToString("F2")),
            new XElement(Cac + "PricingReference",
                new XElement(Cac + "AlternativeConditionPrice",
                    new XElement(Cbc + "PriceAmount",
                        new XAttribute("currencyID", moneda),
                        d.MtoPrecioUnitario.ToString("F2")),
                    new XElement(Cbc + "PriceTypeCode", "01"))),
            taxTotalLine,
            new XElement(Cac + "Item",
                new XElement(Cbc + "Description", d.Descripcion),
                new XElement(Cac + "SellersItemIdentification",
                    new XElement(Cbc + "ID", d.CodProducto ?? "-"))),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount",
                    new XAttribute("currencyID", moneda),
                    d.MtoValorUnitario.ToString("F2"))));

        return lineElement;
    }
}