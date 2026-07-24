namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IRetryRobotRepository
{
    Task<IEnumerable<int>> GetPendientesComprobantesAsync(bool incluirEnviadoEnResumen);
    Task<IEnumerable<int>> GetPendientesNotasAsync(bool incluirEnviadoEnResumen);
}
