using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface INotificacionEnviadaRepository : IRepository<NotificacionEnviada>
{
    Task<IEnumerable<NotificacionEnviada>> GetAllNotificacionesEnviadasAsync();
    Task<bool> RegistrarNotificacionEnviadaAsync(NotificacionEnviada notificacion);
    Task<bool> EditarNotificacionEnviadaAsync(NotificacionEnviada notificacion);
}
