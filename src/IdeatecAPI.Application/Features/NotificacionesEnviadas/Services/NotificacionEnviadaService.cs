using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.NotificacionesEnviadas.DTOs;

namespace IdeatecAPI.Application.Features.NotificacionesEnviadas.Services;

public interface INotificacionEnviadaService
{
    Task<IEnumerable<NotificacionEnviadaDto>> GetAllNotificacionesEnviadasAsync();
    Task<bool> RegistrarNotificacionEnviadaAsync(RegistrarNotificacionEnviadaDto dto);
    Task<bool> EditarNotificacionEnviadaAsync(int id, EditarNotificacionEnviadaDto dto);
}

public class NotificacionEnviadaService : INotificacionEnviadaService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificacionEnviadaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<NotificacionEnviadaDto>> GetAllNotificacionesEnviadasAsync()
    {
        var notificaciones = await _unitOfWork.NotificacionesEnviadas.GetAllNotificacionesEnviadasAsync();

        return notificaciones.Select(n => new NotificacionEnviadaDto
        {
            Id = n.Id,
            NumDoc = n.NumDoc,
            PeriodoTipo = n.PeriodoTipo,
            Moneda = n.Moneda,
            TipoDoc = n.TipoDoc,
            EmailEnviado = n.EmailEnviado,
            WhatsappEnviado = n.WhatsappEnviado,
            FechaFin = n.FechaFin,
            FechaEnvio = n.FechaEnvio,
            UsuarioId = n.UsuarioId
        });
    }

    public async Task<bool> RegistrarNotificacionEnviadaAsync(RegistrarNotificacionEnviadaDto dto)
    {
        _unitOfWork.BeginTransaction();

        try
        {
            var notificacion = new Domain.Entities.NotificacionEnviada
            {
                NumDoc = dto.NumDoc,
                PeriodoTipo = dto.PeriodoTipo,
                Moneda = dto.Moneda,
                TipoDoc = dto.TipoDoc,
                EmailEnviado = dto.EmailEnviado,
                WhatsappEnviado = dto.WhatsappEnviado,
                FechaFin = dto.FechaFin,
                FechaEnvio = dto.FechaEnvio,
                UsuarioId = dto.UsuarioId
            };

            var result = await _unitOfWork.NotificacionesEnviadas.RegistrarNotificacionEnviadaAsync(notificacion);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarNotificacionEnviadaAsync(int id, EditarNotificacionEnviadaDto dto)
    {
        if (id <= 0)
            throw new ArgumentException("Id inválido");

        _unitOfWork.BeginTransaction();

        try
        {
            var notificacion = new Domain.Entities.NotificacionEnviada
            {
                Id = id,
                NumDoc = dto.NumDoc,
                PeriodoTipo = dto.PeriodoTipo,
                Moneda = dto.Moneda,
                TipoDoc = dto.TipoDoc,
                EmailEnviado = dto.EmailEnviado,
                WhatsappEnviado = dto.WhatsappEnviado,
                FechaFin = dto.FechaFin,
                FechaEnvio = dto.FechaEnvio,
                UsuarioId = dto.UsuarioId
            };

            var result = await _unitOfWork.NotificacionesEnviadas.EditarNotificacionEnviadaAsync(notificacion);

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
