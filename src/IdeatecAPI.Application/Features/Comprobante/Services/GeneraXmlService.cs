using System.Text;
using System.Xml;
using System.Xml.Linq;
using IdeatecAPI.Application.Features.Comprobante.DTOs;

namespace IdeatecAPI.Application.Features.Comprobante.Services;

public interface IComprobanteXmlService
{
    XmlResultado GenerarXml(GenerarComprobanteDTO dto);
}

public class XmlResultado
{
    public bool Exitoso { get; set; }
    public string? XmlString { get; set; }
    public string? XmlBase64 { get; set; }
    public string? Error { get; set; }
}

public class GeneraXmlService : IComprobanteXmlService
{
    private static readonly XNamespace Ns  = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";

    public XmlResultado GenerarXml(GenerarComprobanteDTO dto)
    {
        try
        {
            var xml       = BuildXml(dto);
            var xmlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));
            return new XmlResultado { Exitoso = true, XmlString = xml, XmlBase64 = xmlBase64 };
        }
        catch (Exception ex)
        {
            return new XmlResultado { Exitoso = false, Error = ex.Message };
        }
    }

    private string BuildXml(GenerarComprobanteDTO dto)
    {
        var emp    = dto.Company;
        var cli    = dto.Cliente;
        var moneda = dto.TipoMoneda;
        var correlativo = dto.Correlativo.PadLeft(8, '0');

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null));

        var root = new XElement(Ns + "Invoice",
            new XAttribute(XNamespace.Xmlns + "xsi",  "http://www.w3.org/2001/XMLSchema-instance"),
            new XAttribute(XNamespace.Xmlns + "xsd",  "http://www.w3.org/2001/XMLSchema"),
            new XAttribute(XNamespace.Xmlns + "cac",  Cac),
            new XAttribute(XNamespace.Xmlns + "cbc",  Cbc),
            new XAttribute(XNamespace.Xmlns + "ext",  Ext),

            // ── UBLExtensions ────────────────────────────────────────────
            new XElement(Ext + "UBLExtensions",
                new XElement(Ext + "UBLExtension",
                    new XElement(Ext + "ExtensionContent"))),

            // ── Cabecera ─────────────────────────────────────────────────
            new XElement(Cbc + "UBLVersionID", dto.UblVersion),
            new XElement(Cbc + "CustomizationID",
                new XAttribute("schemeAgencyName", "PE:SUNAT"),
                "2.0"),
            new XElement(Cbc + "ProfileID",
                new XAttribute("schemeAgencyName", "PE:SUNAT"),
                new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo17"),
                dto.TipoOperacion),
            new XElement(Cbc + "ID", $"{dto.Serie}-{correlativo}"),
            new XElement(Cbc + "IssueDate",  dto.FechaEmision.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "IssueTime",  dto.HoraEmision.ToString("HH:mm:ss")),
            new XElement(Cbc + "DueDate",    dto.FechaVencimiento.ToString("yyyy-MM-dd")),
            new XElement(Cbc + "InvoiceTypeCode",
                new XAttribute("listAgencyName", "PE:SUNAT"),
                new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01"),
                new XAttribute("listID", dto.TipoOperacion),
                dto.TipoComprobante),
            // ← LEYENDA AQUÍ, antes de DocumentCurrencyCode
            dto.Legends != null
               ? new XElement(Cbc + "Note",
                     new XAttribute("languageLocaleID", dto.Legends.Code),
                     dto.Legends.Value)
               : null!,
            new XElement(Cbc + "DocumentCurrencyCode",
                new XAttribute("listID", "ISO 4217 Alpha"),
                new XAttribute("listAgencyName", "United Nations Economic Commission for Europe"),
                moneda),
            new XElement(Cbc + "LineCountNumeric", dto.Details.Count)
        );

        // ── Firma placeholder ─────────────────────────────────────────────
        root.Add(BuildSignatureSection(dto, correlativo));

        // ── Emisor ────────────────────────────────────────────────────────
        root.Add(BuildSupplierParty(emp));

        // ── Cliente ───────────────────────────────────────────────────────
        root.Add(BuildCustomerParty(cli));

        // ── Forma de pago ─────────────────────────────────────────────────

        var MontoCredito = dto.Cuotas?
            .Sum(p => p.Monto ?? 0m) ?? 0m;
        

        if (dto.TipoPago?.ToLower() == "contado")
        {
            root.Add(new XElement(Cac + "PaymentTerms",
                new XElement(Cbc + "ID", "FormaPago"),
                new XElement(Cbc + "PaymentMeansID", "Contado"),
                new XElement(Cbc + "Amount",
                    new XAttribute("currencyID", moneda),
                    dto.MtoImpVenta.ToString("F2"))));
        }
        else
        {
            root.Add(new XElement(Cac + "PaymentTerms",
                new XElement(Cbc + "ID", "FormaPago"),
                new XElement(Cbc + "PaymentMeansID", "Credito"),
                new XElement(Cbc + "Amount",
                    new XAttribute("currencyID", moneda),
                    MontoCredito.ToString("F2"))));

            if (dto.TipoComprobante == "01" && dto.Cuotas != null && dto.Cuotas.Any())
            {
                foreach (var cuota in dto.Cuotas)
                {
                    root.Add(new XElement(Cac + "PaymentTerms",
                        new XElement(Cbc + "ID", "FormaPago"),
                        new XElement(Cbc + "PaymentMeansID", cuota.NumeroCuota),
                        new XElement(Cbc + "Amount",
                            new XAttribute("currencyID", moneda),
                            (cuota.Monto ?? 0).ToString("F2")),
                        new XElement(Cbc + "PaymentDueDate",
                            cuota.FechaVencimiento.ToString("yyyy-MM-dd"))));
                }
            }
        }

        // ── Impuestos cabecera ────────────────────────────────────────────
        root.Add(BuildTaxTotal(dto, moneda));

        // ── Totales monetarios ────────────────────────────────────────────
        root.Add(BuildLegalMonetaryTotal(dto, moneda));

        // ── Líneas de detalle ─────────────────────────────────────────────
        for (int i = 0; i < dto.Details.Count; i++)
        {
            root.Add(BuildInvoiceLine(dto.Details[i], i + 1, moneda));
        }

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

    // ── Firma ─────────────────────────────────────────────────────────────────
    private static XElement BuildSignatureSection(GenerarComprobanteDTO dto, string correlativo) =>
        new(Cac + "Signature",
            new XElement(Cbc + "ID", $"{dto.Serie}-{correlativo}"),
            new XElement(Cac + "SignatoryParty",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID", dto.Company.NumeroDocumento)),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", dto.Company.RazonSocial))),
            new XElement(Cac + "DigitalSignatureAttachment",
                new XElement(Cac + "ExternalReference",
                    new XElement(Cbc + "URI", "#SignatureSP"))));

    // ── Emisor ────────────────────────────────────────────────────────────────
    private static XElement BuildSupplierParty(EmpresaDTO emp) =>
        new(Cac + "AccountingSupplierParty",
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", "6"),
                        new XAttribute("schemeAgencyName", "PE:SUNAT"),
                        new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                        emp.NumeroDocumento)),
                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name", emp.RazonSocial)),
                new XElement(Cac + "PartyTaxScheme",
                    new XElement(Cbc + "RegistrationName", emp.RazonSocial),
                    new XElement(Cbc + "CompanyID",
                        new XAttribute("schemeID", "6"),
                        new XAttribute("schemeAgencyName", "PE:SUNAT"),
                        emp.NumeroDocumento),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID",
                            new XAttribute("schemeID", "UN/ECE 5153"),
                            new XAttribute("schemeAgencyName", "PE:SUNAT"),
                            "1000"),
                        new XElement(Cbc + "Name", "IGV"),
                        new XElement(Cbc + "TaxTypeCode", "VAT"))),
                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName", emp.RazonSocial),
                    new XElement(Cac + "RegistrationAddress",
                        new XElement(Cbc + "ID",
                            new XAttribute("schemeName", "Ubigeos"),
                            new XAttribute("schemeAgencyName", "PE:INEI"),
                            emp.Ubigeo),
                        new XElement(Cbc + "AddressTypeCode",
                            new XAttribute("listAgencyName", "PE:SUNAT"),
                            new XAttribute("listName", "Establecimientos anexos"),
                            emp.EstablecimientoAnexo),
                        new XElement(Cbc + "CityName", emp.Provincia),
                        new XElement(Cbc + "CountrySubentity", emp.Departamento),
                        new XElement(Cbc + "District", emp.Distrito),
                        new XElement(Cac + "AddressLine",
                            new XElement(Cbc + "Line", emp.DireccionLineal)),
                        new XElement(Cac + "Country",
                            new XElement(Cbc + "IdentificationCode", "PE"))))));

    // ── Cliente ───────────────────────────────────────────────────────────────
   private static XElement BuildCustomerParty(ClienteDTO cli)
    {
        bool clienteVarios = cli.TipoDocumento == "0";
        bool esDni = cli.TipoDocumento == "1";

        return new XElement(Cac + "AccountingCustomerParty",
            new XElement(Cac + "Party",
                new XElement(Cac + "PartyIdentification",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", clienteVarios ? "0" : cli.TipoDocumento ?? "0"),
                        new XAttribute("schemeAgencyName", "PE:SUNAT"),
                        new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"),
                        clienteVarios ? "0" : cli.NumeroDocumento ?? "0")),

                new XElement(Cac + "PartyName",
                    new XElement(Cbc + "Name",
                        clienteVarios ? "CLIENTES VARIOS" : cli.RazonSocial ?? "-")),

                new XElement(Cac + "PartyTaxScheme",
                    new XElement(Cbc + "RegistrationName",
                        clienteVarios ? "CLIENTES VARIOS" : cli.RazonSocial ?? "-"),
                    new XElement(Cbc + "CompanyID",
                        new XAttribute("schemeID", clienteVarios ? "0" : cli.TipoDocumento ?? "0"),
                        new XAttribute("schemeAgencyName", "PE:SUNAT"),
                        clienteVarios ? "0" : cli.NumeroDocumento ?? "0"),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID",
                            new XAttribute("schemeID", "UN/ECE 5153"),
                            new XAttribute("schemeAgencyName", "PE:SUNAT"),
                            "1000"),
                        new XElement(Cbc + "Name", "IGV"),
                        new XElement(Cbc + "TaxTypeCode", "VAT"))),

                new XElement(Cac + "PartyLegalEntity",
                    new XElement(Cbc + "RegistrationName",
                        clienteVarios ? "CLIENTES VARIOS" : cli.RazonSocial ?? "-"),

                    // 🔹 Enviar dirección SOLO si NO es DNI y NO es cliente varios
                    (!esDni && !clienteVarios)
                    ? new XElement(Cac + "RegistrationAddress",
                        new XElement(Cbc + "ID",
                            new XAttribute("schemeName", "Ubigeos"),
                            new XAttribute("schemeAgencyName", "PE:INEI"),
                            cli.Ubigeo ?? "-"),
                        new XElement(Cbc + "CityName", cli.Provincia ?? "-"),
                        new XElement(Cbc + "CountrySubentity", cli.Departamento ?? "-"),
                        new XElement(Cbc + "District", cli.Distrito ?? "-"),
                        new XElement(Cac + "AddressLine",
                            new XElement(Cbc + "Line", cli.DireccionLineal ?? "-")),
                        new XElement(Cac + "Country",
                            new XElement(Cbc + "IdentificationCode", "PE")))
                    : null
                )
            )
        );
    }


    // ── Impuestos cabecera ────────────────────────────────────────────────────
    private static XElement BuildTaxTotal(GenerarComprobanteDTO dto, string moneda)
    {
        var taxTotal = new XElement(Cac + "TaxTotal",
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", moneda),
                dto.TotalImpuestos.ToString("F2")));

        if (dto.MtoOperGravadas > 0)
            taxTotal.Add(BuildTaxSubtotal(moneda, dto.MtoOperGravadas, dto.MtoIGV, "S", "1000", "IGV", "VAT"));

        if (dto.MtoOperExoneradas > 0)
            taxTotal.Add(BuildTaxSubtotal(moneda, dto.MtoOperExoneradas, 0, "E", "9997", "EXO", "VAT"));

        if (dto.MtoOperInafectas > 0)
            taxTotal.Add(BuildTaxSubtotal(moneda, dto.MtoOperInafectas, 0, "O", "9998", "INA", "FRE"));

        return taxTotal;
    }

    private static XElement BuildTaxSubtotal(string moneda, decimal taxable, decimal tax,
        string catId, string schemeId, string taxName, string taxType) =>
        new(Cac + "TaxSubtotal",
            new XElement(Cbc + "TaxableAmount",
                new XAttribute("currencyID", moneda),
                taxable.ToString("F2")),
            new XElement(Cbc + "TaxAmount",
                new XAttribute("currencyID", moneda),
                tax.ToString("F2")),
            new XElement(Cac + "TaxCategory",
                new XElement(Cbc + "ID",
                    new XAttribute("schemeID", "UN/ECE 5305"),
                    new XAttribute("schemeAgencyName", "United Nations Economic Commission for Europe"),
                    catId),
                new XElement(Cac + "TaxScheme",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeID", "UN/ECE 5153"),
                        new XAttribute("schemeAgencyName", "PE:SUNAT"),
                        schemeId),
                    new XElement(Cbc + "Name", taxName),
                    new XElement(Cbc + "TaxTypeCode", taxType))));

    // ── Totales monetarios ────────────────────────────────────────────────────
    private static XElement BuildLegalMonetaryTotal(GenerarComprobanteDTO dto, string moneda)
    {
        var total = new XElement(Cac + "LegalMonetaryTotal",
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", moneda),
                dto.ValorVenta.ToString("F2")));

        if (dto.TotalDescuentos > 0)
            total.Add(new XElement(Cbc + "AllowanceTotalAmount",
                new XAttribute("currencyID", moneda),
                dto.TotalDescuentos.ToString("F2")));

        if (dto.TotalOtrosCargos > 0)
            total.Add(new XElement(Cbc + "ChargeTotalAmount",
                new XAttribute("currencyID", moneda),
                dto.TotalOtrosCargos.ToString("F2")));

        total.Add(new XElement(Cbc + "TaxInclusiveAmount",
            new XAttribute("currencyID", moneda),
            dto.SubTotal.ToString("F2")));

        total.Add(new XElement(Cbc + "PayableAmount",
            new XAttribute("currencyID", moneda),
            dto.MtoImpVenta.ToString("F2")));

        return total;
    }

    // ── Línea de detalle ──────────────────────────────────────────────────────
    private static XElement BuildInvoiceLine(DetalleFacturaDTO d, int item, string moneda)
    {
        var (catId, schemeId, taxName, taxType) = GetCodigosTributo(d.TipoAfectacionIGV);

        return new XElement(Cac + "InvoiceLine",
            new XElement(Cbc + "ID", item.ToString()),
            new XElement(Cbc + "InvoicedQuantity",
                new XAttribute("unitCode", d.UnidadMedida ?? "NIU"),
                new XAttribute("unitCodeListID", "UN/ECE rec 20"),
                d.Cantidad.ToString("F2")),
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", moneda),
                d.ValorVenta.ToString("F2")),
            new XElement(Cac + "PricingReference",
                new XElement(Cac + "AlternativeConditionPrice",
                    new XElement(Cbc + "PriceAmount",
                        new XAttribute("currencyID", moneda),
                        d.PrecioVenta.ToString("F2")),
                    new XElement(Cbc + "PriceTypeCode",
                        new XAttribute("listAgencyName", "PE:SUNAT"),
                        new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo16"),
                        "01"))),
            new XElement(Cac + "TaxTotal",
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", moneda),
                    d.MontoIGV.ToString("F2")),
                new XElement(Cac + "TaxSubtotal",
                    new XElement(Cbc + "TaxableAmount",
                        new XAttribute("currencyID", moneda),
                        d.BaseIgv.ToString("F2")),
                    new XElement(Cbc + "TaxAmount",
                        new XAttribute("currencyID", moneda),
                        d.MontoIGV.ToString("F2")),
                    new XElement(Cac + "TaxCategory",
                        new XElement(Cbc + "ID",
                            new XAttribute("schemeID", "UN/ECE 5305"),
                            new XAttribute("schemeAgencyName", "United Nations Economic Commission for Europe"),
                            catId),
                        new XElement(Cbc + "Percent", d.PorcentajeIGV.ToString("F2")),
                        new XElement(Cbc + "TaxExemptionReasonCode",
                            new XAttribute("listAgencyName", "PE:SUNAT"),
                            new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo07"),
                            d.TipoAfectacionIGV),
                        new XElement(Cac + "TaxScheme",
                            new XElement(Cbc + "ID",
                                new XAttribute("schemeID", "UN/ECE 5153"),
                                new XAttribute("schemeAgencyName", "PE:SUNAT"),
                                schemeId),
                            new XElement(Cbc + "Name", taxName),
                            new XElement(Cbc + "TaxTypeCode", taxType))))),
            new XElement(Cac + "Item",
                new XElement(Cbc + "Description", d.Descripcion),
                new XElement(Cac + "SellersItemIdentification",
                    new XElement(Cbc + "ID", d.Codigo)),
                new XElement(Cac + "CommodityClassification",
                    new XElement(Cbc + "ItemClassificationCode",
                        new XAttribute("listID", "UNSPSC"),
                        new XAttribute("listAgencyName", "GS1 US"),
                        "10191509"))),
            new XElement(Cac + "Price",
                new XElement(Cbc + "PriceAmount",
                    new XAttribute("currencyID", moneda),
                    d.PrecioUnitario.ToString("F2"))));
    }

    private static (string catId, string schemeId, string taxName, string taxType) GetCodigosTributo(string? tipAfeIgv) =>
        tipAfeIgv switch
        {
            "10" => ("S", "1000", "IGV", "VAT"),
            "20" => ("E", "9997", "EXO", "VAT"),
            "30" => ("O", "9998", "INA", "FRE"),
            _    => ("S", "1000", "IGV", "VAT")
        };
}