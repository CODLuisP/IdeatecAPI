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
            // LEYENDAS
            dto.Legends?.Select(l =>
                new XElement(Cbc + "Note",
                    new XAttribute("languageLocaleID", l.Code),
                    l.Value
                )
            ),
            new XElement(Cbc + "DocumentCurrencyCode",
                new XAttribute("listID", "ISO 4217 Alpha"),
                new XAttribute("listAgencyName", "United Nations Economic Commission for Europe"),
                moneda),
            new XElement(Cbc + "LineCountNumeric", dto.Details.Count)
            
        );

        // ── Guías de remisión ────────────────────────────────────────────
        if (dto.Guias != null && dto.Guias.Any())
        {
            foreach (var guia in dto.Guias)
            {
                root.Add(new XElement(Cac + "DespatchDocumentReference",
                    new XElement(Cbc + "ID", guia.GuiaNumeroCompleto),
                    new XElement(Cbc + "DocumentTypeCode", guia.GuiaTipoDoc)));
            }
        }

        // ── Firma placeholder ─────────────────────────────────────────────
        root.Add(BuildSignatureSection(dto, correlativo));

        // ── Emisor ────────────────────────────────────────────────────────
        root.Add(BuildSupplierParty(emp));

        // ── Cliente ───────────────────────────────────────────────────────
        root.Add(BuildCustomerParty(cli!));

        // ── Detracción (dinámica) ────────────────────────────────────────────
        if (dto.Detracciones != null && dto.Detracciones.Any())
        {
            foreach (var detraccion in dto.Detracciones)
            {
                root.Add(new XElement(Cac + "PaymentMeans",
                    new XElement(Cbc + "ID", "Detraccion"),
                    new XElement(Cbc + "PaymentMeansCode", detraccion.CodigoMedioPago ?? "001"),
                    new XElement(Cac + "PayeeFinancialAccount",
                        new XElement(Cbc + "ID", detraccion.CuentaBancoDetraccion))));

                root.Add(new XElement(Cac + "PaymentTerms",
                    new XElement(Cbc + "ID",
                        new XAttribute("schemeName", "SUNAT:Codigo de detraccion"),
                        new XAttribute("schemeAgencyName", "PE:SUNAT"),
                        new XAttribute("schemeURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo54"),
                        detraccion.CodigoBienDetraccion),
                    new XElement(Cbc + "PaymentPercent",
                        (detraccion.PorcentajeDetraccion ?? 0).ToString("F2")),
                    new XElement(Cbc + "Amount",
                        new XAttribute("currencyID", moneda),
                        (detraccion.MontoDetraccion ?? 0).ToString("F2"))));
            }
        }

        // ── Forma de pago ────────────────────────────────────────────────
        if (dto.TipoPago?.ToLower() == "contado")
        {
            root.Add(new XElement(Cac + "PaymentTerms",
                new XElement(Cbc + "ID", "FormaPago"),
                new XElement(Cbc + "PaymentMeansID", "Contado"),
                new XElement(Cbc + "Amount", 
                    new XAttribute("currencyID", moneda),
                    dto.ImporteTotal.ToString("F2"))));
        }
        else
        {
            root.Add(new XElement(Cac + "PaymentTerms",
                new XElement(Cbc + "ID", "FormaPago"),
                new XElement(Cbc + "PaymentMeansID", "Credito"),
                new XElement(Cbc + "Amount",
                    new XAttribute("currencyID", moneda),
                    dto.MontoCredito.ToString("F2"))));

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

        // ── Tipo de cambio DESPUÉS de PaymentTerms ────────────────────────
        if (dto.TipoMoneda != "PEN" && dto.TipoCambio.HasValue)
        {
            root.Add(new XElement(Cac + "TaxExchangeRate",
                new XElement(Cbc + "SourceCurrencyCode", dto.TipoMoneda),
                new XElement(Cbc + "TargetCurrencyCode", "PEN"),
                new XElement(Cbc + "CalculationRate",
                    dto.TipoCambio.Value.ToString("F3")),
                new XElement(Cbc + "Date",
                    dto.FechaEmision.ToString("yyyy-MM-dd"))));
        }

        // ── Descuento global ─────────────────────────────────────────────
        if (dto.DescuentoGlobal > 0)
        {
            if (dto.CodigoTipoDescGlobal == "03")
            {
                root.Add(
                    new XElement(Cac + "AllowanceCharge",
                        new XElement(Cbc + "ChargeIndicator", "false"),
                        new XElement(Cbc + "AllowanceChargeReasonCode",
                            new XAttribute("listAgencyName", "PE:SUNAT"),
                            new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo53"),
                            "03"),
                        new XElement(Cbc + "Amount",
                            new XAttribute("currencyID", moneda),
                            dto.DescuentoGlobal.ToString("F2")),
                        new XElement(Cbc + "BaseAmount",
                            new XAttribute("currencyID", moneda),
                            dto.SubTotal.ToString("F2"))
                    )
                );
            }
            if (dto.CodigoTipoDescGlobal == "02")
            {
                root.Add(
                    new XElement(Cac + "AllowanceCharge",
                        new XElement(Cbc + "ChargeIndicator", "false"),
                        new XElement(Cbc + "AllowanceChargeReasonCode",
                            new XAttribute("listAgencyName", "PE:SUNAT"),
                            new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo53"),
                            "02"),
                        new XElement(Cbc + "Amount",
                            new XAttribute("currencyID", moneda),
                            dto.DescuentoGlobal.ToString("F2")),
                        new XElement(Cbc + "BaseAmount",
                            new XAttribute("currencyID", moneda),
                            (dto.TotalOperacionesGravadas + dto.DescuentoGlobal).ToString("F2"))
                    )
                );
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

        if (dto.TotalOperacionesGravadas > 0)
            taxTotal.Add(BuildTaxSubtotal(moneda, dto.TotalOperacionesGravadas, dto.TotalIGV, "S", "1000", "IGV", "VAT"));

        if (dto.TotalOperacionesExoneradas > 0)
            taxTotal.Add(BuildTaxSubtotal(moneda, dto.TotalOperacionesExoneradas, 0, "E", "9997", "EXO", "VAT"));

        if (dto.TotalOperacionesInafectas > 0)
            taxTotal.Add(BuildTaxSubtotal(moneda, dto.TotalOperacionesInafectas, 0, "O", "9998", "INA", "FRE"));
        
        if (dto.TotalOperacionesGratuitas > 0)
            taxTotal.Add(BuildTaxSubtotal(moneda, dto.TotalOperacionesGratuitas, dto.TotalIgvGratuitas, "Z", "9996", "GRA", "FRE"));

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

    // TaxInclusiveAmount ANTES de AllowanceTotalAmount
    total.Add(new XElement(Cbc + "TaxInclusiveAmount",
        new XAttribute("currencyID", moneda),
        dto.SubTotal.ToString("F2")));

    if (dto.TotalDescuentos > 0)
        total.Add(new XElement(Cbc + "AllowanceTotalAmount",
            new XAttribute("currencyID", moneda),
            dto.TotalDescuentos.ToString("F2")));

    if (dto.TotalOtrosCargos > 0)
        total.Add(new XElement(Cbc + "ChargeTotalAmount",
            new XAttribute("currencyID", moneda),
            dto.TotalOtrosCargos.ToString("F2")));

    total.Add(new XElement(Cbc + "PayableAmount",
        new XAttribute("currencyID", moneda),
        dto.ImporteTotal.ToString("F2")));

    return total;
}

    // ── Línea de detalle ──────────────────────────────────────────────────────
private static XElement BuildInvoiceLine(DetalleFacturaDTO d, int item, string moneda)
{
    var (catId, schemeId, taxName, taxType) = GetCodigosTributo(d.TipoAfectacionIGV);
    bool esGratuito = d.TipoAfectacionIGV == "11" 
               || d.TipoAfectacionIGV == "21" 
               || d.TipoAfectacionIGV == "31";

    var invoiceLine = new XElement(Cac + "InvoiceLine",
        new XElement(Cbc + "ID", item.ToString()),
        new XElement(Cbc + "InvoicedQuantity",
            new XAttribute("unitCode", d.UnidadMedida ?? "NIU"),
            new XAttribute("unitCodeListID", "UN/ECE rec 20"),
            d.Cantidad.ToString("F2")),
            new XElement(Cbc + "LineExtensionAmount",
                new XAttribute("currencyID", moneda),
                d.ValorVenta.ToString("F2")) 
            );

    // Caso productos gratuitos (11, 21, 31)
    if (d.TipoAfectacionIGV == "11" || d.TipoAfectacionIGV == "21" || d.TipoAfectacionIGV == "31")
    {
        invoiceLine.Add(
            new XElement(Cac + "PricingReference",
                new XElement(Cac + "AlternativeConditionPrice",
                    new XElement(Cbc + "PriceAmount",
                        new XAttribute("currencyID", moneda),
                        d.PrecioUnitario.ToString("F2")),
                    new XElement(Cbc + "PriceTypeCode",
                        new XAttribute("listAgencyName", "PE:SUNAT"),
                        new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo16"),
                        "02")) // 02 = gratuito
            )
        );
    }
    else
    {
        invoiceLine.Add(
            new XElement(Cac + "PricingReference",
                new XElement(Cac + "AlternativeConditionPrice",
                    new XElement(Cbc + "PriceAmount",
                        new XAttribute("currencyID", moneda),
                        d.PrecioVenta.ToString("F2")),
                    new XElement(Cbc + "PriceTypeCode",
                        new XAttribute("listAgencyName", "PE:SUNAT"),
                        new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo16"),
                        "01")) // vemta normal
            )
        );
    }

    // Descuentos aplicados al item
    if (d.DescuentoTotal > 0)
    {
        invoiceLine.Add(
            new XElement(Cac + "AllowanceCharge",
                new XElement(Cbc + "ChargeIndicator", "false"),
                new XElement(Cbc + "AllowanceChargeReasonCode",
                    new XAttribute("listAgencyName", "PE:SUNAT"),
                    new XAttribute("listURI", "urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo53"),
                    d.CodigoTipoDescuento),
                new XElement(Cbc + "Amount",
                    new XAttribute("currencyID", moneda),
                    d.DescuentoTotal.ToString("F2")),
                new XElement(Cbc + "BaseAmount",
                    new XAttribute("currencyID", moneda),
                    d.ValorVenta.ToString("F2"))
            )
        );
    }

    var taxTotalAmount = d.MontoIGV + (d.Icbper > 0 ? d.Icbper : 0);
    var taxTotal = new XElement(Cac + "TaxTotal",
        new XElement(Cbc + "TaxAmount",
            new XAttribute("currencyID", moneda),
            taxTotalAmount.ToString("F2")),
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
                    new XElement(Cbc + "TaxTypeCode", taxType))))
    );

    if (d.Icbper > 0 && d.FactorIcbper > 0)
    {
        taxTotal.Add(
            new XElement(Cac + "TaxSubtotal",
                new XElement(Cbc + "TaxAmount",
                    new XAttribute("currencyID", moneda),
                    d.Icbper.ToString("F2")),
                new XElement(Cbc + "BaseUnitMeasure",
                    new XAttribute("unitCode", d.UnidadMedida ?? "NIU"),
                    ((int)d.Cantidad).ToString()),
                new XElement(Cac + "TaxCategory",
                    new XElement(Cbc + "PerUnitAmount",
                        new XAttribute("currencyID", moneda),
                        d.FactorIcbper.ToString("F2")),
                    new XElement(Cac + "TaxScheme",
                        new XElement(Cbc + "ID", "7152"),
                        new XElement(Cbc + "Name", "ICBPER"),
                        new XElement(Cbc + "TaxTypeCode", "OTH"))))
        );
    }

    invoiceLine.Add(taxTotal);

    // ✅ 4. Item
    invoiceLine.Add(
        new XElement(Cac + "Item",
            new XElement(Cbc + "Description", d.Descripcion),
            new XElement(Cac + "SellersItemIdentification",
                new XElement(Cbc + "ID", d.Codigo)),
            new XElement(Cac + "CommodityClassification",
                new XElement(Cbc + "ItemClassificationCode",
                    new XAttribute("listID", "UNSPSC"),
                    new XAttribute("listAgencyName", "GS1 US"),
                    "10191509")))
    );

    invoiceLine.Add(
        new XElement(Cac + "Price",
            new XElement(Cbc + "PriceAmount",
                new XAttribute("currencyID", moneda),
                (esGratuito ? 0 : d.PrecioUnitario).ToString("F2")))
    );

    return invoiceLine;
}

    private static (string catId, string schemeId, string taxName, string taxType) GetCodigosTributo(string? tipAfeIgv) =>
        tipAfeIgv switch
        {
            "10" => ("S", "1000", "IGV", "VAT"),
            "11" or "21" or "31" => ("Z", "9996", "GRA", "FRE"),
            "20" or "21" => ("E", "9997", "EXO", "VAT"),
            "30" or "31" => ("O", "9998", "INA", "FRE"),
            _    => ("S", "1000", "IGV", "VAT")
        };
}