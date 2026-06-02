using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdeatecAPI.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace IdeatecAPI.Infrastructure.Services;

public class TipoCambioService : ITipoCambioService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiUrl;
    private readonly string _apiKey;

    public TipoCambioService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _apiUrl = configuration["TipoCambio:ApiUrl"] ?? "https://api.json.pe/api/tipo_de_cambio";
        _apiKey = configuration["TipoCambio:ApiKey"] ?? "";
    }

    public async Task<decimal?> GetTipoCambioVentaAsync(DateTime? fecha = null)
    {
        try
        {
            var client  = _httpClientFactory.CreateClient();
            var fechaStr = (fecha ?? DateTime.Today).ToString("yyyy-MM-dd");

            var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { fecha = fechaStr }),
                Encoding.UTF8,
                "application/json");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json   = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TipoCambioApiResponse>(json);

            return result?.Success == true ? result.Data?.Venta : null;
        }
        catch
        {
            return null;
        }
    }
}

// ── Modelos de respuesta ────────────────────────────────────────────────────

file sealed class TipoCambioApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public TipoCambioApiData? Data { get; set; }
}

file sealed class TipoCambioApiData
{
    [JsonPropertyName("venta")]
    public decimal Venta { get; set; }

    [JsonPropertyName("compra")]
    public decimal Compra { get; set; }
}
