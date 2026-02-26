using System.Xml.Linq;
using BajaEntity = IdeatecAPI.Domain.Entities.ComunicacionBaja;
using BajaDetalleEntity = IdeatecAPI.Domain.Entities.ComunicacionBajaDetalle;
using System.Text;

namespace IdeatecAPI.Application.Features.ComunicacionBaja.Services;

public interface IXmlBajaBuilderService
{
    string BuildXml(BajaEntity baja, List<BajaDetalleEntity> details);
}

public class XmlBajaBuilderService : IXmlBajaBuilderService
{
    private static readonly XNamespace Ns = "urn:sunat:names:specification:ubl:peru:schema:xsd:VoidedDocuments-1";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Ds = "http://www.w3.org/2000/09/xmldsig#";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
    private static readonly XNamespace Sac = "urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";

    public string BuildXml(BajaEntity baja, List<BajaDetalleEntity> details)
    {
        // Nombre del archivo: RUC-RA-YYYYMMDD-Correlativo
        var serie = baja.FecGeneracion.ToString("yyyyMMdd");
        var id = $"RA-{serie}-{baja.Correlativo}";

        var root = new XElement(Ns + "VoidedDocuments",
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "ds", Ds),
            new XAttribute(XNamespace.Xmlns + "ext", Ext),
            new XAttribute(XNamespace.Xmlns + "sac", Sac),
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi),

            // ── 1. UBLExtensions ─────────────────────────────────────────
            new XElement(Ext + "UBLExtensions",
                new XElement(Ext + "UBLExtension",
                    new XElement(Ext + "ExtensionContent"))),

            // ── 2. Versión ───────────────────────────────────────────────
            new XElement(Cbc + "UBLVersionID", "2.0"),
            new XElement(Cbc + "CustomizationID", "1.0"),

            // ── 3. ID ────────────────────────────────────────────────────
            new XElement(Cbc + "ID", id),

            // ── 4. Fechas ────────────────────────────────────────────────
            new XElement(Cbc + "ReferenceDate", baja.FecGeneracion.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueDate", baja.FecComunicacion.ToString("yyyy-MM-dd")),

            // ── 5. Firma ─────────────────────────────────────────────────
            BuildSignatureSection(baja),

            // ── 6. Emisor ────────────────────────────────────────────────
            BuildSupplierParty(baja)
        );

        // ── 7. Líneas de detalle ──────────────────────────────────────────
        for (int i = 0; i < details.Count; i++)
        {
            root.Add(BuildVoidedLine(i + 1, details[i]));
        }

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "no"),
            root);

        using var ms = new MemoryStream();
        using var writer = new System.Xml.XmlTextWriter(ms, new UTF8Encoding(false)); // ← sin BOM
        writer.Formatting = System.Xml.Formatting.None; 
        doc.Save(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static XElement BuildSignatureSection(BajaEntity baja) =>
        new(Cac + "Signature",
            new XElement(Cbc + "ID", "IDSignKG"),
            new XElement(Cac + "SignatoryParty",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID", baja.EmpresaRuc)),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", baja.EmpresaRazonSocial))),
            new XElement(Cac + "DigitalSignatureAttachment",
                new XElement(Cac + "ExternalReference",
                    new XElement(Cbc + "URI", "#SignatureSP"))));

    private static XElement BuildSupplierParty(BajaEntity baja) =>
        new(Cac + "AccountingSupplierParty",
            new XElement(Cbc + "CustomerAssignedAccountID", baja.EmpresaRuc),
            new XElement(Cbc + "AdditionalAccountID", "6"),
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", baja.EmpresaRazonSocial))));

    private static XElement BuildVoidedLine(int lineId, BajaDetalleEntity detalle) =>
        new(Sac + "VoidedDocumentsLine",
            new XElement(Cbc + "LineID", lineId.ToString()),
            new XElement(Cbc + "DocumentTypeCode", detalle.TipoDoc),
            new XElement(Sac + "DocumentSerialID", detalle.Serie),
            new XElement(Sac + "DocumentNumberID", detalle.Correlativo),
            new XElement(Sac + "VoidReasonDescription", detalle.DesMotivoBaja));
}