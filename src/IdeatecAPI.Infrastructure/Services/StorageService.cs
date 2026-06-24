using IdeatecAPI.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace IdeatecAPI.Infrastructure.Services;

public class StorageService : IStorageService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _microservicioUrl;

    // Mapeo de códigos SUNAT a nombres de carpeta
    private static readonly Dictionary<string, string> TiposCarpeta = new()
    {
        { "01", "facturas" },
        { "03", "boletas" },
        { "07", "notas-credito" },
        { "08", "notas-debito" },
        { "09", "guias-remision" },
        { "31", "guia-transportista" }
    };

    public StorageService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _microservicioUrl = configuration["Storage:MicroservicioUrl"]
            ?? throw new InvalidOperationException("Storage:MicroservicioUrl no configurado");
    }

    public async Task<string> SubirZipAsync(string ruc, string tipoComprobante, string nombreArchivo, byte[] zipBytes, string entorno)
    {
        var tipo = TiposCarpeta.GetValueOrDefault(tipoComprobante, tipoComprobante);
        var filename = $"{nombreArchivo}.zip";

        await SubirAlMicroservicio(ruc, tipo, filename, zipBytes, entorno);

        return $"/{entorno}/{ruc}/{tipo}/{filename}";
    }

    public async Task<string> SubirCdrAsync(string ruc, string tipoComprobante, string nombreArchivo, string cdrBase64, string entorno)
    {
        var tipo = TiposCarpeta.GetValueOrDefault(tipoComprobante, tipoComprobante);
        var cdrBytes = Convert.FromBase64String(cdrBase64);
        var filename = $"R-{nombreArchivo}.zip";

        await SubirAlMicroservicio(ruc, tipo, filename, cdrBytes, entorno);

        return $"/{entorno}/{ruc}/{tipo}/{filename}";
    }

    private async Task SubirAlMicroservicio(string ruc, string tipo, string filename, byte[] zipBytes, string entorno)
    {
        var client = _httpClientFactory.CreateClient();

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(zipBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");

        form.Add(fileContent, "file", filename);
        form.Add(new StringContent(ruc), "ruc");
        form.Add(new StringContent(tipo), "tipo");
        form.Add(new StringContent(entorno), "entorno");

        var response = await client.PostAsync($"{_microservicioUrl}/files/upload", form);
        response.EnsureSuccessStatusCode();
    }
}