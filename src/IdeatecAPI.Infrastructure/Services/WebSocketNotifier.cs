using IdeatecAPI.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace IdeatecAPI.Infrastructure.Services;

public class WebSocketNotifier : IWebSocketNotifier
{
    private readonly HttpClient _httpClient;
    private readonly string _notifyUrl;

    public WebSocketNotifier(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _notifyUrl = config["WebSocket:NotifyUrl"] ?? "http://localhost:8080/notify";
    }

   public async Task NotifyAsync(int? sucursalId, string? empresaRuc, string evento = "all")
{
    try
    {
        var payload = JsonSerializer.Serialize(new { sucursalId, empresaRuc, evento });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await _httpClient.PostAsync(_notifyUrl, content, cts.Token);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ WebSocket notify falló: {ex.Message}");
    }
}

public async Task NotifyWithDelayAsync(int? sucursalId, string? empresaRuc, int delaySeconds = 4, string evento = "all")
{
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        await NotifyAsync(sucursalId, empresaRuc, evento);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ WebSocket notify con delay falló: {ex.Message}");
    }
}
}