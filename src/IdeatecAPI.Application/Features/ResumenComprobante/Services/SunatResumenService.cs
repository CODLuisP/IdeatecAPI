using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace IdeatecAPI.Application.Features.ResumenComprobante.Services;

public class SunatResumenResponse
{
    public bool Success { get; set; }
    public string? Ticket { get; set; }
    public string? CodigoRespuesta { get; set; }
    public string? Descripcion { get; set; }
    public string? CdrBase64 { get; set; }
    public bool TieneObservaciones { get; set; }
}

public interface ISunatResumenService
{
    Task<SunatResumenResponse> SendResumenAsync(
        byte[] xmlFirmadoBytes,
        string nombreArchivo,
        string ruc,
        string solUsuario,
        string solClave,
        string environment
    );
}

public class SunatResumenService : ISunatResumenService
{
    private readonly IHttpClientFactory _httpClientFactory;

    private const string UrlBeta       = "https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService";
    private const string UrlProduccion = "https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService";

    public SunatResumenService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<SunatResumenResponse> SendResumenAsync(
        byte[] xmlFirmadoBytes,
        string nombreArchivo,
        string ruc,
        string solUsuario,
        string solClave,
        string environment)
    {
        // ── 1. Comprimir en ZIP ───────────────────────────────────────────
        var zipBase64 = ComprimirXml(xmlFirmadoBytes, nombreArchivo + ".xml");
        var url       = environment.ToLower() == "beta" ? UrlBeta : UrlProduccion;

        // ── 2. Enviar con sendSummary ─────────────────────────────────────
        var ticket = await EnviarSendSummaryAsync(url, ruc, solUsuario, solClave, nombreArchivo, zipBase64);
        if (ticket.StartsWith("ERROR"))
        {
            return new SunatResumenResponse
            {
                Success         = false,
                CodigoRespuesta = "ERROR_ENVIO",
                Descripcion     = ticket
            };
        }

        // ── 3. Esperar y consultar ticket con reintentos ──────────────────
        const int maxIntentos = 5;
        const int esperaMs    = 3000;

        SunatResumenResponse? resultado = null;

        for (int intento = 1; intento <= maxIntentos; intento++)
        {
            await Task.Delay(esperaMs);
            resultado = await ConsultarTicketAsync(url, ruc, solUsuario, solClave, ticket);

            if (resultado.CodigoRespuesta != "98")
                break;
        }

        if (resultado is null || resultado.CodigoRespuesta == "98")
        {
            return new SunatResumenResponse
            {
                Success         = false,
                Ticket          = ticket,
                CodigoRespuesta = "98",
                Descripcion     = $"SUNAT aún procesando. Ticket: {ticket}"
            };
        }

        return resultado;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string ComprimirXml(byte[] xmlBytes, string nombreArchivo)
    {
        using var memStream = new MemoryStream();
        using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(nombreArchivo, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(xmlBytes, 0, xmlBytes.Length);
        }
        return Convert.ToBase64String(memStream.ToArray());
    }

    private async Task<string> EnviarSendSummaryAsync(
        string url, string ruc, string solUsuario, string solClave,
        string nombreArchivo, string zipBase64)
    {
        var usuario      = ruc + solUsuario;
        var soapEnvelope = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ser=""http://service.sunat.gob.pe"" xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd""><soapenv:Header><wsse:Security><wsse:UsernameToken><wsse:Username>{usuario}</wsse:Username><wsse:Password>{solClave}</wsse:Password></wsse:UsernameToken></wsse:Security></soapenv:Header><soapenv:Body><ser:sendSummary><fileName>{nombreArchivo}.zip</fileName><contentFile>{zipBase64}</contentFile></ser:sendSummary></soapenv:Body></soapenv:Envelope>";

        try
        {
            var client  = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("SOAPAction", "");
            request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

            var response        = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            var xDoc  = XDocument.Parse(responseContent);
            var fault = xDoc.Descendants("faultstring").FirstOrDefault();
            if (fault != null)
                return $"ERROR: {fault.Value}";

            var ticket = xDoc.Descendants("ticket").FirstOrDefault()?.Value;
            return string.IsNullOrEmpty(ticket) ? "ERROR: No se recibió ticket" : ticket;
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    private async Task<SunatResumenResponse> ConsultarTicketAsync(
        string url, string ruc, string solUsuario, string solClave, string ticket)
    {
        var usuario      = ruc + solUsuario;
        var soapEnvelope = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ser=""http://service.sunat.gob.pe"" xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd""><soapenv:Header><wsse:Security><wsse:UsernameToken><wsse:Username>{usuario}</wsse:Username><wsse:Password>{solClave}</wsse:Password></wsse:UsernameToken></wsse:Security></soapenv:Header><soapenv:Body><ser:getStatus><ticket>{ticket}</ticket></ser:getStatus></soapenv:Body></soapenv:Envelope>";

        try
        {
            var client  = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("SOAPAction", "");
            request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

            var response        = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            var xDoc  = XDocument.Parse(responseContent);
            var fault = xDoc.Descendants("faultstring").FirstOrDefault();
            if (fault != null)
            {
                return new SunatResumenResponse
                {
                    Success         = false,
                    Ticket          = ticket,
                    CodigoRespuesta = "SOAP_FAULT",
                    Descripcion     = fault.Value
                };
            }

            var cdrBase64 = xDoc.Descendants("content").FirstOrDefault()?.Value;
            if (string.IsNullOrEmpty(cdrBase64))
            {
                return new SunatResumenResponse
                {
                    Success         = false,
                    Ticket          = ticket,
                    CodigoRespuesta = "SIN_CDR",
                    Descripcion     = "SUNAT no devolvió CDR"
                };
            }

            var (codigo, descripcion, tieneObservaciones) = ExtraerCodigoCdr(cdrBase64);

            return new SunatResumenResponse
            {
                Success            = codigo == "0",
                Ticket             = ticket,
                CodigoRespuesta    = codigo,
                Descripcion        = descripcion,
                CdrBase64          = cdrBase64,
                TieneObservaciones = tieneObservaciones
            };
        }
        catch (Exception ex)
        {
            return new SunatResumenResponse
            {
                Success         = false,
                Ticket          = ticket,
                CodigoRespuesta = "ERROR_CONSULTA",
                Descripcion     = $"Error al consultar ticket: {ex.Message}"
            };
        }
    }

    private static (string codigo, string descripcion, bool tieneObservaciones) ExtraerCodigoCdr(string cdrBase64)
    {
        try
        {
            var zipBytes = Convert.FromBase64String(cdrBase64);
            using var zipStream = new MemoryStream(zipBytes);
            using var zip       = new ZipArchive(zipStream, ZipArchiveMode.Read);

            var entry = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".xml"))
                ?? throw new InvalidOperationException("CDR zip sin XML");

            string cdrRaw;
            using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                cdrRaw = reader.ReadToEnd();

            var xCdr = XDocument.Parse(cdrRaw);
            XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
            XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

            var responseNode = xCdr.Descendants(cac + "Response").FirstOrDefault();
            var responseCode = responseNode?.Element(cbc + "ResponseCode")?.Value ?? "???";
            var description  = responseNode?.Element(cbc + "Description")?.Value ?? "Sin descripción";

            var tieneObservaciones = xCdr.Descendants(cbc + "ID")
                .Any(e => int.TryParse(e.Value, out var cod) && cod >= 4000);

            return (responseCode, description, tieneObservaciones);
        }
        catch (Exception ex)
        {
            return ("CDR_ERROR", $"No se pudo leer el CDR: {ex.Message}", false);
        }
    }
}