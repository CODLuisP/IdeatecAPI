using System.Text;
using System.Xml.Linq;
using GuiaEntity = IdeatecAPI.Domain.Entities.GuiaRemision;
using GuiaDetalleEntity = IdeatecAPI.Domain.Entities.GuiaRemisionDetalle;

namespace IdeatecAPI.Application.Features.GuiaRemision.Services;

public interface IXmlGuiaBuilderService
{
    string BuildXml(GuiaEntity guia, List<GuiaDetalleEntity> details);
    string BuildXmlTransportista(GuiaEntity guia, List<GuiaDetalleEntity> details);

}

public class XmlGuiaBuilderService : IXmlGuiaBuilderService
{
    private static readonly XNamespace Ns = "urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Ds = "http://www.w3.org/2000/09/xmldsig#";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
    private static readonly XNamespace Sac = "urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace Qdt = "urn:oasis:names:specification:ubl:schema:xsd:QualifiedDatatypes-2";
    private static readonly XNamespace Udt = "urn:un:unece:uncefact:data:specification:UnqualifiedDataTypesSchemaModule:2";
    private static readonly XNamespace Ccts = "urn:un:unece:uncefact:documentation:2";
    private static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";

    public string BuildXml(GuiaEntity guia, List<GuiaDetalleEntity> details)
    {
        var id = $"{guia.Serie}-{guia.Correlativo:D8}";

        var root = new XElement(Ns + "DespatchAdvice",
            new XAttribute(XNamespace.Xmlns + "ds", Ds),
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "qdt", Qdt),
            new XAttribute(XNamespace.Xmlns + "ccts", Ccts),
            new XAttribute(XNamespace.Xmlns + "xsd", Xsd),
            new XAttribute(XNamespace.Xmlns + "udt", Udt),
            new XAttribute(XNamespace.Xmlns + "ext", Ext),
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi),
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XAttribute(XNamespace.Xmlns + "sac", Sac),

            // ── 1. UBLExtensions ─────────────────────────────────────────
            new XElement(Ext + "UBLExtensions",
                new XElement(Ext + "UBLExtension",
                    new XElement(Ext + "ExtensionContent"))),

            // ── 2. Versión ───────────────────────────────────────────────
            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "2.0"),

            // ── 3. ID y fechas ───────────────────────────────────────────
            new XElement(Cbc + "ID", id),
            new XElement(Cbc + "IssueDate", guia.FechaEmision.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueTime", "10:10:10"),
            new XElement(Cbc + "DespatchAdviceTypeCode", guia.TipoDoc),
            new XElement(Cbc + "Note", guia.Observacion ?? "--"),

// ← Referencia a documento relacionado (factura, orden, etc.)
!string.IsNullOrEmpty(guia.RelDocNroDoc)
    ? new XElement(Cac + "AdditionalDocumentReference",
        new XElement(Cbc + "ID", guia.RelDocNroDoc),
        new XElement(Cbc + "DocumentTypeCode",
            new XAttribute("listAgencyName", "PE:SUNAT"),
            new XAttribute("listName", "Documento relacionado al transporte"),
            new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo61"),
            guia.RelDocTipoDoc ?? "01"),
        new XElement(Cbc + "DocumentType", "Factura"),
        new XElement(Cac + "IssuerParty",
            new XElement(Cac + "PartyIdentification",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeID", "6"),
                    new XAttribute("schemeName", "Documento de Identidad"),
                    new XAttribute("schemeAgencyName", "PE:SUNAT"),
                    new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                    guia.EmpresaRuc ?? ""))))
    : null!,

            // ── 4. Firma ─────────────────────────────────────────────────
            BuildSignatureSection(guia, id),

            // ── 5. Emisor ────────────────────────────────────────────────
            BuildDespatchSupplierParty(guia),

            // ── 6. Destinatario ──────────────────────────────────────────
            BuildDeliveryCustomerParty(guia),

            // ── 7. Shipment ──────────────────────────────────────────────
            BuildShipment(guia)
        );

        // ── 8. Líneas de detalle ──────────────────────────────────────────
        for (int i = 0; i < details.Count; i++)
        {
            root.Add(BuildDespatchLine(i + 1, details[i]));
        }

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "no"),
            root);

        using var ms = new MemoryStream();
        using var writer = new System.Xml.XmlTextWriter(ms, new UTF8Encoding(false));
        writer.Formatting = System.Xml.Formatting.None;
        doc.Save(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static XElement BuildSignatureSection(GuiaEntity guia, string id) =>
        new(Cac + "Signature",
            new XElement(Cbc + "ID", id),
            new XElement(Cac + "SignatoryParty",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID", guia.EmpresaRuc)),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", guia.EmpresaRazonSocial))),
            new XElement(Cac + "DigitalSignatureAttachment",
                new XElement(Cac + "ExternalReference",
                    new XElement(Cbc + "URI", $"#{id}"))));

    private static XElement BuildDespatchSupplierParty(GuiaEntity guia) =>
        new(Cac + "DespatchSupplierParty",
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", "6"),
                        new XAttribute("schemeName", "Documento de Identidad"),
                        new XAttribute("schemeAgencyName", "PE:SUNAT"),
                        new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                        guia.EmpresaRuc!)),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", guia.EmpresaRazonSocial))));

    private static XElement BuildDeliveryCustomerParty(GuiaEntity guia) =>
        new(Cac + "DeliveryCustomerParty",
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", guia.DestinatarioTipoDoc ?? "6"),
                        new XAttribute("schemeName", "Documento de Identidad"),
                        new XAttribute("schemeAgencyName", "PE:SUNAT"),
                        new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                        guia.DestinatarioNumDoc!)),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", guia.DestinatarioRznSocial))));

    private static XElement BuildShipment(GuiaEntity guia)
    {
        var shipment = new XElement(Cac + "Shipment",
            new XElement(Cbc + "ID", "1"),
            new XElement(Cbc + "HandlingCode", guia.CodTraslado),
            new XElement(Cbc + "HandlingInstructions", guia.DesTraslado),
            new XElement(Cbc + "GrossWeightMeasure",
                new XAttribute("unitCode", guia.UndPesoTotal ?? "KGM"),
                guia.PesoTotal?.ToString("F2") ?? "0.00")
        );

        // ── Transbordo (solo modo público) ──────────────────────────────────
        if (guia.ModTraslado == "01" && guia.IndTransbordo)
        {
            shipment.Add(new XElement(Cbc + "SpecialInstructions", "TRANSBORDO"));
        }

        // ── Material peligroso ──────────────────────────────────────────────
        if (!string.IsNullOrEmpty(guia.MatPeligrosoClase))
        {
            // Formato: "CODIGO_ONU|CLASE"  →  ej: "UN1203|3"
            var valor = string.IsNullOrEmpty(guia.MatPeligrosoNroONU)
                ? guia.MatPeligrosoClase
                : $"{guia.MatPeligrosoNroONU}|{guia.MatPeligrosoClase}";

            shipment.Add(new XElement(Cbc + "SpecialInstructions", valor));
        }

        shipment.Add(BuildShipmentStage(guia));
        shipment.Add(BuildDelivery(guia));

        // ── TransportHandlingUnit (modo privado) ────────────────────────────
        if (guia.ModTraslado == "02" && !string.IsNullOrEmpty(guia.TransportistaPlaca))
        {
            shipment.Add(new XElement(Cac + "TransportHandlingUnit",
                new XElement(Cac + "TransportEquipment",
                    new XElement(Cbc + "ID", guia.TransportistaPlaca))));

            foreach (var placa in new[] { guia.PlacaSecundaria1, guia.PlacaSecundaria2 }
                .Where(p => !string.IsNullOrEmpty(p)))
            {
                shipment.Add(new XElement(Cac + "TransportHandlingUnit",
                    new XElement(Cac + "TransportEquipment",
                        new XElement(Cbc + "ID", placa))));
            }
        }

        return shipment;
    }

    private static XElement BuildShipmentStage(GuiaEntity guia)
    {
        var stage = new XElement(Cac + "ShipmentStage",
            new XElement(Cbc + "TransportModeCode", guia.ModTraslado),
            new XElement(Cac + "TransitPeriod",
                new XElement(Cbc + "StartDate",
                    guia.FecTraslado?.ToString("yyyy-MM-dd") ?? guia.FechaEmision.ToString("yyyy-MM-dd")))
        );

        // ── Modo público → CarrierParty ─────────────────────────────
        if (guia.ModTraslado == "01")
        {
            stage.Add(new XElement(Cac + "CarrierParty",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", guia.TransportistaTipoDoc ?? "6"),
                        guia.TransportistaNumDoc!)),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", guia.TransportistaRznSocial))));
        }

        // ── Modo privado → Conductor principal ───────────────────────
        if (guia.ModTraslado == "02")
        {
            stage.Add(new XElement(Cac + "DriverPerson",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeID", guia.ChoferTipoDoc ?? "1"),
                    guia.ChoferDoc),
                new XElement(Cbc + "FirstName", guia.ChoferNombres ?? ""),
                new XElement(Cbc + "FamilyName", guia.ChoferApellidos ?? ""),
                new XElement(Cbc + "JobTitle", "Principal"),
                new XElement(Cac + "IdentityDocumentReference",
                    new XElement(Cbc + "ID", guia.ChoferLicencia ?? ""))));
        }

        // ── Conductor secundario ─────────────────────────────────────
        if (guia.ModTraslado == "02" && !string.IsNullOrEmpty(guia.ChoferSecundarioDoc))
        {
            stage.Add(new XElement(Cac + "DriverPerson",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeID", guia.ChoferSecundarioTipoDoc ?? "1"),
                    guia.ChoferSecundarioDoc),
                new XElement(Cbc + "FirstName", guia.ChoferSecundarioNombres ?? ""),
                new XElement(Cbc + "FamilyName", guia.ChoferSecundarioApellidos ?? ""),
                new XElement(Cbc + "JobTitle", "Secundario"),
                new XElement(Cac + "IdentityDocumentReference",
                    new XElement(Cbc + "ID", guia.ChoferSecundarioLicencia ?? ""))));
        }

        // Conductor secundario 2  ← NUEVO
        if (guia.ModTraslado == "02" && !string.IsNullOrEmpty(guia.ChoferSecundario2Doc))
        {
            stage.Add(new XElement(Cac + "DriverPerson",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeID", guia.ChoferSecundario2TipoDoc ?? "1"),
                    guia.ChoferSecundario2Doc),
                new XElement(Cbc + "FirstName", guia.ChoferSecundario2Nombres ?? ""),
                new XElement(Cbc + "FamilyName", guia.ChoferSecundario2Apellidos ?? ""),
                new XElement(Cbc + "JobTitle", "Secundario"),
                new XElement(Cac + "IdentityDocumentReference",
                    new XElement(Cbc + "ID", guia.ChoferSecundario2Licencia ?? ""))));
        }

        return stage;
    }

    private static XElement BuildDelivery(GuiaEntity guia) =>
        new(Cac + "Delivery",
            new XElement(Cac + "DeliveryAddress",
                new XElement(Cbc + "ID", guia.LlegadaUbigeo),
                new XElement(Cbc + "AddressTypeCode",
                    new XAttribute("listID", guia.DestinatarioNumDoc ?? ""),
                    new XAttribute("listAgencyName", "PE:SUNAT"),
                    new XAttribute("listName", "Establecimientos anexos"),
                    "0"),
                new XElement(Cac + "AddressLine",
                    new XElement(Cbc + "Line", guia.LlegadaDireccion))),
            new XElement(Cac + "Despatch",
                new XElement(Cac + "DespatchAddress",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeName", "Ubigeos"),
                        new XAttribute("schemeAgencyName", "PE:INEI"),
                        guia.PartidaUbigeo!),
                    new XElement(Cbc + "AddressTypeCode",
                        new XAttribute("listID", guia.EmpresaRuc ?? ""),
                        new XAttribute("listAgencyName", "PE:SUNAT"),
                        new XAttribute("listName", "Establecimientos anexos"),
                        "0"),
                    new XElement(Cac + "AddressLine",
                        new XElement(Cbc + "Line", guia.PartidaDireccion)))));

    private static XElement BuildDespatchLine(int lineId, GuiaDetalleEntity detalle) =>
        new(Cac + "DespatchLine",
            new XElement(Cbc + "ID", lineId.ToString()),
            new XElement(Cbc + "DeliveredQuantity",
                new XAttribute("unitCode", detalle.Unidad),
                detalle.Cantidad.ToString("F2")),
            new XElement(Cac + "OrderLineReference",
                new XElement(Cbc + "LineID", lineId.ToString())),
            new XElement(Cac + "Item",
                new XElement(Cbc + "Description", detalle.Descripcion),
                new XElement(Cac + "SellersItemIdentification",
                    new XElement(Cbc + "ID", detalle.Codigo ?? lineId.ToString()))));

    public string BuildXmlTransportista(GuiaEntity guia, List<GuiaDetalleEntity> details)
    {
        var id = $"{guia.Serie}-{guia.Correlativo:D8}";

        var root = new XElement(Ns + "DespatchAdvice",
            new XAttribute(XNamespace.Xmlns + "ds", Ds),
            new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
            new XAttribute(XNamespace.Xmlns + "qdt", Qdt),
            new XAttribute(XNamespace.Xmlns + "ccts", Ccts),
            new XAttribute(XNamespace.Xmlns + "xsd", Xsd),
            new XAttribute(XNamespace.Xmlns + "udt", Udt),
            new XAttribute(XNamespace.Xmlns + "ext", Ext),
            new XAttribute(XNamespace.Xmlns + "xsi", Xsi),
            new XAttribute(XNamespace.Xmlns + "cac", Cac),
            new XAttribute(XNamespace.Xmlns + "sac", Sac),

            // ── 1. UBLExtensions ─────────────────────────────────────────
            new XElement(Ext + "UBLExtensions",
                new XElement(Ext + "UBLExtension",
                    new XElement(Ext + "ExtensionContent"))),

            // ── 2. Versión ───────────────────────────────────────────────
            new XElement(Cbc + "UBLVersionID", "2.1"),
            new XElement(Cbc + "CustomizationID", "2.0"),

            // ── 3. ID y fechas ───────────────────────────────────────────
            new XElement(Cbc + "ID", id),
            new XElement(Cbc + "IssueDate", guia.FechaEmision.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueTime", "10:10:10"),
            new XElement(Cbc + "DespatchAdviceTypeCode", "31"),
            new XElement(Cbc + "Note", guia.Observacion ?? "--"),

            // ── 4. Referencia GRE Remitente ──────────────────────────────
            !string.IsNullOrEmpty(guia.RelDocNroDoc)
                ? new XElement(Cac + "AdditionalDocumentReference",
                    new XElement(Cbc + "ID", guia.RelDocNroDoc),
                    new XElement(Cbc + "DocumentTypeCode",
                        new XAttribute("listAgencyName", "PE:SUNAT"),
                        new XAttribute("listName", "Documento relacionado al transporte"),
                        new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo61"),
                        guia.RelDocTipoDoc ?? "09"),
                    new XElement(Cbc + "DocumentType", "Guia Remision Remitente"),
                    new XElement(Cac + "IssuerParty",
                        new XElement(Cac + "PartyIdentification",
                            new XElement(Cbc + "ID",
                                new XAttribute("schemeID", "6"),
                                new XAttribute("schemeName", "Documento de Identidad"),
                                new XAttribute("schemeAgencyName", "PE:SUNAT"),
                                new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                                guia.TerceroNumDoc ?? ""))))
                : null!,

            // ── 5. Firma ─────────────────────────────────────────────────
            BuildSignatureSection(guia, id),

            // ── 6. Transportista como emisor (con CustomerAssignedAccountID) ──
            new XElement(Cac + "DespatchSupplierParty",
                new XElement(Cbc + "CustomerAssignedAccountID",
                    new XAttribute("schemeID", "6"),
                    guia.EmpresaRuc ?? ""),
                new XElement(Cac + "Party",
                    new XElement(Cac + "PartyIdentification",
                        new XElement(Cbc + "ID",
                            new XAttribute("schemeID", "6"),
                            new XAttribute("schemeName", "Documento de Identidad"),
                            new XAttribute("schemeAgencyName", "PE:SUNAT"),
                            new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                            guia.EmpresaRuc ?? "")),
                    new XElement(Cac + "PartyLegalEntity",
                        new XElement(Cbc + "RegistrationName", guia.EmpresaRazonSocial ?? "")))),

            // ── 7. Destinatario ──────────────────────────────────────────
            BuildDeliveryCustomerParty(guia),

            // ── 8. Shipment ───────────────────────────────────────────────
            BuildShipmentTransportista(guia)
        );

        // ── 9. Líneas de detalle ──────────────────────────────────────────
        for (int i = 0; i < details.Count; i++)
            root.Add(BuildDespatchLine(i + 1, details[i]));

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "no"),
            root);

        using var ms = new MemoryStream();
        using var writer = new System.Xml.XmlTextWriter(ms, new UTF8Encoding(false));
        writer.Formatting = System.Xml.Formatting.None;
        doc.Save(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static XElement BuildShipmentTransportista(GuiaEntity guia)
    {
        var stage = new XElement(Cac + "ShipmentStage",
            new XElement(Cac + "TransitPeriod",
                new XElement(Cbc + "StartDate",
                    guia.FecTraslado?.ToString("yyyy-MM-dd")
                    ?? guia.FechaEmision.ToString("yyyy-MM-dd"))),

            // ── CarrierParty vacío (requerido por schema) ─────────────────
            new XElement(Cac + "CarrierParty",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", ""))),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", ""))),

            // ── Placa via TransportMeans ──────────────────────────────────
            new XElement(Cac + "TransportMeans",
                new XElement(Cac + "RoadTransport",
                    new XElement(Cbc + "LicensePlateID", guia.TransportistaPlaca ?? ""))),

            // ── Conductor ─────────────────────────────────────────────────
            new XElement(Cac + "DriverPerson",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeID", guia.ChoferTipoDoc ?? "1"),
                    guia.ChoferDoc ?? ""),
                new XElement(Cbc + "FirstName", guia.ChoferNombres ?? ""),
                new XElement(Cbc + "FamilyName", guia.ChoferApellidos ?? ""),
                new XElement(Cbc + "JobTitle", "Principal"),
                new XElement(Cac + "IdentityDocumentReference",
                    new XElement(Cbc + "ID", guia.ChoferLicencia ?? ""))));

        return new XElement(Cac + "Shipment",
            new XElement(Cbc + "ID", "SUNAT_Envio"),
            new XElement(Cbc + "GrossWeightMeasure",
                new XAttribute("unitCode", guia.UndPesoTotal ?? "KGM"),
                guia.PesoTotal?.ToString("F2") ?? "0.00"),
            stage,

            // ── Delivery con DespatchParty (remitente original) ───────────
            new XElement(Cac + "Delivery",
                new XElement(Cac + "DeliveryAddress",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeAgencyName", "PE:INEI"),
                        new XAttribute("schemeName", "Ubigeos"),
                        guia.LlegadaUbigeo ?? ""),
                    new XElement(Cac + "AddressLine",
                        new XElement(Cbc + "Line", guia.LlegadaDireccion ?? ""))),
                new XElement(Cac + "Despatch",
                    new XElement(Cac + "DespatchAddress",
                        new XElement(Cbc + "ID",
                            new XAttribute("schemeAgencyName", "PE:INEI"),
                            new XAttribute("schemeName", "Ubigeos"),
                            guia.PartidaUbigeo ?? ""),
                        new XElement(Cac + "AddressLine",
                            new XElement(Cbc + "Line", guia.PartidaDireccion ?? ""))),
                    // ← Remitente original dentro de Despatch
                    !string.IsNullOrEmpty(guia.TerceroNumDoc)
                        ? new XElement(Cac + "DespatchParty",
                            new XElement(Cac + "PartyIdentification",
                                new XElement(Cbc + "ID",
                                    new XAttribute("schemeID", guia.TerceroTipoDoc ?? "6"),
                                    new XAttribute("schemeName", "Documento de Identidad"),
                                    new XAttribute("schemeAgencyName", "PE:SUNAT"),
                                    new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                                    guia.TerceroNumDoc)),
                            new XElement(Cac + "PartyLegalEntity",
                                new XElement(Cbc + "RegistrationName", guia.TerceroRznSocial ?? "")))
                        : null!)),

            // ── Vehículo ──────────────────────────────────────────────────
            new XElement(Cac + "TransportHandlingUnit",
                new XElement(Cac + "TransportEquipment",
                    new XElement(Cbc + "ID", guia.TransportistaPlaca ?? ""))));
    }
}