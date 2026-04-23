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
        // ── Decodificar Base64 del PEM y obtener texto ────────────────────
        var pemBytes = Convert.FromBase64String(certificadoPem);
        var pemText = Encoding.UTF8.GetString(pemBytes);

        // ── Cargar certificado desde PEM ──────────────────────────────────
        X509Certificate2 certificate;
        if (pemText.Contains("-----BEGIN ENCRYPTED PRIVATE KEY-----"))
        {
            certificate = X509Certificate2.CreateFromEncryptedPem(
                pemText, pemText, certificadoPassword);
        }
        else
        {
            certificate = X509Certificate2.CreateFromPem(pemText, pemText);
        }

        // ── Cargar XML preservando whitespace (importante para la firma) ──
        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        xmlDoc.LoadXml(xmlContent);

        // ── Obtener clave privada RSA ──────────────────────────────────────
        var rsa = certificate.GetRSAPrivateKey()
            ?? throw new InvalidOperationException("El certificado no tiene clave privada RSA");

        // ── Configurar firma ──────────────────────────────────────────────
        var signedXml = new SignedXml(xmlDoc) { SigningKey = rsa };

        if (signedXml.SignedInfo != null)
        {
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            signedXml.SignedInfo.SignatureMethod        = SignedXml.XmlDsigRSASHA1Url;
        }

        // ── Referencia al documento completo ──────────────────────────────
        var reference = new Reference { Uri = "" };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NTransform());
        reference.DigestMethod = SignedXml.XmlDsigSHA1Url;
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