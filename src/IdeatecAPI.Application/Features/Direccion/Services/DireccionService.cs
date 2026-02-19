using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Direccion.DTOs;

namespace IdeatecAPI.Application.Features.Direccion.Services;

public interface IDireccionService
{
    Task CrearDireccionAsync(RegistrarDireccionDTO dto);
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
        var direccion = new Domain.Entities.Cliente.Direccion
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
}