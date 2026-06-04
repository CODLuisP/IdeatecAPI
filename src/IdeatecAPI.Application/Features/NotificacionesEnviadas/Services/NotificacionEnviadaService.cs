using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.NotificacionesEnviadas.DTOs;

namespace IdeatecAPI.Application.Features.NotificacionesEnviadas.Services;

public interface INotificacionEnviadaService
{
    Task<IEnumerable<NotificacionEnviadaDto>> GetAllAsync();
    Task<bool> RegistrarAsync(RegistrarNotificacionEnviadaDto dto);
    Task<bool> EditarAsync(int id, EditarNotificacionEnviadaDto dto);
    Task<bool> EliminarAsync(int id);
}

public class NotificacionEnviadaService : INotificacionEnviadaService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificacionEnviadaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<NotificacionEnviadaDto>> GetAllAsync()
    {
        var notificaciones = await _unitOfWork.NotificacionesEnviadas.GetAllAsync();

        return notificaciones.Select(n => new NotificacionEnviadaDto
        {
            Id              = n.Id,
            EmailEnviado    = n.EmailEnviado,
            WhatsappEnviado = n.WhatsappEnviado
        });
    }

    public async Task<bool> RegistrarAsync(RegistrarNotificacionEnviadaDto dto)
    {
        if (dto.Id <= 0)
            throw new ArgumentException("El Id es obligatorio y debe ser mayor a 0.");

        _unitOfWork.BeginTransaction();
        try
        {
            var notificacion = new Domain.Entities.NotificacionEnviada
            {
                Id              = dto.Id,
                EmailEnviado    = dto.EmailEnviado,
                WhatsappEnviado = dto.WhatsappEnviado
            };

            var result = await _unitOfWork.NotificacionesEnviadas.RegistrarAsync(notificacion);
            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarAsync(int id, EditarNotificacionEnviadaDto dto)
    {
        if (id <= 0)
            throw new ArgumentException("Id inválido.");

        _unitOfWork.BeginTransaction();
        try
        {
            var notificacion = new Domain.Entities.NotificacionEnviada
            {
                Id              = id,
                EmailEnviado    = dto.EmailEnviado,
                WhatsappEnviado = dto.WhatsappEnviado
            };

            var result = await _unitOfWork.NotificacionesEnviadas.EditarAsync(notificacion);
            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EliminarAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Id inválido.");

        _unitOfWork.BeginTransaction();
        try
        {
            var result = await _unitOfWork.NotificacionesEnviadas.EliminarAsync(id);
            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }
}
