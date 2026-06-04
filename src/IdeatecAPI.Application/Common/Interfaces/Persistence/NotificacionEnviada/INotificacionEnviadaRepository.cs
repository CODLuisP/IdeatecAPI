using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface INotificacionEnviadaRepository : IRepository<NotificacionEnviada>
{
    Task<IEnumerable<NotificacionEnviada>> GetAllAsync();
    Task<bool> RegistrarAsync(NotificacionEnviada notificacion);
    Task<bool> EditarAsync(NotificacionEnviada notificacion);
    Task<bool> EliminarAsync(int id);
}
