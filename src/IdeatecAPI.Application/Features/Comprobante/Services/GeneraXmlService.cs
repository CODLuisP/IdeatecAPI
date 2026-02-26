using System.Text;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
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
    public XmlResultado GenerarXml(GenerarComprobanteDTO dto)
    {
        try
        {
            var xml = BuildXml(dto);
            var xmlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));

            return new XmlResultado
            {
                Exitoso   = true,
                XmlString = xml,
                XmlBase64 = xmlBase64
            };
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

        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine($@"<Invoice xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
         xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
         xmlns:cac=""urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2""
         xmlns:cbc=""urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2""
         xmlns:ext=""urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2""
         xmlns=""urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"">
   <ext:UBLExtensions>
      <ext:UBLExtension>
         <ext:ExtensionContent/>
      </ext:UBLExtension>
   </ext:UBLExtensions>
   <cbc:UBLVersionID>{dto.UblVersion}</cbc:UBLVersionID>
   <cbc:CustomizationID schemeAgencyName=""PE:SUNAT"">2.0</cbc:CustomizationID>
   <cbc:ProfileID schemeAgencyName=""PE:SUNAT"" schemeURI=""urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo17"">{dto.TipoOperacion}</cbc:ProfileID>
   <cbc:ID>{dto.Serie}-{dto.Correlativo}</cbc:ID>
   <cbc:IssueDate>{dto.FechaEmision:yyyy-MM-dd}</cbc:IssueDate>
   <cbc:IssueTime>{dto.FechaEmision:HH:mm:ss}</cbc:IssueTime>
   <cbc:DueDate>{dto.FechaVencimiento:yyyy-MM-dd}</cbc:DueDate>
   <cbc:InvoiceTypeCode listAgencyName=""PE:SUNAT"" listURI=""urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01"" listID=""{dto.TipoOperacion}"">{dto.TipoComprobante}</cbc:InvoiceTypeCode>
   <cbc:DocumentCurrencyCode listID=""ISO 4217 Alpha"" listAgencyName=""United Nations Economic Commission for Europe"">{moneda}</cbc:DocumentCurrencyCode>
   <cbc:LineCountNumeric>{dto.Details.Count}</cbc:LineCountNumeric>");

        // Firma placeholder
        sb.AppendLine($@"   <cac:Signature>
      <cbc:ID>{dto.Serie}-{dto.Correlativo}</cbc:ID>
      <cac:SignatoryParty>
         <cac:PartyIdentification>
            <cbc:ID>{emp.NumeroDocumento}</cbc:ID>
         </cac:PartyIdentification>
         <cac:PartyName>
            <cbc:Name><![CDATA[{emp.RazonSocial}]]></cbc:Name>
         </cac:PartyName>
      </cac:SignatoryParty>
      <cac:DigitalSignatureAttachment>
         <cac:ExternalReference>
            <cbc:URI>#SignatureSP</cbc:URI>
         </cac:ExternalReference>
      </cac:DigitalSignatureAttachment>
   </cac:Signature>");

        // Emisor
        sb.AppendLine($@"   <cac:AccountingSupplierParty>
      <cac:Party>
         <cac:PartyIdentification>
            <cbc:ID schemeID=""6"" schemeAgencyName=""PE:SUNAT"" schemeURI=""urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06"">{emp.NumeroDocumento}</cbc:ID>
         </cac:PartyIdentification>
         <cac:PartyName>
            <cbc:Name><![CDATA[{emp.RazonSocial}]]></cbc:Name>
         </cac:PartyName>
         <cac:PartyTaxScheme>
            <cbc:RegistrationName><![CDATA[{emp.RazonSocial}]]></cbc:RegistrationName>
            <cbc:CompanyID schemeID=""6"" schemeAgencyName=""PE:SUNAT"">{emp.NumeroDocumento}</cbc:CompanyID>
            <cac:TaxScheme>
               <cbc:ID schemeID=""6"" schemeAgencyName=""PE:SUNAT"">{emp.NumeroDocumento}</cbc:ID>
            </cac:TaxScheme>
         </cac:PartyTaxScheme>
         <cac:PartyLegalEntity>
            <cbc:RegistrationName><![CDATA[{emp.RazonSocial}]]></cbc:RegistrationName>
            <cac:RegistrationAddress>
               <cbc:ID schemeName=""Ubigeos"" schemeAgencyName=""PE:INEI"">{emp.Ubigeo}</cbc:ID>
               <cbc:AddressTypeCode listAgencyName=""PE:SUNAT"" listName=""Establecimientos anexos"">{emp.EstablecimientoAnexo}</cbc:AddressTypeCode>
               <cbc:CityName><![CDATA[{emp.Provincia}]]></cbc:CityName>
               <cbc:CountrySubentity><![CDATA[{emp.Departamento}]]></cbc:CountrySubentity>
               <cbc:District><![CDATA[{emp.Distrito}]]></cbc:District>
               <cac:AddressLine>
                  <cbc:Line><![CDATA[{emp.DireccionLineal}]]></cbc:Line>
               </cac:AddressLine>
               <cac:Country>
                  <cbc:IdentificationCode>PE</cbc:IdentificationCode>
               </cac:Country>
            </cac:RegistrationAddress>
         </cac:PartyLegalEntity>
      </cac:Party>
   </cac:AccountingSupplierParty>");

        // Cliente
        sb.AppendLine($@"   <cac:AccountingCustomerParty>
      <cac:Party>
         <cac:PartyIdentification>
            <cbc:ID schemeID=""{cli.TipoDocumento}"" schemeAgencyName=""PE:SUNAT"">{cli.NumeroDocumento}</cbc:ID>
         </cac:PartyIdentification>
         <cac:PartyName>
            <cbc:Name><![CDATA[{cli.RazonSocial}]]></cbc:Name>
         </cac:PartyName>
         <cac:PartyTaxScheme>
            <cbc:RegistrationName><![CDATA[{cli.RazonSocial}]]></cbc:RegistrationName>
            <cbc:CompanyID schemeID=""{cli.TipoDocumento}"" schemeAgencyName=""PE:SUNAT"">{cli.NumeroDocumento}</cbc:CompanyID>
            <cac:TaxScheme>
               <cbc:ID schemeID=""{cli.TipoDocumento}"">{cli.NumeroDocumento}</cbc:ID>
            </cac:TaxScheme>
         </cac:PartyTaxScheme>
         <cac:PartyLegalEntity>
            <cbc:RegistrationName><![CDATA[{cli.RazonSocial}]]></cbc:RegistrationName>
            <cac:RegistrationAddress>
               <cbc:ID schemeName=""Ubigeos"" schemeAgencyName=""PE:INEI"">{cli.Ubigeo}</cbc:ID>
               <cbc:CityName><![CDATA[{cli.Provincia}]]></cbc:CityName>
               <cbc:CountrySubentity><![CDATA[{cli.Departamento}]]></cbc:CountrySubentity>
               <cbc:District><![CDATA[{cli.Distrito}]]></cbc:District>
               <cac:AddressLine>
                  <cbc:Line><![CDATA[{cli.DireccionLineal}]]></cbc:Line>
               </cac:AddressLine>
               <cac:Country>
                  <cbc:IdentificationCode>PE</cbc:IdentificationCode>
               </cac:Country>
            </cac:RegistrationAddress>
         </cac:PartyLegalEntity>
      </cac:Party>
   </cac:AccountingCustomerParty>");

         // Forma de pago
         if (dto.TipoPago?.ToLower() == "contado")
         {
            sb.AppendLine($@"   <cac:PaymentTerms>
               <cbc:ID>FormaPago</cbc:ID>
               <cbc:PaymentMeansID>Contado</cbc:PaymentMeansID>
               <cbc:Amount currencyID=""{moneda}"">{dto.MtoImpVenta:F2}</cbc:Amount>
            </cac:PaymentTerms>");
         }
         else
         {
            // Cabecera crédito
            sb.AppendLine($@"   <cac:PaymentTerms>
               <cbc:ID>FormaPago</cbc:ID>
               <cbc:PaymentMeansID>Credito</cbc:PaymentMeansID>
               <cbc:Amount currencyID=""{moneda}"">0.00</cbc:Amount>
            </cac:PaymentTerms>");

            // Una cuota por cada DetalleCuotas
            if (dto.Cuotas != null && dto.Cuotas.Any())
            {
               foreach (var cuota in dto.Cuotas)
               {
                     sb.AppendLine($@"   <cac:PaymentTerms>
               <cbc:ID>{cuota.NumeroCuota}</cbc:ID>
               <cbc:PaymentMeansID>Cuota</cbc:PaymentMeansID>
               <cbc:Amount currencyID=""{moneda}"">{cuota.Monto:F2}</cbc:Amount>
               <cbc:PaymentDueDate>{cuota.FechaVencimiento:yyyy-MM-dd}</cbc:PaymentDueDate>
            </cac:PaymentTerms>");
               }
            }
         }

        // Impuestos cabecera
        sb.AppendLine($@"   <cac:TaxTotal>
      <cbc:TaxAmount currencyID=""{moneda}"">{dto.TotalImpuestos:F2}</cbc:TaxAmount>");

        if (dto.MtoOperGravadas > 0)
            sb.AppendLine($@"      <cac:TaxSubtotal>
         <cbc:TaxableAmount currencyID=""{moneda}"">{dto.MtoOperGravadas:F2}</cbc:TaxableAmount>
         <cbc:TaxAmount currencyID=""{moneda}"">{dto.MtoIGV:F2}</cbc:TaxAmount>
         <cac:TaxCategory>
            <cbc:ID schemeID=""UN/ECE 5305"" schemeAgencyName=""United Nations Economic Commission for Europe"">S</cbc:ID>
            <cac:TaxScheme>
               <cbc:ID schemeID=""UN/ECE 5153"" schemeAgencyID=""6"">1000</cbc:ID>
               <cbc:Name>IGV</cbc:Name>
               <cbc:TaxTypeCode>VAT</cbc:TaxTypeCode>
            </cac:TaxScheme>
         </cac:TaxCategory>
      </cac:TaxSubtotal>");

        if (dto.MtoOperExoneradas > 0)
            sb.AppendLine($@"      <cac:TaxSubtotal>
         <cbc:TaxableAmount currencyID=""{moneda}"">{dto.MtoOperExoneradas:F2}</cbc:TaxableAmount>
         <cbc:TaxAmount currencyID=""{moneda}"">0.00</cbc:TaxAmount>
         <cac:TaxCategory>
            <cbc:ID schemeID=""UN/ECE 5305"" schemeAgencyName=""United Nations Economic Commission for Europe"">E</cbc:ID>
            <cac:TaxScheme>
               <cbc:ID schemeID=""UN/ECE 5153"" schemeAgencyID=""6"">9997</cbc:ID>
               <cbc:Name>EXO</cbc:Name>
               <cbc:TaxTypeCode>VAT</cbc:TaxTypeCode>
            </cac:TaxScheme>
         </cac:TaxCategory>
      </cac:TaxSubtotal>");

        if (dto.MtoOperInafectas > 0)
            sb.AppendLine($@"      <cac:TaxSubtotal>
         <cbc:TaxableAmount currencyID=""{moneda}"">{dto.MtoOperInafectas:F2}</cbc:TaxableAmount>
         <cbc:TaxAmount currencyID=""{moneda}"">0.00</cbc:TaxAmount>
         <cac:TaxCategory>
            <cbc:ID schemeID=""UN/ECE 5305"" schemeAgencyName=""United Nations Economic Commission for Europe"">O</cbc:ID>
            <cac:TaxScheme>
               <cbc:ID schemeID=""UN/ECE 5153"" schemeAgencyID=""6"">9998</cbc:ID>
               <cbc:Name>INA</cbc:Name>
               <cbc:TaxTypeCode>FRE</cbc:TaxTypeCode>
            </cac:TaxScheme>
         </cac:TaxCategory>
      </cac:TaxSubtotal>");

        sb.AppendLine("   </cac:TaxTotal>");

        // Totales monetarios
        sb.AppendLine($@"   <cac:LegalMonetaryTotal>
      <cbc:LineExtensionAmount currencyID=""{moneda}"">{dto.ValorVenta:F2}</cbc:LineExtensionAmount>
      <cbc:TaxInclusiveAmount currencyID=""{moneda}"">{dto.SubTotal:F2}</cbc:TaxInclusiveAmount>
      <cbc:PayableAmount currencyID=""{moneda}"">{dto.MtoImpVenta:F2}</cbc:PayableAmount>
   </cac:LegalMonetaryTotal>");

        // Líneas de detalle
   for (int i = 0; i < dto.Details.Count; i++)
{
    var d = dto.Details[i];
    var (catId, schemeId, taxName, taxType) = GetCodigosTributo(d.TipoAfectacionIGV);

    sb.AppendLine($@"   <cac:InvoiceLine>
      <cbc:ID>{i + 1}</cbc:ID>
      <cbc:InvoicedQuantity unitCode=""{d.UnidadMedida}"" unitCodeListID=""UN/ECE rec 20"">{d.Cantidad:F2}</cbc:InvoicedQuantity>
      <cbc:LineExtensionAmount currencyID=""{moneda}"">{d.ValorVenta:F2}</cbc:LineExtensionAmount>
      <cac:PricingReference>
         <cac:AlternativeConditionPrice>
            <cbc:PriceAmount currencyID=""{moneda}"">{d.PrecioVenta:F2}</cbc:PriceAmount>
            <cbc:PriceTypeCode listAgencyName=""PE:SUNAT"" listURI=""urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo16"">01</cbc:PriceTypeCode>
         </cac:AlternativeConditionPrice>
      </cac:PricingReference>
      <cac:TaxTotal>
         <cbc:TaxAmount currencyID=""{moneda}"">{d.MontoIGV:F2}</cbc:TaxAmount>
         <cac:TaxSubtotal>
            <cbc:TaxableAmount currencyID=""{moneda}"">{d.BaseIgv:F2}</cbc:TaxableAmount>
            <cbc:TaxAmount currencyID=""{moneda}"">{d.MontoIGV:F2}</cbc:TaxAmount>
            <cac:TaxCategory>
               <cbc:ID schemeID=""UN/ECE 5305"" schemeAgencyName=""United Nations Economic Commission for Europe"">{catId}</cbc:ID>
               <cbc:Percent>{d.PorcentajeIGV:F2}</cbc:Percent>
               <cbc:TaxExemptionReasonCode listAgencyName=""PE:SUNAT"" listURI=""urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo07"">{d.TipoAfectacionIGV}</cbc:TaxExemptionReasonCode>
               <cac:TaxScheme>
                  <cbc:ID schemeID=""UN/ECE 5153"" schemeAgencyName=""PE:SUNAT"">{schemeId}</cbc:ID>
                  <cbc:Name>{taxName}</cbc:Name>
                  <cbc:TaxTypeCode>{taxType}</cbc:TaxTypeCode>
               </cac:TaxScheme>
            </cac:TaxCategory>
         </cac:TaxSubtotal>
      </cac:TaxTotal>
      <cac:Item>
         <cbc:Description><![CDATA[{d.Descripcion}]]></cbc:Description>
         <cac:SellersItemIdentification>
            <cbc:ID><![CDATA[{d.Codigo}]]></cbc:ID>
         </cac:SellersItemIdentification>
         <cac:CommodityClassification>
            <cbc:ItemClassificationCode listID=""UNSPSC"" listAgencyName=""GS1 US"">10191509</cbc:ItemClassificationCode>
         </cac:CommodityClassification>
      </cac:Item>
      <cac:Price>
         <cbc:PriceAmount currencyID=""{moneda}"">{d.PrecioUnitario:F2}</cbc:PriceAmount>
      </cac:Price>
   </cac:InvoiceLine>");
}
        // Leyenda
        if (dto.Legends != null)
            sb.AppendLine($@"   <cbc:Note languageLocaleID=""{dto.Legends.Code}""><![CDATA[{dto.Legends.Value}]]></cbc:Note>");

        sb.AppendLine("</Invoice>");
        return sb.ToString();
    }

   private (string catId, string schemeId, string taxName, string taxType) GetCodigosTributo(string? tipAfeIgv)
   {
      return tipAfeIgv switch
      {
         "10" => ("S", "1000", "IGV", "VAT"),
         "20" => ("E", "9997", "EXO", "VAT"),
         "30" => ("O", "9998", "INA", "FRE"),
         _    => ("S", "1000", "IGV", "VAT")
      };
   }
}