namespace IdeatecAPI.Application.Common.Interfaces;

public interface IWebSocketNotifier
{
    Task NotifyAsync(int? sucursalId, string? empresaRuc, string evento = "all");
    Task NotifyWithDelayAsync(int? sucursalId, string? empresaRuc, int delaySeconds = 6, string evento = "all");
}