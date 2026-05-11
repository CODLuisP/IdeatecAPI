using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace IdeatecAPI.Application.Features.GuiaRemision.Services;

public class SunatGuiaResponse
{
    public bool Success { get; set; }
    public string? Ticket { get; set; }
    public string? CodigoRespuesta { get; set; }
    public string? Descripcion { get; set; }
    public string? CdrBase64 { get; set; }
}

public interface ISunatGuiaService
{
    Task<SunatGuiaResponse> SendGuiaAsync(
        byte[] xmlFirmadoBytes,
        string nombreArchivo,
        string ruc,
        string solUsuario,
        string solClave,
        string clienteId,
        string clientSecret,
        string environment
    );
}

public class SunatGuiaService : ISunatGuiaService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SunatGuiaService> _logger;

    // ── URLs Beta (nubefact) ──────────────────────────────────────────────
    private const string UrlTokenBeta   = "https://gre-test.nubefact.com/v1/clientessol/{0}/oauth2/token";
    private const string UrlEnvioBeta   = "https://gre-test.nubefact.com/v1/contribuyente/gem/comprobantes/{0}";
    private const string UrlConsultaBeta = "https://gre-test.nubefact.com/v1/contribuyente/gem/comprobantes/envios/{0}";

    // ── URLs Producción (SUNAT) ───────────────────────────────────────────
    private const string UrlTokenProd   = "https://api-seguridad.sunat.gob.pe/v1/clientessol/{0}/oauth2/token";
    private const string UrlEnvioProd   = "https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/{0}";
    private const string UrlConsultaProd = "https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/envios/{0}";

    // ── Tiempos de espera entre reintentos (ms) ───────────────────────────
    // Intento:        1      2      3      4      5
    private static readonly int[] RetryDelays = { 1000, 2000, 3000, 4000, 5000 };

    public SunatGuiaService(IHttpClientFactory httpClientFactory, ILogger<SunatGuiaService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    public async Task<SunatGuiaResponse> SendGuiaAsync(
        byte[] xmlFirmadoBytes,
        string nombreArchivo,
        string ruc,
        string solUsuario,
        string solClave,
        string clienteId,
        string clientSecret,
        string environment)
    {
        var esBeta = environment.ToLower() == "beta";

        // ── 1. Obtener token OAuth2 ───────────────────────────────────────
        var urlToken = string.Format(esBeta ? UrlTokenBeta : UrlTokenProd, clienteId);
        var token    = await ObtenerTokenAsync(urlToken, clienteId, clientSecret, ruc, solUsuario, solClave);

        if (string.IsNullOrEmpty(token))
            return Error("ERROR_TOKEN", "No se pudo obtener el token de autenticación");

        // ── 2. Comprimir en ZIP ───────────────────────────────────────────
        var zipBytes  = ComprimirXml(xmlFirmadoBytes, nombreArchivo + ".xml");
        var zipBase64 = Convert.ToBase64String(zipBytes);
        var hashZip   = CalcularHashSha256(zipBytes);

        // ── 3. Enviar a SUNAT → recibe ticket ─────────────────────────────
        var urlEnvio = string.Format(esBeta ? UrlEnvioBeta : UrlEnvioProd, nombreArchivo);
        var ticket   = await EnviarGuiaAsync(urlEnvio, token, nombreArchivo + ".zip", zipBase64, hashZip);

        if (ticket.StartsWith("ERROR"))
            return Error("ERROR_ENVIO", ticket);

        // ── 4. Consultar ticket con reintentos y backoff ───────────────────
        // SUNAT procesa de forma asíncrona: el POST solo registra el comprobante
        // y devuelve un ticket. Hay que hacer GET hasta que deje de responder
        // codRespuesta="98" (EN_PROCESO). Con backoff evitamos esperar de más
        // cuando SUNAT es rápido y seguimos reintentando cuando tarda.
        var urlConsulta = string.Format(esBeta ? UrlConsultaBeta : UrlConsultaProd, ticket);
        return await ConsultarConReintentosAsync(urlConsulta, token, ticket);
    }

    // ── Consulta con backoff progresivo ───────────────────────────────────────

    private async Task<SunatGuiaResponse> ConsultarConReintentosAsync(
        string url, string token, string ticket)
    {
        for (int intento = 0; intento < RetryDelays.Length; intento++)
        {
            // Esperar antes de consultar (la primera vez también,
            // porque SUNAT casi nunca está listo en menos de 1 segundo)
            await Task.Delay(RetryDelays[intento]);

            _logger.LogInformation(
                "[SUNAT] Consultando ticket {Ticket} — intento {Intento}/{Max}",
                ticket, intento + 1, RetryDelays.Length);

            var resultado = await ConsultarTicketAsync(url, token, ticket);

            if (resultado.CodigoRespuesta != "EN_PROCESO")
                return resultado; // ACEPTADO o RECHAZADO → salimos

            _logger.LogInformation(
                "[SUNAT] Ticket {Ticket} aún EN_PROCESO, reintentando en {Delay}ms...",
                ticket, intento + 1 < RetryDelays.Length ? RetryDelays[intento + 1] : 0);
        }

        // Agotamos los reintentos — devolvemos EN_PROCESO para que el
        // cliente pueda consultarlo más tarde con el ticket guardado en BD
        return new SunatGuiaResponse
        {
            Success         = false,
            Ticket          = ticket,
            CodigoRespuesta = "EN_PROCESO",
            Descripcion     = "SUNAT aún está procesando. Consulta el ticket más tarde."
        };
    }

    // ── Helpers HTTP ──────────────────────────────────────────────────────────

    private async Task<string?> ObtenerTokenAsync(
        string url, string clienteId, string clientSecret,
        string ruc, string solUsuario, string solClave)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = new Dictionary<string, string>
            {
                { "grant_type",    "password" },
                { "scope",         "https://api-cpe.sunat.gob.pe" },
                { "client_id",     clienteId },
                { "client_secret", clientSecret },
                { "username",      ruc + solUsuario },
                { "password",      solClave }
            };

            var response = await client.PostAsync(url, new FormUrlEncodedContent(payload));
            var content  = await response.Content.ReadAsStringAsync();
            var json     = JsonDocument.Parse(content);

            return json.RootElement.TryGetProperty("access_token", out var tokenProp)
                ? tokenProp.GetString()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SUNAT] Error obteniendo token");
            return null;
        }
    }

    private async Task<string> EnviarGuiaAsync(
        string url, string token,
        string nombreZip, string zipBase64, string hashZip)
    {
        try
        {
            var client  = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Accept", "*/*");

            var body = JsonSerializer.Serialize(new
            {
                archivo = new
                {
                    nomArchivo = nombreZip,
                    arcGreZip  = zipBase64,
                    hashZip    = hashZip
                }
            });

            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var content  = await response.Content.ReadAsStringAsync();
            var json     = JsonDocument.Parse(content);

            if (json.RootElement.TryGetProperty("numTicket", out var ticketProp))
                return ticketProp.GetString() ?? "ERROR: ticket vacío";

            if (json.RootElement.TryGetProperty("errors", out var errors))
                return $"ERROR: {errors}";

            return $"ERROR: Respuesta inesperada: {content}";
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    private async Task<SunatGuiaResponse> ConsultarTicketAsync(
        string url, string token, string ticket)
    {
        try
        {
            var client  = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Accept", "*/*");

            var response = await client.SendAsync(request);
            var content  = await response.Content.ReadAsStringAsync();
            var json     = JsonDocument.Parse(content);

            // ── Formato nuevo con codRespuesta ────────────────────────────
            if (json.RootElement.TryGetProperty("codRespuesta", out var codRespuesta))
            {
                var cod = codRespuesta.GetString();

                if (cod == "98")
                    return new SunatGuiaResponse
                    {
                        Success         = false,
                        Ticket          = ticket,
                        CodigoRespuesta = "EN_PROCESO",
                        Descripcion     = "SUNAT aún está procesando la guía"
                    };

                if (cod == "99")
                {
                    var numError = json.RootElement.TryGetProperty("error", out var err)
                        ? err.GetProperty("numError").GetString()
                        : "ERROR";
                    var desError = json.RootElement.TryGetProperty("error", out var err2)
                        ? err2.GetProperty("desError").GetString()
                        : "Error desconocido";

                    string? cdrBase64 = null;
                    if (json.RootElement.TryGetProperty("indCdrGenerado", out var indCdr)
                        && indCdr.GetString() == "1"
                        && json.RootElement.TryGetProperty("arcCdr", out var arcCdr))
                        cdrBase64 = arcCdr.GetString();

                    return new SunatGuiaResponse
                    {
                        Success         = false,
                        Ticket          = ticket,
                        CodigoRespuesta = numError,
                        Descripcion     = desError,
                        CdrBase64       = cdrBase64
                    };
                }

                if (cod == "0")
                {
                    var cdrBase64 = json.RootElement.TryGetProperty("arcCdr", out var arcCdr)
                        ? arcCdr.GetString() ?? string.Empty
                        : string.Empty;

                    var (codigo, descripcion) = ExtraerCodigoCdr(cdrBase64);

                    return new SunatGuiaResponse
                    {
                        Success         = true,
                        Ticket          = ticket,
                        CodigoRespuesta = codigo,
                        Descripcion     = descripcion,
                        CdrBase64       = cdrBase64
                    };
                }
            }

            // ── Formato anterior con indCdrGenerado (compatibilidad) ──────
            if (json.RootElement.TryGetProperty("indCdrGenerado", out var indCdrGen)
                && indCdrGen.GetString() == "1")
            {
                var cdrBase64 = json.RootElement
                    .GetProperty("arcCdr").GetString() ?? string.Empty;

                var (codigo, descripcion) = ExtraerCodigoCdr(cdrBase64);

                return new SunatGuiaResponse
                {
                    Success         = codigo == "0",
                    Ticket          = ticket,
                    CodigoRespuesta = codigo,
                    Descripcion     = descripcion,
                    CdrBase64       = cdrBase64
                };
            }

            return Error("SIN_CDR", $"Respuesta inesperada: {content}", ticket);
        }
        catch (Exception ex)
        {
            return Error("ERROR_CONSULTA", $"Error al consultar ticket: {ex.Message}", ticket);
        }
    }

    // ── Helpers estáticos ─────────────────────────────────────────────────────

    private static SunatGuiaResponse Error(string codigo, string descripcion, string? ticket = null) =>
        new() { Success = false, Ticket = ticket, CodigoRespuesta = codigo, Descripcion = descripcion };

    private static byte[] ComprimirXml(byte[] xmlBytes, string nombreArchivo)
    {
        using var memStream = new MemoryStream();
        using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry(nombreArchivo, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(xmlBytes, 0, xmlBytes.Length);
        }
        return memStream.ToArray();
    }

    private static string CalcularHashSha256(byte[] bytes)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(bytes)).ToLower();
    }

    private static (string codigo, string descripcion) ExtraerCodigoCdr(string cdrBase64)
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

            var docResponse  = xCdr.Descendants(cac + "DocumentResponse").FirstOrDefault();
            var responseCode = docResponse?.Element(cac + "Response")
                                          ?.Element(cbc + "ResponseCode")?.Value ?? "???";
            var description  = docResponse?.Element(cac + "Response")
                                          ?.Element(cbc + "Description")?.Value ?? "Sin descripción";

            return (responseCode, description);
        }
        catch (Exception ex)
        {
            return ("CDR_ERROR", $"No se pudo leer el CDR: {ex.Message}");
        }
    }
}