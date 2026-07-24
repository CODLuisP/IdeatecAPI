using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;

namespace IdeatecAPI.Infrastructure.Services;

public record RetryLogEntry(
    DateTime Timestamp,
    string Tipo,        // "ciclo" | "comprobante" | "nota" | "error"
    string Resultado,   // "INICIO" | "FIN" | "ACEPTADO" | "PENDIENTE" | "RECHAZADO" | "ERROR" | "ERROR_CICLO"
    string? NumeroCompleto = null,
    string? Detalle = null
);

public class RetryRobotStatus
{
    public bool IsRunning { get; set; }
    public int IntervaloMinutos { get; set; }
    public bool IncluirEnviadoEnResumen { get; set; }
    public DateTime? UltimoCiclo { get; set; }
    public int UltimoCicloTotal { get; set; }
    public int UltimoCicloAceptados { get; set; }
}

public class RetryLogStore
{
    private const int MaxEntries = 500;
    private readonly ConcurrentQueue<RetryLogEntry> _logs = new();

    public RetryRobotStatus Status { get; }

    public RetryLogStore(IConfiguration configuration)
    {
        var cfg = configuration.GetSection("RetryRobot");
        Status = new RetryRobotStatus
        {
            IsRunning = true,
            IntervaloMinutos = cfg.GetValue<int>("IntervaloMinutos", 5),
            IncluirEnviadoEnResumen = cfg.GetValue<bool>("IncluirEnviadoEnResumen", true)
        };
    }

    public void Add(RetryLogEntry entry)
    {
        _logs.Enqueue(entry);
        while (_logs.Count > MaxEntries)
            _logs.TryDequeue(out _);
    }

    public IEnumerable<RetryLogEntry> GetLogs(int count = 100)
        => _logs.TakeLast(Math.Min(count, MaxEntries));
}
