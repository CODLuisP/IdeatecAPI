using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.NotificacionDias.DTOs;

namespace IdeatecAPI.Application.Features.NotificacionDias.Services;

public interface INotificacionDiasService
{
    Task<IEnumerable<NotificacionDiasDto>> GetAllNotificacionDiasAsync();
    Task<bool> RegistrarNotificacionDiasAsync(RegistrarNotificacionDiasDto dto);
    Task<bool> EditarNotificacionDiasAsync(int id, EditarNotificacionDiasDto dto);
}

public class NotificacionDiasService : INotificacionDiasService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificacionDiasService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<NotificacionDiasDto>> GetAllNotificacionDiasAsync()
    {
        var registros = await _unitOfWork.NotificacionDias.GetAllNotificacionDiasAsync();

        return registros.Select(n => new NotificacionDiasDto
        {
            Id = n.Id,
            Periodo = n.Periodo,
            Dias = n.Dias
        });
    }

    public async Task<bool> RegistrarNotificacionDiasAsync(RegistrarNotificacionDiasDto dto)
    {
        _unitOfWork.BeginTransaction();

        try
        {
            var notificacionDias = new Domain.Entities.NotificacionDias
            {
                Periodo = dto.Periodo,
                Dias = dto.Dias
            };

            var result = await _unitOfWork.NotificacionDias.RegistrarNotificacionDiasAsync(notificacionDias);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarNotificacionDiasAsync(int id, EditarNotificacionDiasDto dto)
    {
        if (id <= 0)
            throw new ArgumentException("Id inválido");

        _unitOfWork.BeginTransaction();

        try
        {
            var notificacionDias = new Domain.Entities.NotificacionDias
            {
                Id = id,
                Periodo = dto.Periodo,
                Dias = dto.Dias
            };

            var result = await _unitOfWork.NotificacionDias.EditarNotificacionDiasAsync(notificacionDias);

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
