// IdeatecAPI.Application.Features.PlantillaVelsat/Services/IPlantillaVelsatService.cs + PlantillaVelsatService.cs
using System;
using System.Linq;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.PlantillaVelsat.DTOs;
using DomainPlantillaVelsat = IdeatecAPI.Domain.Entities.PlantillaVelsat;

namespace IdeatecAPI.Application.Features.PlantillaVelsat.Services;

public interface IPlantillaVelsatService
{
    Task<IEnumerable<ObtenerPlantillaVelsatDTO>> GetAllAsync(string? periodo = null);
    Task<ObtenerPlantillaVelsatDTO> CrearAsync(CrearPlantillaVelsatDTO dto);
    Task<bool> EditarAsync(int id, EditarPlantillaVelsatDTO dto);
    Task<bool> EliminarAsync(int id);
}

public class PlantillaVelsatService : IPlantillaVelsatService
{
    private readonly IUnitOfWork _unitOfWork;

    public PlantillaVelsatService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ObtenerPlantillaVelsatDTO>> GetAllAsync(string? periodo = null)
    {
        if (string.Equals(periodo, "all", StringComparison.OrdinalIgnoreCase))
        {
            periodo = null;
        }

        var registros = await _unitOfWork.PlantillaVelsat.GetAllAsync(periodo);
        return registros.Select(MapToDTO);
    }

    public async Task<ObtenerPlantillaVelsatDTO> CrearAsync(CrearPlantillaVelsatDTO dto)
    {
        _unitOfWork.BeginTransaction();
        try
        {
            var entidad = new DomainPlantillaVelsat
            {
                Numdoc = dto.Numdoc,
                RazonSocial = dto.RazonSocial,
                Periodo = dto.Periodo,
                Concepto = dto.Concepto,
                Moneda = dto.Moneda,
                Importe = dto.Importe,
                Fechaini = dto.Fechaini,
                Fechafin = dto.Fechafin,
                Placa = dto.Placa
                , Correo = dto.Correo
                , Whatsapp = dto.Whatsapp
            };

            var creado = await _unitOfWork.PlantillaVelsat.CrearAsync(entidad);
            _unitOfWork.Commit();
            return MapToDTO(creado);
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarAsync(int id, EditarPlantillaVelsatDTO dto)
    {
        if (id <= 0)
            throw new ArgumentException("Id inválido");

        _unitOfWork.BeginTransaction();
        try
        {
            var entidad = new DomainPlantillaVelsat { Id = id };
            var resultado = await _unitOfWork.PlantillaVelsat.EditarAsync(entidad, dto);
            _unitOfWork.Commit();
            return resultado;
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
            throw new ArgumentException("Id inválido");

        _unitOfWork.BeginTransaction();
        try
        {
            var resultado = await _unitOfWork.PlantillaVelsat.EliminarAsync(id);
            _unitOfWork.Commit();
            return resultado;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    private ObtenerPlantillaVelsatDTO MapToDTO(DomainPlantillaVelsat p) => new()
    {
        Id = p.Id,
        Numdoc = p.Numdoc,
        RazonSocial = p.RazonSocial,
        Periodo = p.Periodo,
        Concepto = p.Concepto,
        Moneda = p.Moneda,
        Importe = p.Importe,
        Fechaini = p.Fechaini,
        Fechafin = p.Fechafin,
        Placa = p.Placa,
        Estado = p.Estado
        , Correo = p.Correo
        , Whatsapp = p.Whatsapp
    };
}