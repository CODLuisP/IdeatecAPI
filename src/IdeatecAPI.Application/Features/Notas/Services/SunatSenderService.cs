using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;

namespace IdeatecAPI.Application.Features.Notas.Services;

public class SunatResponse
{
    public bool Success { get; set; }
    public string? CodigoRespuesta { get; set; }
    public string? Descripcion { get; set; }
    public string? CdrBase64 { get; set; }
}

public interface ISunatSenderService
{
    Task<SunatResponse> SendNoteAsync(
        byte[] xmlFirmadoBytes,
        string nombreArchivo,
        string solUsuario,
        string solClave,
        string environment
    );
}

public class SunatSenderService : ISunatSenderService
{
    private readonly IHttpClientFactory _httpClientFactory;

    private const string UrlBeta = "https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService";
    private const string UrlProduccion = "https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService";

    public SunatSenderService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<SunatResponse> SendNoteAsync(
        byte[] xmlFirmadoBytes,   // ← recibe bytes directamente
        string nombreArchivo,
        string solUsuario,
        string solClave,
        string environment)
    {
        // ── 1. Empaquetar bytes en ZIP ────────────────────────────────────
        var zipBase64 = ComprimirXml(xmlFirmadoBytes, nombreArchivo + ".xml");

        // ── 2. Construir el SOAP envelope ─────────────────────────────────
        var soapEnvelope = BuildSoapEnvelope(nombreArchivo, zipBase64);

        // ── 3. Enviar a SUNAT ─────────────────────────────────────────────
        var url = environment.ToLower() == "beta" ? UrlBeta : UrlProduccion;
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{solUsuario}:{solClave}"));

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Add("SOAPAction", "");
        request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request);
        }
        catch (Exception ex)
        {
            return new SunatResponse
            {
                Success = false,
                CodigoRespuesta = "ERROR_RED",
                Descripcion = $"Error de conexión con SUNAT: {ex.Message}"
            };
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        // ── 4. Parsear respuesta SOAP ─────────────────────────────────────
        return ParseSoapResponse(responseContent);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string ComprimirXml(byte[] xmlBytes, string nombreArchivo)
    {
        using var memStream = new MemoryStream();
        using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(nombreArchivo, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(xmlBytes, 0, xmlBytes.Length); // ← sin re-encodear
        }
        return Convert.ToBase64String(memStream.ToArray());
    }

    private static string BuildSoapEnvelope(string nombreArchivo, string zipBase64) => $@"
        <soapenv:Envelope
            xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
            xmlns:ser=""http://service.sunat.gob.pe""
            xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
            xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
            <soapenv:Header/>
            <soapenv:Body>
                <ser:sendBill>
                    <fileName>{nombreArchivo}.zip</fileName>
                    <contentFile>{zipBase64}</contentFile>
                </ser:sendBill>
            </soapenv:Body>
        </soapenv:Envelope>";

    private static SunatResponse ParseSoapResponse(string soapResponse)
    {
        try
        {
            var xDoc = XDocument.Parse(soapResponse);

            var fault = xDoc.Descendants("faultstring").FirstOrDefault();
            if (fault != null)
            {
                return new SunatResponse
                {
                    Success = false,
                    CodigoRespuesta = "SOAP_FAULT",
                    Descripcion = fault.Value
                };
            }

            var cdrBase64 = xDoc.Descendants("applicationResponse").FirstOrDefault()?.Value
                         ?? xDoc.Descendants("return").FirstOrDefault()?.Value;

            if (string.IsNullOrEmpty(cdrBase64))
            {
                return new SunatResponse
                {
                    Success = false,
                    CodigoRespuesta = "SIN_RESPUESTA",
                    Descripcion = "SUNAT no devolvió CDR"
                };
            }

            var (codigo, descripcion) = ExtraerCodigoCdr(cdrBase64);

            return new SunatResponse
            {
                Success = codigo == "0",  // ← codigo, no responseCode
                CodigoRespuesta = codigo,
                Descripcion = descripcion,
                CdrBase64 = cdrBase64
            };
        }
        catch (Exception ex)
        {
            return new SunatResponse
            {
                Success = false,
                CodigoRespuesta = "ERROR_PARSE",
                Descripcion = $"Error al procesar respuesta: {ex.Message}"
            };
        }
    }

    private static (string codigo, string descripcion) ExtraerCodigoCdr(string cdrBase64)
    {
        try
        {
            var zipBytes = Convert.FromBase64String(cdrBase64);
            using var zipStream = new MemoryStream(zipBytes);
            using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var entry = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".xml"))
                ?? throw new InvalidOperationException("CDR zip sin XML");

            string cdrRaw;
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                cdrRaw = reader.ReadToEnd();

            var xCdr = XDocument.Parse(cdrRaw);
            XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
            XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

            var response = xCdr.Descendants(cac + "Response").FirstOrDefault();
            var responseCode = response?.Element(cbc + "ResponseCode")?.Value ?? "???";
            var description = response?.Element(cbc + "Description")?.Value ?? "Sin descripción";

            // ← return dentro del try
            return (responseCode, description);
        }
        catch (Exception ex)
        {
            return ("CDR_ERROR", $"No se pudo leer el CDR: {ex.Message}");
        }
    }
}