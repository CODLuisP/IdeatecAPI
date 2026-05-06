namespace IdeatecAPI.Application.Common.Interfaces;

public interface IWebSocketNotifier
{
    Task NotifyAsync(int? sucursalId, string? empresaRuc);

    Task NotifyWithDelayAsync(int? sucursalId, string? empresaRuc, int delaySeconds = 6);

}