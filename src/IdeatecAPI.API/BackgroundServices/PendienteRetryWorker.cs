using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Comprobante.Services;
using IdeatecAPI.Application.Features.Notas.Services;
using IdeatecAPI.Infrastructure.Services;

namespace IdeatecAPI.API.BackgroundServices;

public class PendienteRetryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RetryLogStore _logStore;
    private readonly string[] _environments = { "production", "beta" };

    public PendienteRetryWorker(IServiceScopeFactory scopeFactory, RetryLogStore logStore)
    {
        _scopeFactory = scopeFactory;
        _logStore = logStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logStore.Status.IsRunning)
            {
                try
                {
                    await RunCycleAsync(stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logStore.Add(new RetryLogEntry(
                        DateTime.Now, "error", "ERROR_CICLO",
                        Detalle: ex.Message));
                }
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(_logStore.Status.IntervaloMinutos),
                    stoppingToken);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        var inicio = DateTime.Now;
        int total = 0, aceptados = 0;

        _logStore.Add(new RetryLogEntry(inicio, "ciclo", "INICIO"));

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork          = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var comprobanteService  = scope.ServiceProvider.GetRequiredService<IComprobanteService>();
        var noteService         = scope.ServiceProvider.GetRequiredService<INoteService>();

        var incluirResumen = _logStore.Status.IncluirEnviadoEnResumen;

        foreach (var env in _environments)
        {
            if (ct.IsCancellationRequested) break;

            unitOfWork.SetEnvironment(env);

            var compIds = (await unitOfWork.RetryRobot.GetPendientesComprobantesAsync(incluirResumen)).ToList();
            var noteIds = (await unitOfWork.RetryRobot.GetPendientesNotasAsync(incluirResumen)).ToList();

            total += compIds.Count + noteIds.Count;

            foreach (var id in compIds)
            {
                if (ct.IsCancellationRequested) break;
                aceptados += await ProcesarComprobanteAsync(id, comprobanteService);
            }

            foreach (var id in noteIds)
            {
                if (ct.IsCancellationRequested) break;
                aceptados += await ProcesarNotaAsync(id, noteService);
            }
        }

        var duracion = (int)(DateTime.Now - inicio).TotalSeconds;
        _logStore.Status.UltimoCiclo           = inicio;
        _logStore.Status.UltimoCicloTotal      = total;
        _logStore.Status.UltimoCicloAceptados  = aceptados;

        _logStore.Add(new RetryLogEntry(
            DateTime.Now, "ciclo", "FIN",
            Detalle: $"Total: {total} | Aceptados: {aceptados} | Duración: {duracion}s"));
    }

    private async Task<int> ProcesarComprobanteAsync(int id, IComprobanteService service)
    {
        try
        {
            var result  = await service.SendToSunatAsync(id);
            var estado  = result.EstadoSunat ?? "PENDIENTE";
            var aceptado = estado is "ACEPTADO" or "ACEPTADO_CON_OBSERVACIONES";

            _logStore.Add(new RetryLogEntry(
                DateTime.Now, "comprobante", estado,
                NumeroCompleto: $"{result.Serie}-{result.Correlativo}",
                Detalle: result.MensajeRespuesta));

            return aceptado ? 1 : 0;
        }
        catch (Exception ex)
        {
            _logStore.Add(new RetryLogEntry(
                DateTime.Now, "comprobante", "ERROR",
                Detalle: ex.Message));
            return 0;
        }
    }

    private async Task<int> ProcesarNotaAsync(int id, INoteService service)
    {
        try
        {
            var result  = await service.SendToSunatAsync(id);
            var estado  = result.EstadoSunat ?? "PENDIENTE";
            var aceptado = estado is "ACEPTADO" or "ACEPTADO_CON_OBSERVACIONES";

            _logStore.Add(new RetryLogEntry(
                DateTime.Now, "nota", estado,
                NumeroCompleto: result.NumeroCompleto,
                Detalle: result.MensajeRespuestaSunat));

            return aceptado ? 1 : 0;
        }
        catch (Exception ex)
        {
            _logStore.Add(new RetryLogEntry(
                DateTime.Now, "nota", "ERROR",
                Detalle: ex.Message));
            return 0;
        }
    }
}
