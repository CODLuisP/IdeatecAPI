using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using IdeatecAPI.Application.Features.Sire.DTOs;
using Microsoft.Extensions.Logging;

namespace IdeatecAPI.Application.Features.Sire.Services;

public class SireService : ISireService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SireService> _logger;

    private const string UrlToken = "https://api-seguridad.sunat.gob.pe/v1/clientessol/{0}/oauth2/token/";
    private const string UrlPeriodos = "https://apisire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/padron/web/omisos/140000/periodos";
    private const string UrlExportaPropuesta = "https://apisire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvie/propuesta/web/propuesta/{0}/exportapropuesta?codTipoArchivo=0";
    private const string UrlConsultaEstadoTicket = "https://apisire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/gestionprocesosmasivos/web/masivo/consultaestadotickets?perIni={0}&perFin={0}&page=1&perPage=20&numTicket={1}";
    private const string UrlArchivoReporte = "https://apisire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/gestionprocesosmasivos/web/masivo/archivoreporte?nomArchivoReporte={0}&codTipoArchivoReporte=01&codLibro=140000&perTributario={1}&codProceso={2}&numTicket={3}";
    private const string UrlAceptaPropuesta = "https://apisire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvie/propuesta/web/propuesta/{0}/aceptapropuesta";
    private const string UrlRegistraPreliminar = "https://apisire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/gestionlibro/web/registroslibros/{0}/registrapreliminar";

    // SUNAT no documenta explícitamente el codProceso para "exportar propuesta RVIE";
    // "01" es el valor observado en integraciones de este flujo junto a codLibro=140000.
    private const string CodProcesoExportaPropuesta = "01";

    private static readonly int[] TicketRetryDelays = { 2000, 3000, 5000, 5000, 8000, 8000, 10000, 10000 };

    public SireService(IHttpClientFactory httpClientFactory, ILogger<SireService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SirePeriodosResponse> ConsultarPeriodosAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret)
    {
        var token = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret);
        if (string.IsNullOrEmpty(token))
            return new SirePeriodosResponse { Success = false, Mensaje = "No se pudo obtener el token de autenticación" };

        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, UrlPeriodos);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE] Error consultando periodos: {Status} {Content}", response.StatusCode, content);
                return new SirePeriodosResponse { Success = false, Mensaje = $"SUNAT respondió {response.StatusCode}", RespuestaCruda = content };
            }

            var periodos = new List<SirePeriodoDto>();
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("periodos", out var periodosEl) && periodosEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in periodosEl.EnumerateArray())
                {
                    periodos.Add(new SirePeriodoDto
                    {
                        Periodo = item.TryGetProperty("perTributario", out var p) ? p.GetString() : null,
                        Estado = item.TryGetProperty("estado", out var e) ? e.GetString() : null,
                        Descripcion = item.TryGetProperty("descripcion", out var d) ? d.GetString() : null
                    });
                }
            }

            return new SirePeriodosResponse { Success = true, Periodos = periodos, RespuestaCruda = content };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE] Error consultando periodos");
            return new SirePeriodosResponse { Success = false, Mensaje = ex.Message };
        }
    }

    public async Task<SireDescargarPropuestaResponse> DescargarPropuestaAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario)
    {
        var token = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret);
        if (string.IsNullOrEmpty(token))
            return new SireDescargarPropuestaResponse { Success = false, Mensaje = "No se pudo obtener el token de autenticación" };

        try
        {
            var numTicket = await ExportarPropuestaAsync(token, perTributario);
            if (string.IsNullOrEmpty(numTicket))
                return new SireDescargarPropuestaResponse { Success = false, Mensaje = "SUNAT no devolvió un ticket para exportar la propuesta" };

            var (nomArchivoReporte, mensajeEspera) = await EsperarTicketTerminadoAsync(token, perTributario, numTicket);
            if (string.IsNullOrEmpty(nomArchivoReporte))
                return new SireDescargarPropuestaResponse { Success = false, Mensaje = mensajeEspera, NumTicket = numTicket };

            var zipBytes = await DescargarArchivoReporteAsync(token, nomArchivoReporte, perTributario, numTicket);
            if (zipBytes is null)
                return new SireDescargarPropuestaResponse { Success = false, Mensaje = "No se pudo descargar el archivo de la propuesta", NumTicket = numTicket };

            var comprobantes = ExtraerComprobantesDeZip(zipBytes);
            return new SireDescargarPropuestaResponse { Success = true, NumTicket = numTicket, Comprobantes = comprobantes };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE] Error descargando propuesta {Periodo}", perTributario);
            return new SireDescargarPropuestaResponse { Success = false, Mensaje = ex.Message };
        }
    }

    private async Task<string?> ExportarPropuestaAsync(string token, string perTributario)
    {
        var url = string.Format(UrlExportaPropuesta, perTributario);
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("Accept", "application/json");

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("[SIRE] Error exportando propuesta {Periodo}: {Status} {Content}", perTributario, response.StatusCode, content);
            return null;
        }

        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.TryGetProperty("numTicket", out var t) ? t.GetString() : null;
    }

    private async Task<(string? nomArchivoReporte, string mensaje)> EsperarTicketTerminadoAsync(
        string token, string perTributario, string numTicket)
    {
        var url = string.Format(UrlConsultaEstadoTicket, perTributario, numTicket);
        var client = _httpClientFactory.CreateClient();

        foreach (var delay in TicketRetryDelays)
        {
            await Task.Delay(delay);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE] Error consultando ticket {Ticket}: {Status} {Content}", numTicket, response.StatusCode, content);
                continue;
            }

            using var doc = JsonDocument.Parse(content);
            if (!doc.RootElement.TryGetProperty("registros", out var registros) || registros.ValueKind != JsonValueKind.Array)
                continue;

            var registro = registros.EnumerateArray().FirstOrDefault();
            if (registro.ValueKind == JsonValueKind.Undefined)
                continue;

            var codEstado = registro.TryGetProperty("codEstadoProceso", out var ce) ? ce.GetString() : null;
            if (codEstado == "3")
            {
                var nomArchivo = registro.TryGetProperty("nomArchivoReporte", out var na) ? na.GetString() : null;
                return (nomArchivo, "Terminado");
            }
        }

        return (null, "SUNAT aún está generando la propuesta. Intenta de nuevo en unos minutos.");
    }

    private async Task<byte[]?> DescargarArchivoReporteAsync(
        string token, string nomArchivoReporte, string perTributario, string numTicket)
    {
        var url = string.Format(UrlArchivoReporte, nomArchivoReporte, perTributario, CodProcesoExportaPropuesta, numTicket);
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {token}");

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogError("[SIRE] Error descargando archivo reporte {Archivo}: {Status} {Content}", nomArchivoReporte, response.StatusCode, content);
            return null;
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    private List<SireComprobanteDto> ExtraerComprobantesDeZip(byte[] zipBytes)
    {
        var comprobantes = new List<SireComprobanteDto>();

        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return comprobantes;

        using var reader = new StreamReader(entry.Open(), System.Text.Encoding.Latin1);
        string? linea;
        while ((linea = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(linea)) continue;

            var campos = linea.Split('|');
            if (campos.Length < 26) continue;

            comprobantes.Add(new SireComprobanteDto
            {
                RucEmisor = campos[0],
                RazonSocialEmisor = campos[1],
                Periodo = campos[2],
                CarSunat = campos[3],
                Correlativo = campos[4],
                FechaEmision = campos[5],
                TipoComprobante = campos[6],
                Serie = campos[7],
                Numero = campos[8],
                TipoDocCliente = campos[10],
                NumDocCliente = campos[11],
                RazonSocialCliente = campos[12],
                BaseImponible = ParseDecimal(campos[14]),
                Igv = ParseDecimal(campos[16]),
                ImporteTotal = ParseDecimal(campos[21]),
                Activo = campos[23] == "1",
                TipoCambio = ParseDecimal(campos[24]),
                CodMoneda = campos[25],
                Inconsistencias = campos.Length >= 57 ? campos[56] : null
            });
        }

        return comprobantes;
    }

    private static decimal ParseDecimal(string valor)
    {
        return decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    public async Task<SireAceptarPropuestaResponse> AceptarPropuestaAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario)
    {
        var token = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret);
        if (string.IsNullOrEmpty(token))
            return new SireAceptarPropuestaResponse { Success = false, Mensaje = "No se pudo obtener el token de autenticación" };

        try
        {
            var url = string.Format(UrlAceptaPropuesta, perTributario);
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE] Error aceptando propuesta {Periodo}: {Status} {Content}", perTributario, response.StatusCode, content);
                return new SireAceptarPropuestaResponse { Success = false, Mensaje = $"SUNAT respondió {response.StatusCode}", RespuestaCruda = content };
            }

            using var doc = JsonDocument.Parse(content);
            var numTicket = doc.RootElement.TryGetProperty("numTicket", out var t) ? t.GetString() : null;

            return new SireAceptarPropuestaResponse { Success = true, NumTicket = numTicket, RespuestaCruda = content };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE] Error aceptando propuesta {Periodo}", perTributario);
            return new SireAceptarPropuestaResponse { Success = false, Mensaje = ex.Message };
        }
    }

    public async Task<SireRegistrarPreliminarResponse> RegistrarPreliminarAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario)
    {
        var token = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret);
        if (string.IsNullOrEmpty(token))
            return new SireRegistrarPreliminarResponse { Success = false, Mensaje = "No se pudo obtener el token de autenticación" };

        try
        {
            var url = string.Format(UrlRegistraPreliminar, perTributario);
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE] Error registrando preliminar {Periodo}: {Status} {Content}", perTributario, response.StatusCode, content);
                return new SireRegistrarPreliminarResponse { Success = false, Mensaje = $"SUNAT respondió {response.StatusCode}", RespuestaCruda = content };
            }

            return new SireRegistrarPreliminarResponse { Success = true, Mensaje = "Periodo cerrado correctamente", RespuestaCruda = content };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE] Error registrando preliminar {Periodo}", perTributario);
            return new SireRegistrarPreliminarResponse { Success = false, Mensaje = ex.Message };
        }
    }

    private async Task<string?> ObtenerTokenAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret)
    {
        try
        {
            var url = string.Format(UrlToken, clienteId);
            var client = _httpClientFactory.CreateClient();
            var payload = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "scope", "https://api-sire.sunat.gob.pe" },
                { "client_id", clienteId },
                { "client_secret", clientSecret },
                { "username", ruc + solUsuario },
                { "password", solClave }
            };

            var response = await client.PostAsync(url, new FormUrlEncodedContent(payload));
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE] Error obteniendo token: {Status} {Content}", response.StatusCode, content);
                return null;
            }

            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.TryGetProperty("access_token", out var tokenProp)
                ? tokenProp.GetString()
                : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE] Error obteniendo token");
            return null;
        }
    }
}
