using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace IdeatecAPI.Application.Features.Notas.Services;

public interface IXmlSignerService
{
    string SignXml(string xmlContent, string certificadoPem, string certificadoPassword);
    byte[] SignXmlToBytes(string xmlContent, string certificadoPem, string certificadoPassword);
}

public class XmlSignerService : IXmlSignerService
{
    public string SignXml(string xmlContent, string certificadoPem, string certificadoPassword)
    {
        var bytes = SignXmlToBytes(xmlContent, certificadoPem, certificadoPassword);
        return Encoding.UTF8.GetString(bytes);
    }

    public byte[] SignXmlToBytes(string xmlContent, string certificadoPem, string certificadoPassword)
    {
        // ── Intentar cargar certificado (soporta PFX/Binary o PEM/Text) ────
        var certBytes = Convert.FromBase64String(certificadoPem);
        X509Certificate2 certificate;

        try
        {
            // Intentar cargar como PFX (PKCS#12)
            certificate = new X509Certificate2(certBytes, certificadoPassword, 
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);

            // Si se cargó pero no tiene llave privada (pasa con PEMs en este constructor), 
            // forzamos la entrada al catch para procesarlo como PEM.
            if (!certificate.HasPrivateKey)
            {
                throw new Exception("Carga incompleta, procesar como PEM.");
            }
        }
        catch
        {
            // Fallback a PEM: Método ultra-robusto para BouncyCastle PEMs
            var pemText = Encoding.UTF8.GetString(certBytes);
            
            try 
            {
                // 1. Cargar el certificado público (toma el primer bloque BEGIN CERTIFICATE)
                var publicCert = X509Certificate2.CreateFromPem(pemText);
                
                // 2. Cargar la llave privada RSA (toma el bloque BEGIN RSA PRIVATE KEY)
                using var rsaKey = System.Security.Cryptography.RSA.Create();
                rsaKey.ImportFromPem(pemText); 
                
                // 3. Vincular llave privada con certificado público
                using var certWithKey = publicCert.CopyWithPrivateKey(rsaKey);
                
                // 4. Exportar y re-importar como PFX con clave temporal
                // Este paso es VITAL en Windows/IIS para que SignedXml reconozca la clave privada
                var pfxData = certWithKey.Export(X509ContentType.Pfx, "1234");
                certificate = new X509Certificate2(pfxData, "1234", 
                    X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error crítico al procesar certificado PEM: {ex.Message}. Verifique que el archivo contenga tanto el CERTIFICATE como la PRIVATE KEY.");
            }
        }

        // ── Cargar XML preservando whitespace ──────────────────────────────
        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        xmlDoc.LoadXml(xmlContent);

        // ── Obtener clave privada RSA ──────────────────────────────────────
        var rsa = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("El certificado no tiene clave privada RSA");

        // ── Configurar firma (Usando SHA256 para producción) ──────────────
        var signedXml = new SignedXml(xmlDoc) { SigningKey = rsa };

        if (signedXml.SignedInfo != null)
        {
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        }

        // ── Referencia al documento completo ──────────────────────────────
        var reference = new Reference { Uri = "" };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NTransform());
        reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
        signedXml.AddReference(reference);

        // ── Agregar certificado público en KeyInfo ────────────────────────
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(certificate));
        signedXml.KeyInfo = keyInfo;

        // ── Firmar ────────────────────────────────────────────────────────
        signedXml.ComputeSignature();
        var signatureElement = signedXml.GetXml();

        // ── Inyectar firma en ext:ExtensionContent ────────────────────────
        var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
        nsManager.AddNamespace("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");

        var extensionContent = xmlDoc.SelectSingleNode("//ext:ExtensionContent", nsManager)
            ?? throw new InvalidOperationException("No se encontró el nodo ext:ExtensionContent en el XML");

        extensionContent.AppendChild(xmlDoc.ImportNode(signatureElement, true));

        // ── Guardar como bytes UTF-8 sin BOM ──────────────────────────────
        using var ms = new MemoryStream();
        using var xw = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Encoding           = new UTF8Encoding(false),
            Indent             = false,
            OmitXmlDeclaration = false
        });
        xmlDoc.Save(xw);
        xw.Flush();
        return ms.ToArray();
    }
}