using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Vales.DTOs;

namespace IdeatecAPI.Application.Features.Vales.Services;

public interface IValeService
{
    Task<IEnumerable<ValeDto>> GetAllValesAsync();
    Task<bool> RegistrarValeAsync(RegistrarValeDto dto);
    Task<bool> EditarValeAsync(int idVale, EditarValeDto dto);
    Task<bool> EliminarValeAsync(int idVale);
}

public class ValeService : IValeService
{
    private readonly IUnitOfWork _unitOfWork;

    public ValeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ValeDto>> GetAllValesAsync()
    {
        var vales = await _unitOfWork.Vales.GetAllValesAsync();

        return vales.Select(v => new ValeDto
        {
            IdVale       = v.IdVale,
            Nombre       = v.Nombre,
            Descripcion  = v.Descripcion,
            FechaEmision = v.FechaEmision,
            Duracion     = v.Duracion,
            Estado       = v.Estado
        });
    }

    public async Task<bool> RegistrarValeAsync(RegistrarValeDto dto)
    {
        _unitOfWork.BeginTransaction();

        try
        {
            var limaZone = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Lima");

            var vale = new Domain.Entities.Vale
            {
                Nombre       = dto.Nombre,
                Descripcion  = dto.Descripcion,
                FechaEmision = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, limaZone),
                Duracion     = dto.Duracion,
                Estado       = true
            };

            var result = await _unitOfWork.Vales.RegistrarValeAsync(vale);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarValeAsync(int idVale, EditarValeDto dto)
    {
        if (idVale <= 0)
            throw new ArgumentException("IdVale inválido");

        _unitOfWork.BeginTransaction();

        try
        {
            var vale = new Domain.Entities.Vale
            {
                Nombre       = dto.Nombre,
                Descripcion  = dto.Descripcion,
                FechaEmision = dto.FechaEmision,
                Duracion     = dto.Duracion,
                Estado       = dto.Estado
            };

            var result = await _unitOfWork.Vales.EditarValeAsync(idVale, vale);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EliminarValeAsync(int idVale)
    {
        if (idVale <= 0)
            throw new ArgumentException("IdVale inválido");

        _unitOfWork.BeginTransaction();

        try
        {
            var result = await _unitOfWork.Vales.EliminarValeAsync(idVale);

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
