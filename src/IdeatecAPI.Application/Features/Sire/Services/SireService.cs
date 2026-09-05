using System.Globalization;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
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
    // Manual v30 §5.16: codLibro y codOrigenEnvio son obligatorios. {2}=codLibro (140000 RVIE / 080000 RCE, Manual SIRE_Compras v27 §5.31)
    private const string UrlConsultaEstadoTicket = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/gestionprocesosmasivos/web/masivo/consultaestadotickets?perIni={0}&perFin={0}&page=1&perPage=20&numTicket={1}&codLibro={2}&codOrigenEnvio=2";
    // Manual v30 §5.17: codTipoArchivoReporte viene dinámico de la respuesta 5.16 (archivoReporte[0].codTipoAchivoReporte). {5}=codLibro (Manual SIRE_Compras v27 §5.32)
    private const string UrlArchivoReporte = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/gestionprocesosmasivos/web/masivo/archivoreporte?nomArchivoReporte={0}&codTipoArchivoReporte={1}&codLibro={5}&perTributario={2}&codProceso={3}&numTicket={4}";
    private const string UrlAceptaPropuesta = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvie/propuesta/web/propuesta/{0}/aceptapropuesta";
    private const string UrlRegistraPreliminar = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/gestionlibro/web/registroslibros/{0}/registrapreliminar";
    // Manual v30 §5.13: DELETE, elimina de la propuesta (antes de aceptar)
    private const string UrlEliminarComprobantePropuesta = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvie/propuesta/web/propuesta/{0}/eliminacomprobante";
    // Manual v30 §5.14: POST, elimina del preliminar (después de "Registrar preliminar")
    private const string UrlEliminarComprobantePreliminar = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/gestionlibro/web/registroslibros/{0}/comprobantepreliminar";
    // Manual v30 §5.4/§5.3: endpoints servidor TUS.IO para importar/reemplazar comprobantes
    private const string UrlImportarPropuesta = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/receptorpropuesta/web/propuesta/upload";
    private const string UrlImportarPreliminar = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/receptorpreliminar/web/preliminar/upload";
    // Manual v30 §5.12: PUT, solo aplica sobre la propuesta (antes de aceptar)
    private const string UrlEditarTipoCambioIndividual = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvie/propuesta/web/propuesta/{0}/complementoindividual";

    // RCE (Registro de Compras Electrónico) - Manual SIRE_Compras v27. Solo se implementa consulta/descarga
    // (comprobantes que otras empresas emitieron a favor del RUC), no el flujo de aceptar/cerrar de RVIE.
    private const string CodLibroRvie = "140000";
    private const string CodLibroRce = "080000";
    // Manual SIRE_Compras v27 §5.33: mismo endpoint que UrlPeriodos, parametrizado por codLibro
    private const string UrlPeriodosPorLibro = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rvierce/padron/web/omisos/{0}/periodos";
    // Manual SIRE_Compras v27 §5.34: genera el ticket para descargar la propuesta de compras (txt, codTipoArchivo=0)
    private const string UrlExportaPropuestaCompras = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rce/propuesta/web/propuesta/{0}/exportacioncomprobantepropuesta?codTipoArchivo=0&codOrigenEnvio=2";
    // Manual SIRE_Compras v28 §5.2: a diferencia de RVIE, RCE tiene su propia URL de aceptar propuesta (no comparte "rvie/propuesta/...")
    private const string UrlAceptaPropuestaRce = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rce/propuesta/web/registroslibros/{0}/aceptarpropuesta";
    // Manual SIRE_Compras v28 §5.4: URL propia de RCE para registrar preliminar (distinta de rvierce/gestionlibro/... de RVIE)
    private const string UrlRegistraPreliminarRce = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rce/preliminar/web/registroslibros/{0}/registrapreliminares";
    // Manual SIRE_Compras v28 §5.15: DELETE, elimina de la propuesta (antes de aceptar). Body es un array plano (igual que RVIE)
    private const string UrlEliminarComprobantePropuestaRce = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rce/propuesta/web/propuestarce/{0}";
    // Manual SIRE_Compras v28 §5.16: POST, elimina del preliminar. Body va envuelto en la clave "detalleAjustes"
    private const string UrlEliminarComprobantePreliminarRce = "https://api-sire.sunat.gob.pe/v1/contribuyente/migeigv/libros/rce/preliminar/web/comprobanteslibroscompras/{0}/eliminacomprobante";

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
                return new SirePeriodosResponse { Success = false, Mensaje = ExtraerMensajeErrorSunat(response.StatusCode, content), RespuestaCruda = content };
            }

            // Manual v30 §5.2: la respuesta es un array raíz de ejercicios (numEjercicio, desEstado, lisPeriodos[])
            // Cada lisPeriodos contiene: perTributario, codEstado, desEstado
            var ejercicios = new List<SireEjercicioDto>();
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var ejercicio in doc.RootElement.EnumerateArray())
                {
                    if (!ejercicio.TryGetProperty("lisPeriodos", out var lisPeriodos)
                        || lisPeriodos.ValueKind != JsonValueKind.Array)
                        continue;

                    var periodosDelAnio = new List<SirePeriodoDto>();
                    foreach (var item in lisPeriodos.EnumerateArray())
                    {
                        periodosDelAnio.Add(new SirePeriodoDto
                        {
                            Periodo = item.TryGetProperty("perTributario", out var p) ? p.GetString() : null,
                            Estado = item.TryGetProperty("desEstado", out var e) ? e.GetString() : null,
                            Descripcion = item.TryGetProperty("codEstado", out var d) ? d.GetString() : null
                        });
                    }

                    // El año se deriva del propio perTributario (YYYYMM) en vez de confiar en el tipo
                    // de numEjercicio, que el manual no especifica con certeza (string vs number).
                    var anio = periodosDelAnio
                        .FirstOrDefault(p => !string.IsNullOrEmpty(p.Periodo) && p.Periodo!.Length >= 4)
                        ?.Periodo?.Substring(0, 4);

                    ejercicios.Add(new SireEjercicioDto { Anio = anio, Periodos = periodosDelAnio });
                }
            }

            return new SirePeriodosResponse { Success = true, Ejercicios = ejercicios, RespuestaCruda = content };
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

            var (nomArchivoReporte, codTipoArchivoReporte, codProcesoTicket, mensajeEspera) = await EsperarTicketTerminadoAsync(token, perTributario, numTicket);
            if (string.IsNullOrEmpty(nomArchivoReporte))
                return new SireDescargarPropuestaResponse { Success = false, Mensaje = mensajeEspera, NumTicket = numTicket };

            // El polling del ticket puede tardar hasta ~51s; se renueva el token antes de descargar
            // por si el original ya expiró.
            var tokenDescarga = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret) ?? token;

            var (zipBytes, errorDescarga) = await DescargarArchivoReporteAsync(
                tokenDescarga, nomArchivoReporte, codTipoArchivoReporte, perTributario, codProcesoTicket ?? "01", numTicket);
            if (zipBytes is null)
                return new SireDescargarPropuestaResponse { Success = false, Mensaje = errorDescarga, NumTicket = numTicket };

            var comprobantes = ExtraerComprobantesDeZip(zipBytes);
            return new SireDescargarPropuestaResponse { Success = true, NumTicket = numTicket, Comprobantes = comprobantes };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE] Error descargando propuesta {Periodo}", perTributario);
            return new SireDescargarPropuestaResponse { Success = false, Mensaje = ex.Message };
        }
    }

    public async Task<SirePeriodosResponse> ConsultarPeriodosRceAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret)
    {
        var token = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret);
        if (string.IsNullOrEmpty(token))
            return new SirePeriodosResponse { Success = false, Mensaje = "No se pudo obtener el token de autenticación" };

        try
        {
            var url = string.Format(UrlPeriodosPorLibro, CodLibroRce);
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("User-Agent", UserAgent);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE][RCE] Error consultando periodos: {Status} {Content}", response.StatusCode, content);
                return new SirePeriodosResponse { Success = false, Mensaje = ExtraerMensajeErrorSunat(response.StatusCode, content), RespuestaCruda = content };
            }

            var ejercicios = new List<SireEjercicioDto>();
            using var doc = JsonDocument.Parse(content);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var ejercicio in doc.RootElement.EnumerateArray())
                {
                    if (!ejercicio.TryGetProperty("lisPeriodos", out var lisPeriodos)
                        || lisPeriodos.ValueKind != JsonValueKind.Array)
                        continue;

                    var periodosDelAnio = new List<SirePeriodoDto>();
                    foreach (var item in lisPeriodos.EnumerateArray())
                    {
                        periodosDelAnio.Add(new SirePeriodoDto
                        {
                            Periodo = item.TryGetProperty("perTributario", out var p) ? p.GetString() : null,
                            Estado = item.TryGetProperty("desEstado", out var e) ? e.GetString() : null,
                            Descripcion = item.TryGetProperty("codEstado", out var d) ? d.GetString() : null
                        });
                    }

                    var anio = periodosDelAnio
                        .FirstOrDefault(p => !string.IsNullOrEmpty(p.Periodo) && p.Periodo!.Length >= 4)
                        ?.Periodo?.Substring(0, 4);

                    ejercicios.Add(new SireEjercicioDto { Anio = anio, Periodos = periodosDelAnio });
                }
            }

            return new SirePeriodosResponse { Success = true, Ejercicios = ejercicios, RespuestaCruda = content };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE][RCE] Error consultando periodos");
            return new SirePeriodosResponse { Success = false, Mensaje = ex.Message };
        }
    }

    public async Task<SireDescargarPropuestaComprasResponse> DescargarPropuestaComprasAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario)
    {
        var token = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret);
        if (string.IsNullOrEmpty(token))
            return new SireDescargarPropuestaComprasResponse { Success = false, Mensaje = "No se pudo obtener el token de autenticación" };

        try
        {
            var url = string.Format(UrlExportaPropuestaCompras, perTributario);
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("User-Agent", UserAgent);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE][RCE] Error exportando propuesta {Periodo}: {Status} {Content}", perTributario, response.StatusCode, content);
                return new SireDescargarPropuestaComprasResponse { Success = false, Mensaje = ExtraerMensajeErrorSunat(response.StatusCode, content) };
            }

            string? numTicket;
            using (var doc = JsonDocument.Parse(content))
            {
                numTicket = doc.RootElement.TryGetProperty("numTicket", out var t) ? t.GetString() : null;
            }

            if (string.IsNullOrEmpty(numTicket))
                return new SireDescargarPropuestaComprasResponse { Success = false, Mensaje = "SUNAT no devolvió un ticket para exportar la propuesta de compras" };

            var (nomArchivoReporte, codTipoArchivoReporte, codProcesoTicket, mensajeEspera) =
                await EsperarTicketTerminadoAsync(token, perTributario, numTicket, CodLibroRce);
            if (string.IsNullOrEmpty(nomArchivoReporte))
                return new SireDescargarPropuestaComprasResponse { Success = false, Mensaje = mensajeEspera, NumTicket = numTicket };

            var tokenDescarga = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret) ?? token;

            var (zipBytes, errorDescarga) = await DescargarArchivoReporteAsync(
                tokenDescarga, nomArchivoReporte, codTipoArchivoReporte, perTributario, codProcesoTicket ?? "01", numTicket, CodLibroRce);
            if (zipBytes is null)
                return new SireDescargarPropuestaComprasResponse { Success = false, Mensaje = errorDescarga, NumTicket = numTicket };

            var comprobantes = ExtraerComprobantesComprasDeZip(zipBytes);
            return new SireDescargarPropuestaComprasResponse { Success = true, NumTicket = numTicket, Comprobantes = comprobantes };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE][RCE] Error descargando propuesta de compras {Periodo}", perTributario);
            return new SireDescargarPropuestaComprasResponse { Success = false, Mensaje = ex.Message };
        }
    }

    public async Task<SireAceptarPropuestaResponse> AceptarPropuestaRceAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario)
    {
        var token = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret);
        if (string.IsNullOrEmpty(token))
            return new SireAceptarPropuestaResponse { Success = false, Mensaje = "No se pudo obtener el token de autenticación" };

        try
        {
            var url = string.Format(UrlAceptaPropuestaRce, perTributario);
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("User-Agent", UserAgent);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE][RCE] Error aceptando propuesta {Periodo}: {Status} {Content}", perTributario, response.StatusCode, content);
                return new SireAceptarPropuestaResponse { Success = false, Mensaje = ExtraerMensajeErrorSunat(response.StatusCode, content), RespuestaCruda = content };
            }

            string? numTicket = null;
            if (!string.IsNullOrWhiteSpace(content) && content.Trim() != "null")
            {
                try
                {
                    using var doc = JsonDocument.Parse(content);
                    numTicket = doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("numTicket", out var t)
                        ? t.GetString()
                        : null;
                }
                catch (JsonException) { /* respuesta no JSON, se trata como éxito vacío */ }
            }

            return new SireAceptarPropuestaResponse { Success = true, NumTicket = numTicket, RespuestaCruda = content };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE][RCE] Error aceptando propuesta {Periodo}", perTributario);
            return new SireAceptarPropuestaResponse { Success = false, Mensaje = ex.Message };
        }
    }

    public async Task<SireRegistrarPreliminarResponse> RegistrarPreliminarRceAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario)
    {
        var token = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret);
        if (string.IsNullOrEmpty(token))
            return new SireRegistrarPreliminarResponse { Success = false, Mensaje = "No se pudo obtener el token de autenticación" };

        try
        {
            var url = string.Format(UrlRegistraPreliminarRce, perTributario);
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("User-Agent", UserAgent);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE][RCE] Error registrando preliminar {Periodo}: {Status} {Content}", perTributario, response.StatusCode, content);
                return new SireRegistrarPreliminarResponse { Success = false, Mensaje = ExtraerMensajeErrorSunat(response.StatusCode, content), RespuestaCruda = content };
            }

            // Manual SIRE_Compras v28 §5.4: si la respuesta trae numTicket debe esperarse hasta codEstadoProceso 06=Terminado
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
                        var (_, _, _, mensajeTicket) = await EsperarTicketTerminadoAsync(token, perTributario, numTicket, CodLibroRce);
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
            _logger.LogError(ex, "[SIRE][RCE] Error registrando preliminar {Periodo}", perTributario);
            return new SireRegistrarPreliminarResponse { Success = false, Mensaje = ex.Message };
        }
    }

    public async Task<SireEliminarComprobanteResponse> EliminarComprobanteRceAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario, bool enPreliminar, List<SireComprobanteEliminarDto> comprobantes)
    {
        var token = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret);
        if (string.IsNullOrEmpty(token))
            return new SireEliminarComprobanteResponse { Success = false, Mensaje = "No se pudo obtener el token de autenticación" };

        try
        {
            var url = string.Format(enPreliminar ? UrlEliminarComprobantePreliminarRce : UrlEliminarComprobantePropuestaRce, perTributario);
            var detalle = comprobantes.Select(c => new
            {
                numSerieCDP = c.NumSerieCDP,
                numCDP = c.NumCDP,
                codCar = c.CodCar,
                codTipoCDP = c.CodTipoCDP
            });
            // Manual SIRE_Compras v28: §5.15 (propuesta) espera un array plano; §5.16 (preliminar) lo envuelve en "detalleAjustes"
            object body = enPreliminar ? new { detalleAjustes = detalle } : detalle;

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(enPreliminar ? HttpMethod.Post : HttpMethod.Delete, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("User-Agent", UserAgent);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE][RCE] Error eliminando comprobante {Periodo}: {Status} {Content}", perTributario, response.StatusCode, content);
                return new SireEliminarComprobanteResponse { Success = false, Mensaje = ExtraerMensajeErrorSunat(response.StatusCode, content) };
            }

            return new SireEliminarComprobanteResponse { Success = true, Mensaje = "Comprobante eliminado correctamente" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE][RCE] Error eliminando comprobante {Periodo}", perTributario);
            return new SireEliminarComprobanteResponse { Success = false, Mensaje = ex.Message };
        }
    }

    private async Task<string?> ExportarPropuestaAsync(string token, string perTributario)
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
            return null;
        }

        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.TryGetProperty("numTicket", out var t) ? t.GetString() : null;
    }

    // Manual v30 Anexo III - codEstadoProceso: 01=Cargado, 02=Validando, 03=Error, 04=Procesado OK, 05=En proceso, 06=Terminado
    // Retorna: (nomArchivoReporte, codTipoArchivoReporte, codProceso, mensaje)
    private async Task<(string? NomArchivoReporte, string? CodTipoArchivoReporte, string? CodProceso, string Mensaje)> EsperarTicketTerminadoAsync(
        string token, string perTributario, string numTicket, string codLibro = CodLibroRvie)
    {
        var url = string.Format(UrlConsultaEstadoTicket, perTributario, numTicket, codLibro);
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

                // Manual v30 §5.17: el codProceso para descargar el archivo (5.17) debe ser el que
                // devuelve ESTE servicio (5.16 registros[0].codProceso), no el de exportapropuesta (5.18).
                var codProcesoTicket = registro.TryGetProperty("codProceso", out var cp) ? cp.GetString() : null;

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

                return (nomArchivo, codTipoArchivo, codProcesoTicket, "Terminado");
            }

            if (estadoNorm == "3") // 03=Procesado con Errores
            {
                _logger.LogError("[SIRE] Ticket {Ticket} procesado con errores por SUNAT", numTicket);
                return (null, null, null, "SUNAT procesó el ticket con errores. Revisa los comprobantes e intenta nuevamente.");
            }
            // 01, 02, 05 → sigue esperando
        }

        return (null, null, null, "SUNAT aún está generando la propuesta. Intenta de nuevo en unos minutos.");
    }

    // Manual v30 §5.17: codTipoArchivoReporte viene de archivoReporte[0].codTipoAchivoReporte del ticket
    // Si es null, se envía "null" según indicación del manual
    private async Task<(byte[]? Bytes, string? Error)> DescargarArchivoReporteAsync(
        string token, string nomArchivoReporte, string? codTipoArchivoReporte, string perTributario, string codProceso, string numTicket, string codLibro = CodLibroRvie)
    {
        var codTipo = string.IsNullOrEmpty(codTipoArchivoReporte) ? "null" : codTipoArchivoReporte;
        var url = string.Format(UrlArchivoReporte, nomArchivoReporte, codTipo, perTributario, codProceso, numTicket, codLibro);
        var client = _httpClientFactory.CreateClient();

        const int intentos = 2;
        for (var intento = 1; intento <= intentos; intento++)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("User-Agent", UserAgent);

            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                return (await response.Content.ReadAsByteArrayAsync(), null);

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "[SIRE] Error descargando archivo reporte {Archivo} (intento {Intento}/{Total}): {Status} {Content}",
                nomArchivoReporte, intento, intentos, response.StatusCode, content);

            if (intento < intentos)
            {
                // SUNAT a veces marca el ticket como Terminado antes de que el archivo esté
                // realmente disponible para descarga; se reintenta una vez tras una breve espera.
                await Task.Delay(3000);
                continue;
            }

            var detalle = content.Length > 300 ? content[..300] : content;
            return (null, $"No se pudo descargar el archivo de la propuesta. SUNAT respondió {(int)response.StatusCode} ({response.StatusCode}): {detalle}");
        }

        return (null, "No se pudo descargar el archivo de la propuesta");
    }

    private List<SireComprobanteDto> ExtraerComprobantesDeZip(byte[] zipBytes)
    {
        var comprobantes = new List<SireComprobanteDto>();

        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return comprobantes;

        // Índices de columna según RS 112-2021/SUNAT, Anexo N°2 "Información de la propuesta RVIE"
        // (campo oficial N -> índice N-1 en el array, separador '|'). No coinciden con lo asumido
        // anteriormente (esa versión leía columnas de otros campos: BI Grav IVAP, ICBPER, Otros
        // Tributos, etc., que están vacías en comprobantes normales, de ahí Total=0 y Activo=false
        // siempre).
        using var reader = new StreamReader(entry.Open(), System.Text.Encoding.Latin1);
        string? linea;
        while ((linea = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(linea)) continue;

            var campos = linea.Split('|');
            if (campos.Length < 35) continue;
            // Fila de encabezado del TXT de SUNAT: el campo 1 (RUC emisor) no es numérico de 11 dígitos
            if (campos[0].Length != 11 || !campos[0].All(char.IsDigit)) continue;

            comprobantes.Add(new SireComprobanteDto
            {
                RucEmisor = campos[0],           // campo 1: RUC
                RazonSocialEmisor = campos[1],   // campo 2: ID
                Periodo = campos[2],             // campo 3: Periodo
                CarSunat = campos[3],             // campo 4: CAR SUNAT
                FechaEmision = campos[4],         // campo 5: Fecha de emisión
                FechaVctoPago = campos[5],        // campo 6: Fecha Vcto/Pago
                TipoComprobante = campos[6],      // campo 7: Tipo CP/Doc
                Serie = campos[7],                // campo 8: Serie del CDP
                Numero = campos[8],               // campo 9: Nro CP o Doc
                TipoDocCliente = campos[10],      // campo 11: Tipo Doc Identidad
                NumDocCliente = campos[11],       // campo 12: Nro Doc Identidad
                RazonSocialCliente = campos[12],  // campo 13: Apellidos Nombres/Razón Social
                BaseImponible = ParseDecimal(campos[14]), // campo 15: BI Gravada
                Igv = ParseDecimal(campos[16]),           // campo 17: IGV/IPM
                MtoExonerado = ParseDecimal(campos[18]),  // campo 19: Mto Exonerado
                MtoInafecto = ParseDecimal(campos[19]),   // campo 20: Mto Inafecto
                ImporteTotal = ParseDecimal(campos[25]),  // campo 26: Total CP
                CodMoneda = campos[26],                   // campo 27: Moneda
                TipoCambio = ParseDecimal(campos[27]),    // campo 28: Tipo Cambio
                FechaEmisionDocModificado = campos.Length > 28 && !string.IsNullOrWhiteSpace(campos[28]) ? campos[28] : null, // campo 29
                // Campo 35 "Est. Comp" (estado del comprobante). La RS 112-2021 no publica los códigos
                // de este campo para el RVIE, pero el Anexo N°1 modificado por la RS 040-2022/SUNAT sí
                // los documenta para el campo equivalente del RCE (Tabla 14 "Estado de Validez de
                // Comprobante de Pago o Documento"): 1=Activo, 2=Baja, 3=Revertido, 4=Anulado,
                // 5=Autorizado, 6=No Autorizado. Se usa ese mismo indicador aquí; se acepta "01" y el
                // texto literal como variantes de compatibilidad.
                Activo = EsComprobanteActivo(campos[34]),
                Inconsistencias = campos.Length > 38 && !string.IsNullOrWhiteSpace(campos[38]) ? campos[38] : null // campo 39: Incal
            });
        }

        return comprobantes;
    }

    // Índices de columna verificados contra un archivo real de propuesta RCE descargado en producción
    // (2026-09, RUC 20512134832, periodo 202608). El encabezado real es:
    // RUC|Apellidos y Nombres o Razón social|Periodo|CAR SUNAT|Fecha de emisión|Fecha Vcto/Pago|Tipo CP/Doc.|
    // Serie del CDP|Año|Nro CP o Doc. Nro Inicial (Rango)|Nro Final (Rango)|Tipo Doc Identidad|Nro Doc Identidad|
    // Apellidos Nombres/ Razón Social|BI Gravado DG|IGV / IPM DG|BI Gravado DGNG|IGV / IPM DGNG|BI Gravado DNG|
    // IGV / IPM DNG|Valor Adq. NG|ISC|ICBPER|Otros Trib/ Cargos|Total CP|Moneda|Tipo de Cambio|...|Est. Comp.|Incal|CLU1..39
    // A diferencia del RVIE, los campos 0-1 son el RUC/razón social del GENERADOR (la propia empresa, constante
    // en todas las filas) y no del proveedor: el proveedor está en los campos 11-13. Además "BI Gravado"/"IGV"
    // vienen partidos en 3 pares según el destino de la adquisición (DG/DGNG/DNG) en vez de una sola columna.
    private List<SireComprobanteCompraDto> ExtraerComprobantesComprasDeZip(byte[] zipBytes)
    {
        var comprobantes = new List<SireComprobanteCompraDto>();

        using var zipStream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return comprobantes;

        using var reader = new StreamReader(entry.Open(), System.Text.Encoding.Latin1);
        string? linea;
        var esPrimeraLinea = true;
        while ((linea = reader.ReadLine()) is not null)
        {
            if (esPrimeraLinea)
            {
                esPrimeraLinea = false;
                continue; // encabezado
            }
            if (string.IsNullOrWhiteSpace(linea)) continue;

            var campos = linea.Split('|');
            if (campos.Length < 41) continue;

            var rucProveedor = campos[12].Trim();
            if (string.IsNullOrWhiteSpace(rucProveedor)) continue;

            var tipoCambioTexto = campos[26].Trim();
            var incal = campos[40].Trim();

            comprobantes.Add(new SireComprobanteCompraDto
            {
                RucProveedor = rucProveedor,                          // campo 13: Nro Doc Identidad (proveedor)
                RazonSocialProveedor = campos[13].Trim(),              // campo 14: Apellidos Nombres/Razón Social (proveedor)
                Periodo = campos[2].Trim(),                            // campo 3: Periodo
                CarSunat = campos[3].Trim(),                           // campo 4: CAR SUNAT
                FechaEmision = campos[4].Trim(),                       // campo 5: Fecha de emisión
                TipoComprobante = campos[6].Trim(),                    // campo 7: Tipo CP/Doc.
                Serie = campos[7].Trim(),                              // campo 8: Serie del CDP
                Numero = campos[9].Trim(),                             // campo 10: Nro CP o Doc.
                BaseImponible = ParseDecimal(campos[14]) + ParseDecimal(campos[16]) + ParseDecimal(campos[18]), // BI Gravado DG+DGNG+DNG
                Igv = ParseDecimal(campos[15]) + ParseDecimal(campos[17]) + ParseDecimal(campos[19]),           // IGV/IPM DG+DGNG+DNG
                MtoExonerado = 0, // No existe un campo equivalente en el layout de compras
                MtoInafecto = ParseDecimal(campos[20]),                // campo 21: Valor Adq. NG (no gravadas)
                ImporteTotal = ParseDecimal(campos[24]),               // campo 25: Total CP
                CodMoneda = campos[25].Trim(),                         // campo 26: Moneda
                TipoCambio = string.IsNullOrWhiteSpace(tipoCambioTexto) ? null : ParseDecimal(tipoCambioTexto), // campo 27: Tipo de Cambio
                // Campo 40 "Est. Comp." (Anexo N°1, Tabla 14): 1=Activo, 2=Baja, 3=Revertido, 4=Anulado,
                // 5=Autorizado, 6=No Autorizado.
                Activo = EsComprobanteActivo(campos[39]),              // campo 40: Est. Comp.
                Inconsistencias = !string.IsNullOrWhiteSpace(incal) && incal != "0" ? incal : null // campo 41: Incal
            });
        }

        return comprobantes;
    }

    private static decimal ParseDecimal(string valor)
    {
        return decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static bool EsComprobanteActivo(string estadoCrudo)
    {
        var estado = estadoCrudo.Trim();
        return estado == "1" || estado == "01" || estado.Equals("Activo", StringComparison.OrdinalIgnoreCase);
    }

    // SUNAT devuelve errores como { "cod":"422", "msg":"...", "errors":[{ "cod":"2293", "msg":"<mensaje específico>" }] }
    // Se prioriza el mensaje específico de "errors" (más útil para el usuario) sobre el genérico del nivel raíz.
    private static string ExtraerMensajeErrorSunat(System.Net.HttpStatusCode status, string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return $"SUNAT respondió {status}";

            if (doc.RootElement.TryGetProperty("errors", out var errores) && errores.ValueKind == JsonValueKind.Array)
            {
                var primerError = errores.EnumerateArray().FirstOrDefault();
                if (primerError.ValueKind == JsonValueKind.Object
                    && primerError.TryGetProperty("msg", out var msgEspecifico)
                    && !string.IsNullOrWhiteSpace(msgEspecifico.GetString()))
                {
                    return msgEspecifico.GetString()!;
                }
            }

            if (doc.RootElement.TryGetProperty("msg", out var msgGeneral) && !string.IsNullOrWhiteSpace(msgGeneral.GetString()))
                return msgGeneral.GetString()!;

            return $"SUNAT respondió {status}";
        }
        catch (JsonException)
        {
            return $"SUNAT respondió {status}";
        }
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
                return new SireAceptarPropuestaResponse { Success = false, Mensaje = ExtraerMensajeErrorSunat(response.StatusCode, content), RespuestaCruda = content };
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
                return new SireRegistrarPreliminarResponse { Success = false, Mensaje = ExtraerMensajeErrorSunat(response.StatusCode, content), RespuestaCruda = content };
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
                        var (_, _, _, mensajeTicket) = await EsperarTicketTerminadoAsync(token, perTributario, numTicket);
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

    public async Task<SireEliminarComprobanteResponse> EliminarComprobanteAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario, bool enPreliminar, List<SireComprobanteEliminarDto> comprobantes)
    {
        var token = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret);
        if (string.IsNullOrEmpty(token))
            return new SireEliminarComprobanteResponse { Success = false, Mensaje = "No se pudo obtener el token de autenticación" };

        try
        {
            var url = string.Format(enPreliminar ? UrlEliminarComprobantePreliminar : UrlEliminarComprobantePropuesta, perTributario);
            var body = comprobantes.Select(c => new
            {
                numSerieCDP = c.NumSerieCDP,
                numCDP = c.NumCDP,
                codCar = c.CodCar,
                codTipoCDP = c.CodTipoCDP
            });

            var client = _httpClientFactory.CreateClient();
            // Manual v30: §5.13 (propuesta) usa DELETE, §5.14 (preliminar) usa POST
            var request = new HttpRequestMessage(enPreliminar ? HttpMethod.Post : HttpMethod.Delete, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("User-Agent", UserAgent);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE] Error eliminando comprobante {Periodo}: {Status} {Content}", perTributario, response.StatusCode, content);
                return new SireEliminarComprobanteResponse { Success = false, Mensaje = ExtraerMensajeErrorSunat(response.StatusCode, content) };
            }

            return new SireEliminarComprobanteResponse { Success = true, Mensaje = "Comprobante eliminado correctamente" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE] Error eliminando comprobante {Periodo}", perTributario);
            return new SireEliminarComprobanteResponse { Success = false, Mensaje = ex.Message };
        }
    }

    public async Task<SireImportarComprobantesResponse> ImportarComprobantesAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario, string razonSocialEmisor, bool enPreliminar, List<SireComprobanteNuevoDto> comprobantes)
    {
        var token = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret);
        if (string.IsNullOrEmpty(token))
            return new SireImportarComprobantesResponse { Success = false, Mensaje = "No se pudo obtener el token de autenticación" };

        try
        {
            var correlativo = DateTime.Now.ToString("HHmmss");
            var nombreBase = $"{ruc}-CPF-{perTributario}-{correlativo}";
            var txtBytes = ConstruirArchivoComprobantes(ruc, razonSocialEmisor, perTributario, comprobantes);
            var zipBytes = ComprimirArchivo($"{nombreBase}.txt", txtBytes);
            var nombreZip = $"{nombreBase}.zip";

            var url = enPreliminar ? UrlImportarPreliminar : UrlImportarPropuesta;
            // Anexo I: 1=Importar CP-Propuesta, 4=Importar CP-Preliminar
            var codProceso = enPreliminar ? "4" : "1";

            var (numTicket, errorTus) = await SubirArchivoTusAsync(token, url, nombreZip, ruc, perTributario, codProceso, zipBytes);
            if (string.IsNullOrEmpty(numTicket))
                return new SireImportarComprobantesResponse { Success = false, Mensaje = errorTus ?? "SUNAT no devolvió un ticket para el envío" };

            var (_, _, _, mensajeTicket) = await EsperarTicketTerminadoAsync(token, perTributario, numTicket);
            var exitoso = mensajeTicket == "Terminado";
            return new SireImportarComprobantesResponse
            {
                Success = exitoso,
                NumTicket = numTicket,
                Mensaje = exitoso ? "Comprobantes agregados correctamente" : mensajeTicket
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE] Error importando comprobantes {Periodo}", perTributario);
            return new SireImportarComprobantesResponse { Success = false, Mensaje = ex.Message };
        }
    }

    // RS 112-2021/SUNAT, Anexo N°2: 50 campos (1-33 y 41-57; los campos 34-40 son solo referenciales
    // en la propuesta descargada y no forman parte del archivo que la complementa).
    private static byte[] ConstruirArchivoComprobantes(string ruc, string razonSocialEmisor, string perTributario, List<SireComprobanteNuevoDto> comprobantes)
    {
        var sb = new StringBuilder();
        var culture = CultureInfo.InvariantCulture;

        foreach (var c in comprobantes)
        {
            var esNotaModificatoria = c.TipoComprobante is "07" or "08" or "87" or "88";

            var campos = new List<string>
            {
                ruc,                                                              // 1  RUC
                razonSocialEmisor,                                                // 2  ID
                perTributario,                                                    // 3  Periodo
                "",                                                               // 4  CAR SUNAT (lo asigna SUNAT automáticamente)
                c.FechaEmision,                                                   // 5  Fecha de emisión
                c.FechaVctoPago ?? "",                                            // 6  Fecha Vcto/Pago (solo tipo CP '14')
                c.TipoComprobante,                                                // 7  Tipo CP/Doc
                c.Serie,                                                          // 8  Serie del CDP
                c.Numero,                                                         // 9  Nro CP o Doc
                "",                                                               // 10 Nro Final (Rango) - solo tickets consolidados
                c.TipoDocCliente ?? "",                                           // 11 Tipo Doc Identidad
                c.NumDocCliente ?? "",                                            // 12 Nro Doc Identidad
                c.RazonSocialCliente ?? "",                                       // 13 Apellidos Nombres/Razón Social
                c.ValorFacturadoExportacion.ToString("0.00", culture),            // 14 Valor Facturado Exportación
                c.BaseImponible.ToString("0.00", culture),                        // 15 BI Gravada
                "0.00",                                                           // 16 Dscto BI
                c.Igv.ToString("0.00", culture),                                  // 17 IGV/IPM
                "0.00",                                                           // 18 Dscto IGV/IPM
                c.MtoExonerado.ToString("0.00", culture),                         // 19 Mto Exonerado
                c.MtoInafecto.ToString("0.00", culture),                          // 20 Mto Inafecto
                c.Isc.ToString("0.00", culture),                                  // 21 ISC
                "0.00",                                                           // 22 BI Grav IVAP
                "0.00",                                                           // 23 IVAP
                c.Icbper.ToString("0.00", culture),                               // 24 ICBPER
                "0.00",                                                           // 25 Otros Tributos
                c.ImporteTotal.ToString("0.00", culture),                         // 26 Total CP
                c.CodMoneda,                                                      // 27 Moneda
                c.TipoCambio.HasValue ? c.TipoCambio.Value.ToString("0.000", culture) : "", // 28 Tipo Cambio
                esNotaModificatoria ? (c.FechaEmisionDocModificado ?? "") : "",   // 29 Fecha Emisión Doc Modificado
                esNotaModificatoria ? (c.TipoCPModificado ?? "") : "",            // 30 Tipo CP Modificado
                esNotaModificatoria ? (c.SerieCPModificado ?? "") : "",           // 31 Serie CP Modificado
                esNotaModificatoria ? (c.NroCPModificado ?? "") : "",             // 32 Nro CP Modificado
                "",                                                               // 33 ID Proyecto Operadores Atribución
            };

            // Campos 41 a 57: de libre utilización, se dejan vacíos
            campos.AddRange(Enumerable.Repeat("", 17));

            sb.Append(string.Join("|", campos));
            sb.Append("\r\n");
        }

        return Encoding.Latin1.GetBytes(sb.ToString());
    }

    private static byte[] ComprimirArchivo(string nombreEntrada, byte[] contenido)
    {
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(nombreEntrada, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(contenido, 0, contenido.Length);
        }
        return zipStream.ToArray();
    }

    // Protocolo TUS 1.0 (https://tus.io) - Manual v30 §6 "Documentación TUS.IO":
    // 1) POST crea la sesión de subida con metadata en base64 -> SUNAT responde con Location (URL de subida)
    // 2) PATCH sube el contenido completo (los .zip de comprobantes son pequeños, no requiere trocear)
    // NOTA: el manual no muestra el cuerpo JSON exacto de la respuesta final; se asume "numTicket" por
    // consistencia con el resto de servicios. Verificar contra una respuesta real antes de confiar en producción.
    private async Task<(string? NumTicket, string? Error)> SubirArchivoTusAsync(
        string token, string url, string nombreArchivo, string ruc, string perTributario, string codProceso, byte[] contenido)
    {
        var client = _httpClientFactory.CreateClient();

        var metadata = new Dictionary<string, string>
        {
            ["filename"] = nombreArchivo,
            ["filetype"] = "application/zip",
            ["numRuc"] = ruc,
            ["perTributario"] = perTributario,
            ["codOrigenEnvio"] = "2",
            ["codProceso"] = codProceso,
            ["codTipoCorrelativo"] = "01",
            ["nomArchivoImportacion"] = nombreArchivo,
            ["codLibro"] = "140000"
        };
        var uploadMetadata = string.Join(",", metadata.Select(kv => $"{kv.Key} {Convert.ToBase64String(Encoding.UTF8.GetBytes(kv.Value))}"));

        var crear = new HttpRequestMessage(HttpMethod.Post, url);
        crear.Headers.Add("Authorization", $"Bearer {token}");
        crear.Headers.Add("User-Agent", UserAgent);
        crear.Headers.Add("Tus-Resumable", "1.0.0");
        crear.Headers.Add("Upload-Length", contenido.Length.ToString());
        crear.Headers.Add("Upload-Metadata", uploadMetadata);

        var respCrear = await client.SendAsync(crear);
        if (!respCrear.IsSuccessStatusCode || respCrear.Headers.Location is null)
        {
            var detalle = await respCrear.Content.ReadAsStringAsync();
            _logger.LogError("[SIRE] Error creando sesión TUS: {Status} {Content}", respCrear.StatusCode, detalle);
            return (null, $"No se pudo iniciar la subida del archivo. SUNAT respondió {(int)respCrear.StatusCode}: {detalle}");
        }

        var uploadUrl = respCrear.Headers.Location.IsAbsoluteUri
            ? respCrear.Headers.Location
            : new Uri(new Uri(url), respCrear.Headers.Location);

        var subir = new HttpRequestMessage(HttpMethod.Patch, uploadUrl);
        subir.Headers.Add("Authorization", $"Bearer {token}");
        subir.Headers.Add("User-Agent", UserAgent);
        subir.Headers.Add("Tus-Resumable", "1.0.0");
        subir.Headers.Add("Upload-Offset", "0");
        subir.Content = new ByteArrayContent(contenido);
        subir.Content.Headers.ContentType = new MediaTypeHeaderValue("application/offset+octet-stream");

        var respSubir = await client.SendAsync(subir);
        var contenidoRespuesta = await respSubir.Content.ReadAsStringAsync();

        if (!respSubir.IsSuccessStatusCode)
        {
            _logger.LogError("[SIRE] Error subiendo archivo TUS: {Status} {Content}", respSubir.StatusCode, contenidoRespuesta);
            return (null, $"SUNAT rechazó el archivo. Respondió {(int)respSubir.StatusCode}: {contenidoRespuesta}");
        }

        try
        {
            using var doc = JsonDocument.Parse(contenidoRespuesta);
            var numTicket = doc.RootElement.TryGetProperty("numTicket", out var t) ? t.GetString() : null;
            return (numTicket, numTicket is null ? $"SUNAT no devolvió un ticket tras la subida: {contenidoRespuesta}" : null);
        }
        catch (JsonException)
        {
            return (null, $"SUNAT no devolvió una respuesta JSON válida tras la subida: {contenidoRespuesta}");
        }
    }

    // Manual v30 §5.11/§5.12: el parámetro "codMoneda" del request está tipado como "numérico-String",
    // a diferencia del campo 27 (Moneda) de la propuesta descargada, que es alfa ISO 4217 (PEN/USD) según
    // RS 112-2021 Anexo N°2. Se traduce al código ISO 4217 numérico antes de enviarlo a SUNAT.
    private static string MapearCodigoMonedaNumerico(string codigoAlpha) => codigoAlpha?.Trim().ToUpperInvariant() switch
    {
        "PEN" => "604",
        "USD" => "840",
        _ => codigoAlpha ?? string.Empty
    };

    public async Task<SireEditarTipoCambioResponse> EditarTipoCambioAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario, SireEditarTipoCambioDto datos)
    {
        var token = await ObtenerTokenAsync(ruc, solUsuario, solClave, clienteId, clientSecret);
        if (string.IsNullOrEmpty(token))
            return new SireEditarTipoCambioResponse { Success = false, Mensaje = "No se pudo obtener el token de autenticación" };

        try
        {
            var url = string.Format(UrlEditarTipoCambioIndividual, perTributario);
            var body = new Dictionary<string, object?>
            {
                ["codCar"] = datos.CodCar,
                ["codMoneda"] = MapearCodigoMonedaNumerico(datos.CodMoneda),
                ["mtoTipoCambio"] = datos.MtoTipoCambio,
                ["mtoCambioMonedaExt"] = datos.MtoCambioMonedaExt
            };

            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("User-Agent", UserAgent);
            request.Headers.Add("Accept", "application/json");

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[SIRE] Error editando tipo de cambio {Periodo}: {Status} {Content}", perTributario, response.StatusCode, content);
                return new SireEditarTipoCambioResponse { Success = false, Mensaje = ExtraerMensajeErrorSunat(response.StatusCode, content) };
            }

            return new SireEditarTipoCambioResponse { Success = true, Mensaje = "Tipo de cambio actualizado correctamente" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SIRE] Error editando tipo de cambio {Periodo}", perTributario);
            return new SireEditarTipoCambioResponse { Success = false, Mensaje = ex.Message };
        }
    }
}
