using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Direccion.DTOs;

namespace IdeatecAPI.Application.Features.Direccion.Services;

public interface IDireccionService
{
    Task CrearDireccionAsync(RegistrarDireccionDTO dto);
    Task<bool> EditarDireccionAsync(DireccionDTO dto);
    Task<bool> EliminarDireccionAsync(int direccionId);
}

public class DireccionService : IDireccionService
{
    private readonly IUnitOfWork _unitOfWork;

    public DireccionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task CrearDireccionAsync(RegistrarDireccionDTO dto)
    {
        var direccion = new Domain.Entities.Direccion
        {
            ClienteId = dto.ClienteId,
            DireccionLineal = dto.DireccionLineal,
            Ubigeo = dto.Ubigeo,
            Departamento = dto.Departamento,
            Provincia = dto.Provincia,
            Distrito = dto.Distrito,
            TipoDireccion = dto.TipoDireccion
        };

        await _unitOfWork.Direcciones.CrearDireccionAsync(direccion);
    }

    public async Task<bool> EditarDireccionAsync(DireccionDTO dto)
    {
        var direccion = new Domain.Entities.Direccion
        {
            DireccionId = dto.DireccionId,
            DireccionLineal = dto.DireccionLineal,
            Ubigeo = dto.Ubigeo,
            Departamento = dto.Departamento,
            Provincia = dto.Provincia,
            Distrito = dto.Distrito,
            TipoDireccion = dto.TipoDireccion
        };

        var actualizado = await _unitOfWork.Direcciones
            .EditarDireccionAsync(direccion);

        return actualizado;
    }

    public async Task<bool> EliminarDireccionAsync(int direccionId)
{
    _unitOfWork.BeginTransaction();

    try
    {
        var eliminado = await _unitOfWork.Direcciones
            .EliminarDireccionAsync(direccionId);

        if (!eliminado)
            throw new Exception("Dirección no encontrada");

        _unitOfWork.Commit();
        return true;
    }
    catch
    {
        _unitOfWork.Rollback();
        throw;
    }
}
}