using System.Text;
using System.Xml;
using System.Xml.Linq;
using IdeatecAPI.Application.Features.ResumenComprobante.DTO;

namespace IdeatecAPI.Application.Features.ResumenComprobante.Services;

public interface IResumenXmlService
{
    XmlResumenResultado GenerarResumenXml(ObtenerResumenComprobanteDTO dto);
}

public class XmlResumenResultado
{
    public bool Exitoso { get; set; }
    public string? XmlString { get; set; }
    public string? XmlBase64 { get; set; }
    public string? Error { get; set; }
}

public class GeneraResumenXmlService : IResumenXmlService
{
    private static readonly XNamespace Ns  = "urn:sunat:names:specification:ubl:peru:schema:xsd:SummaryDocuments-1";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
    private static readonly XNamespace Sac = "urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1";

    public XmlResumenResultado GenerarResumenXml(ObtenerResumenComprobanteDTO dto)
    {
        try
        {
            var xml       = BuildXml(dto);
            var xmlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));
            return new XmlResumenResultado { Exitoso = true, XmlString = xml, XmlBase64 = xmlBase64 };
        }
        catch (Exception ex)
        {
            return new XmlResumenResultado { Exitoso = false, Error = ex.Message };
        }
    }

    private string BuildXml(ObtenerResumenComprobanteDTO dto)
    {
        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null));

        var root = new XElement(Ns + "SummaryDocuments",
            new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
            new XAttribute(XNamespace.Xmlns + "xsd", "http://www.w3.org/2001/XMLSchema"),
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "ext", Ext),
            new XAttribute(XNamespace.Xmlns + "sac", Sac),

            // ── UBLExtensions (placeholder para firma digital) ────────────
            new XElement(Ext + "UBLExtensions",
                new XElement(Ext + "UBLExtension",
                    new XElement(Ext + "ExtensionContent"))),

            // ── Cabecera ──────────────────────────────────────────────────
            new XElement(Cbc + "UBLVersionID", "2.0"),
            new XElement(Cbc + "CustomizationID",
                new XAttribute("schemeAgencyName", "PE:SUNAT"),
                "1.1"),
            new XElement(Cbc + "ID", dto.Identificador),
            new XElement(Cbc + "ReferenceDate",
                dto.FechaEmisionDocumentos.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueDate",
                dto.FechaGeneracion.ToString("yyyy-MM-dd")),

            // ── Firma placeholder ─────────────────────────────────────────
            new XElement(Cac + "Signature",
                new XElement(Cbc + "ID", dto.Identificador),
                new XElement(Cac + "SignatoryParty",
                    new XElement(Cac + "PartyIdentification",
                        new XElement(Cbc + "ID", dto.EmpresaRuc)),
                    new XElement(Cac + "PartyName",
                        new XElement(Cbc + "Name", dto.EmpresaRazonSocial))),
                new XElement(Cac + "DigitalSignatureAttachment",
                    new XElement(Cac + "ExternalReference",
                        new XElement(Cbc + "URI", dto.Identificador)))), // ← sin #

            // ── Emisor ────────────────────────────────────────────────────
            BuildSupplierParty(dto)
        );

        // ── Líneas de detalle ─────────────────────────────────────────────
        foreach (var detalle in dto.DetallesResumen)
            root.Add(BuildSummaryDocumentsLine(detalle));

        doc.Add(root);

        using var ms = new MemoryStream();
        using var xw = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Encoding           = new UTF8Encoding(false),
            Indent             = true,
            OmitXmlDeclaration = false
        });
        doc.Save(xw);
        xw.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    // ── Emisor ────────────────────────────────────────────────────────────────
    private static XElement BuildSupplierParty(ObtenerResumenComprobanteDTO dto) =>
        new(Cac + "AccountingSupplierParty",
            new XElement(Cbc + "CustomerAssignedAccountID", dto.EmpresaRuc),
            new XElement(Cbc + "AdditionalAccountID", "6"),
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", dto.EmpresaRazonSocial))));

    // ── Línea de resumen ──────────────────────────────────────────────────────
    private static XElement BuildSummaryDocumentsLine(ObtenerResumenDetalleDTO d)
    {
        bool esAnulacion = d.CodigoCondicion == "3";
        bool esGratuito  = d.TotalGratuito > 0 && d.MontoTotalVenta == 0;

        var linea = new XElement(Sac + "SummaryDocumentsLine",
            new XElement(Cbc + "LineID", d.LineID.ToString()),
            new XElement(Cbc + "DocumentTypeCode", d.TipoComprobante),
            new XElement(Cbc + "ID", $"{d.Serie}-{d.Correlativo}"),
            new XElement(Cac + "Status",
                new XElement(Cbc + "ConditionCode", d.CodigoCondicion)));

        // ── TotalAmount ───────────────────────────────────────────────────
        linea.Add(new XElement(Sac + "TotalAmount",
            new XAttribute("currencyID", d.Moneda),
            (esAnulacion ? 0 : d.MontoTotalVenta).ToString("F2")));

        // ── BillingPayments y TaxTotal (solo si NO es anulación) ─────────
        if (!esAnulacion)
        {
            if (d.TotalGravado > 0)
                linea.Add(BuildBillingPayment(d.Moneda, d.TotalGravado, "01"));

            if (d.TotalExonerado > 0)
                linea.Add(BuildBillingPayment(d.Moneda, d.TotalExonerado, "02"));

            if (d.TotalInafecto > 0)
                linea.Add(BuildBillingPayment(d.Moneda, d.TotalInafecto, "03"));

            if (d.TotalGratuito > 0)
                linea.Add(BuildBillingPayment(d.Moneda, d.TotalGratuito, "04"));

            linea.Add(BuildTaxTotal(d, esGratuito));
        }
        return linea;
    }

    // ── BillingPayment ────────────────────────────────────────────────────────
    private static XElement BuildBillingPayment(string moneda, decimal monto, string instructionId) =>
        new(Sac + "BillingPayment",
            new XElement(Cbc + "PaidAmount",
                new XAttribute("currencyID", moneda),
                monto.ToString("F2")),
            new XElement(Cbc + "InstructionID", instructionId));

    // ── TaxTotal ──────────────────────────────────────────────────────────────
    private static XElement BuildTaxTotal(ObtenerResumenDetalleDTO d, bool esGratuito)
    {
        var igvReal = esGratuito ? 0 : d.TotalIGV;

        return new XElement(Cac + "TaxTotal",
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", d.Moneda),
                igvReal.ToString("F2")),
            new XElement(Cac + "TaxSubtotal",
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", d.Moneda),
                    esGratuito
                        ? d.IGVReferencial.ToString("F2")
                        : igvReal.ToString("F2")),
                new XElement(Cac + "TaxCategory",
                    new XElement(Cbc + "Percent", "18.00"),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", "1000"),
                        new XElement(Cbc + "Name", "IGV"),
                        new XElement(Cbc + "TaxTypeCode", "VAT")))));
    }
}