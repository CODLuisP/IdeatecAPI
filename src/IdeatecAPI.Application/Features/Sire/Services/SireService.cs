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
    private const string UrlPeriodos = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/padron/web/omisos/140000/periodos";
    private const string UrlExportaPropuesta = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvie/propuesta/web/propuesta/{0}/exportapropuesta?codTipoArchivo=0";
    // Manual v30 §5.16: codLibro y codOrigenEnvio son obligatorios
    private const string UrlConsultaEstadoTicket = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/gestionprocesosmasivos/web/masivo/consultaestadotickets?perIni={0}&perFin={0}&page=1&perPage=20&numTicket={1}&codLibro=140000&codOrigenEnvio=2";
    // Manual v30 §5.17: codTipoArchivoReporte viene dinámico de la respuesta 5.16 (archivoReporte[0].codTipoAchivoReporte)
    private const string UrlArchivoReporte = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/gestionprocesosmasivos/web/masivo/archivoreporte?nomArchivoReporte={0}&codTipoArchivoReporte={1}&codLibro=140000&perTributario={2}&codProceso={3}&numTicket={4}";
    private const string UrlAceptaPropuesta = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvie/propuesta/web/propuesta/{0}/aceptapropuesta";
    private const string UrlRegistraPreliminar = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/gestionlibro/web/registroslibros/{0}/registrapreliminar";

    private static readonly int[] TicketRetryDelays = { 2000, 3000, 5000, 5000, 8000, 8000, 10000, 10000 };

    // HttpClient no envía User-Agent por defecto; el WAF de SUNAT devuelve 401 (página nginx) sin esta cabecera
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) IdeatecAPI-SIRE/1.0";

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
            request.Headers.Add("User-Agent", UserAgent);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE] Error consultando periodos: {Status} {Content}", response.StatusCode, content);
                return new SirePeriodosResponse { Success = false, Mensaje = $"SUNAT respondió {response.StatusCode}", RespuestaCruda = content };
            }

            // Manual v30 §5.2: la respuesta es un array raíz de ejercicios (numEjercicio, desEstado, lisPeriodos[])
            // Cada lisPeriodos contiene: perTributario, codEstado, desEstado
            var periodos = new List<SirePeriodoDto>();
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var ejercicio in doc.RootElement.EnumerateArray())
                {
                    if (!ejercicio.TryGetProperty("lisPeriodos", out var lisPeriodos)
                        || lisPeriodos.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var item in lisPeriodos.EnumerateArray())
                    {
                        periodos.Add(new SirePeriodoDto
                        {
                            Periodo = item.TryGetProperty("perTributario", out var p) ? p.GetString() : null,
                            Estado = item.TryGetProperty("desEstado", out var e) ? e.GetString() : null,
                            Descripcion = item.TryGetProperty("codEstado", out var d) ? d.GetString() : null
                        });
                    }
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
            var (numTicket, codProceso) = await ExportarPropuestaAsync(token, perTributario);
            if (string.IsNullOrEmpty(numTicket))
                return new SireDescargarPropuestaResponse { Success = false, Mensaje = "SUNAT no devolvió un ticket para exportar la propuesta" };

            var (nomArchivoReporte, codTipoArchivoReporte, mensajeEspera) = await EsperarTicketTerminadoAsync(token, perTributario, numTicket);
            if (string.IsNullOrEmpty(nomArchivoReporte))
                return new SireDescargarPropuestaResponse { Success = false, Mensaje = mensajeEspera, NumTicket = numTicket };

            var zipBytes = await DescargarArchivoReporteAsync(token, nomArchivoReporte, codTipoArchivoReporte, perTributario, codProceso ?? "01", numTicket);
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

    private async Task<(string? NumTicket, string? CodProceso)> ExportarPropuestaAsync(string token, string perTributario)
    {
        var url = string.Format(UrlExportaPropuesta, perTributario);
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("User-Agent", UserAgent);
        request.Headers.Add("Accept", "application/json");

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("[SIRE] Error exportando propuesta {Periodo}: {Status} {Content}", perTributario, response.StatusCode, content);
            return (null, null);
        }

        using var doc = JsonDocument.Parse(content);
        var numTicket = doc.RootElement.TryGetProperty("numTicket", out var t) ? t.GetString() : null;
        var codProceso = doc.RootElement.TryGetProperty("codProceso", out var cp) ? cp.GetString() : null;
        return (numTicket, codProceso);
    }

    // Manual v30 Anexo III - codEstadoProceso: 01=Cargado, 02=Validando, 03=Error, 04=Procesado OK, 05=En proceso, 06=Terminado
    // Retorna: (nomArchivoReporte, codTipoArchivoReporte, mensaje)
    private async Task<(string? NomArchivoReporte, string? CodTipoArchivoReporte, string Mensaje)> EsperarTicketTerminadoAsync(
        string token, string perTributario, string numTicket)
    {
        var url = string.Format(UrlConsultaEstadoTicket, perTributario, numTicket);
        var client = _httpClientFactory.CreateClient();

        foreach (var delay in TicketRetryDelays)
        {
            await Task.Delay(delay);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("User-Agent", UserAgent);
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
            // Normalizar para manejar con o sin cero inicial (SUNAT puede devolver "4" o "04")
            var estadoNorm = codEstado?.TrimStart('0') ?? "";

            if (estadoNorm == "4" || estadoNorm == "6") // 04=Procesado sin errores, 06=Terminado
            {
                string? nomArchivo = null;
                string? codTipoArchivo = null;

                // Manual v30 §5.17: fuente principal es archivoReporte[0]
                if (registro.TryGetProperty("archivoReporte", out var archivoArr)
                    && archivoArr.ValueKind == JsonValueKind.Array)
                {
                    var primerArchivo = archivoArr.EnumerateArray().FirstOrDefault();
                    if (primerArchivo.ValueKind != JsonValueKind.Undefined)
                    {
                        nomArchivo = primerArchivo.TryGetProperty("nomArchivoReporte", out var na) ? na.GetString() : null;
                        // El manual v30 tiene un typo: "codTipoAchivoReporte" (falta la r), se prueba ambas variantes
                        codTipoArchivo = primerArchivo.TryGetProperty("codTipoAchivoReporte", out var cta)
                            ? cta.GetString()
                            : (primerArchivo.TryGetProperty("codTipoArchivoReporte", out var cta2) ? cta2.GetString() : null);
                    }
                }

                // Fallback: detalleTicket.nomArchivoReporte
                if (string.IsNullOrEmpty(nomArchivo)
                    && registro.TryGetProperty("detalleTicket", out var detalle)
                    && detalle.ValueKind == JsonValueKind.Object)
                {
                    nomArchivo = detalle.TryGetProperty("nomArchivoReporte", out var dna) ? dna.GetString() : null;
                }

                return (nomArchivo, codTipoArchivo, "Terminado");
            }

            if (estadoNorm == "3") // 03=Procesado con Errores
            {
                _logger.LogError("[SIRE] Ticket {Ticket} procesado con errores por SUNAT", numTicket);
                return (null, null, "SUNAT procesó el ticket con errores. Revisa los comprobantes e intenta nuevamente.");
            }
            // 01, 02, 05 → sigue esperando
        }

        return (null, null, "SUNAT aún está generando la propuesta. Intenta de nuevo en unos minutos.");
    }

    // Manual v30 §5.17: codTipoArchivoReporte viene de archivoReporte[0].codTipoAchivoReporte del ticket
    // Si es null, se envía "null" según indicación del manual
    private async Task<byte[]?> DescargarArchivoReporteAsync(
        string token, string nomArchivoReporte, string? codTipoArchivoReporte, string perTributario, string codProceso, string numTicket)
    {
        var codTipo = string.IsNullOrEmpty(codTipoArchivoReporte) ? "null" : codTipoArchivoReporte;
        var url = string.Format(UrlArchivoReporte, nomArchivoReporte, codTipo, perTributario, codProceso, numTicket);
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("User-Agent", UserAgent);

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
            request.Headers.Add("User-Agent", UserAgent);
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
            request.Headers.Add("User-Agent", UserAgent);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE] Error registrando preliminar {Periodo}: {Status} {Content}", perTributario, response.StatusCode, content);
                return new SireRegistrarPreliminarResponse { Success = false, Mensaje = $"SUNAT respondió {response.StatusCode}", RespuestaCruda = content };
            }

            // Manual v30 §5.9: si la respuesta trae numTicket debe esperarse hasta codEstadoProceso 06=Terminado
            if (!string.IsNullOrWhiteSpace(content) && content.Trim() != "null")
            {
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object
                        && doc.RootElement.TryGetProperty("numTicket", out var t)
                        && !string.IsNullOrEmpty(t.GetString()))
                    {
                        var numTicket = t.GetString()!;
                        var (_, _, mensajeTicket) = await EsperarTicketTerminadoAsync(token, perTributario, numTicket);
                        var exitoso = mensajeTicket == "Terminado";
                        return new SireRegistrarPreliminarResponse
                        {
                            Success = exitoso,
                            Mensaje = exitoso ? "Periodo cerrado correctamente" : mensajeTicket,
                            RespuestaCruda = content
                        };
                    }
                }
                catch (JsonException)
                {
                    // Respuesta no es JSON válido, se trata como éxito vacío
                }
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

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(payload)
            };
            request.Headers.Add("User-Agent", UserAgent);

            var response = await client.SendAsync(request);
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
