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
        // ── Cargar certificado ────────────────────────────────────────────
        var certBytes = Convert.FromBase64String(certificadoPem);
        var certificate = X509CertificateLoader.LoadPkcs12(
            certBytes,
            certificadoPassword,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable
        );

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
            Encoding           = new UTF8Encoding(false), // false = sin BOM
            Indent             = false,
            OmitXmlDeclaration = false
        });
        xmlDoc.Save(xw);
        xw.Flush();
        return ms.ToArray();
    }
}