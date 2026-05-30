using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface INotificacionDiasRepository : IRepository<NotificacionDias>
{
    Task<IEnumerable<NotificacionDias>> GetAllNotificacionDiasAsync();
    Task<bool> RegistrarNotificacionDiasAsync(NotificacionDias notificacionDias);
    Task<bool> EditarNotificacionDiasAsync(NotificacionDias notificacionDias);
}
