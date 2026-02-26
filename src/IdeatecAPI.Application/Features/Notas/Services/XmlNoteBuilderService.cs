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
    // Namespaces UBL 2.1
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

        // ── 1. UBLExtensions + Signature + UBLVersionID + CustomizationID ────
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

        // ── 2. Leyendas van después de IssueTime según guía SUNAT ────────────
        foreach (var legend in legends)
        {
            root.Add(new XElement(Cbc + "Note",
                new XAttribute("languageLocaleID", legend.Code),
                legend.Value));
        }

        // ── 3. DocumentCurrencyCode y LineCountNumeric ────────────────────────
        root.Add(new XElement(Cbc + "DocumentCurrencyCode", note.TipoMoneda));
        root.Add(new XElement(Cbc + "LineCountNumeric", details.Count));

        // ── 4. Referencia al comprobante afectado ─────────────────────────────
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

        // ── 5. Emisor ─────────────────────────────────────────────────────────
        root.Add(BuildSupplierParty(note));

        // ── 6. Cliente ────────────────────────────────────────────────────────
        root.Add(BuildCustomerParty(note));

        // ── 7. Totales de impuestos ───────────────────────────────────────────
        root.Add(BuildTaxTotal(note.MtoOperGravadas, note.MtoIGV, note.TipoMoneda));

        // ── 8. Total monetario ────────────────────────────────────────────────
        var totalMonetarioTag = note.TipoDoc == "07" ? "LegalMonetaryTotal" : "RequestedMonetaryTotal";

        root.Add(new XElement(Cac + totalMonetarioTag,
    new XElement(Cbc + "LineExtensionAmount",
        new XAttribute("currencyID", note.TipoMoneda),
        note.MtoOperGravadas.ToString("F2")),
    new XElement(Cbc + "TaxInclusiveAmount",
        new XAttribute("currencyID", note.TipoMoneda),
        note.MtoImpVenta.ToString("F2")),
    new XElement(Cbc + "PayableAmount",
        new XAttribute("currencyID", note.TipoMoneda),
        note.MtoImpVenta.ToString("F2"))));

        // ── 9. Líneas de detalle ──────────────────────────────────────────────
        for (int i = 0; i < details.Count; i++)
        {
            root.Add(BuildDetailLine(details[i], i + 1, lineName, qtyName, note.TipoMoneda));
        }

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

    private static XElement BuildCustomerParty(Note note) =>
        new(Cac + "AccountingCustomerParty",
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", note.ClienteTipoDoc),
                        note.ClienteNumDoc)),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", note.ClienteRznSocial))));

    private static XElement BuildTaxTotal(decimal baseImponible, decimal igv, string moneda) =>
        new(Cac + "TaxTotal",
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", moneda),
                igv.ToString("F2")),
            new XElement(Cac + "TaxSubtotal",
                new XElement(Cbc + "TaxableAmount",
                    new XAttribute("currencyID", moneda),
                    baseImponible.ToString("F2")),
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", moneda),
                    igv.ToString("F2")),
                new XElement(Cac + "TaxCategory",
                    new XElement(Cbc + "ID", "S"),
                    new XElement(Cbc + "Percent", "18"),
                    new XElement(Cbc + "TaxExemptionReasonCode", "10"),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", "1000"),
                        new XElement(Cbc + "Name", "IGV"),
                        new XElement(Cbc + "TaxTypeCode", "VAT")))));

    private static XElement BuildDetailLine(NoteDetail d, int item, string lineName, string qtyName, string moneda) =>
        new(Cac + lineName,
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
            new XElement(Cac + "TaxTotal",
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", moneda),
                    d.Igv.ToString("F2")),
                new XElement(Cac + "TaxSubtotal",
                    new XElement(Cbc + "TaxableAmount",
                        new XAttribute("currencyID", moneda),
                        d.MtoBaseIgv.ToString("F2")),
                    new XElement(Cbc + "TaxAmount",
                        new XAttribute("currencyID", moneda),
                        d.Igv.ToString("F2")),
                    new XElement(Cac + "TaxCategory",
                        new XElement(Cbc + "ID", "S"),
                        new XElement(Cbc + "Percent", d.PorcentajeIGV.ToString("F0")),
                        new XElement(Cbc + "TaxExemptionReasonCode", d.TipAfeIgv.ToString()),
                        new XElement(Cac + "TaxScheme",
                            new XElement(Cbc + "ID", "1000"),
                            new XElement(Cbc + "Name", "IGV"),
                            new XElement(Cbc + "TaxTypeCode", "VAT"))))),
            new XElement(Cac + "Item",
                new XElement(Cbc + "Description", d.Descripcion),
                new XElement(Cac + "SellersItemIdentification",
                    new XElement(Cbc + "ID", d.CodProducto ?? "-"))),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount",
                    new XAttribute("currencyID", moneda),
                    d.MtoValorUnitario.ToString("F2"))));
}