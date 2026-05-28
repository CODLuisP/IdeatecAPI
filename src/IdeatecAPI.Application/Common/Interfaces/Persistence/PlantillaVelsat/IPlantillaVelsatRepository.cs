// IdeatecAPI.Application.Common.Interfaces.Persistence/IPlantillaVelsatRepository.cs
using IdeatecAPI.Application.Features.PlantillaVelsat.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IPlantillaVelsatRepository
{
    Task<IEnumerable<PlantillaVelsat>> GetAllAsync(string? periodo = null);
    Task<PlantillaVelsat?> GetByIdAsync(int id);
    Task<PlantillaVelsat> CrearAsync(PlantillaVelsat plantilla);
    Task<bool> EditarAsync(PlantillaVelsat plantilla, EditarPlantillaVelsatDTO dto);
    Task<bool> EliminarAsync(int id); // Cambia estado a 1
}