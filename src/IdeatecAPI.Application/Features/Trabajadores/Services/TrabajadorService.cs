using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Trabajadores.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Trabajadores.Services;

public interface ITrabajadorService
{
    Task<IEnumerable<ObtenerTrabajadorDTO>> GetAllBySucursalAsync(int sucursalId);
    Task<ObtenerTrabajadorDTO?> GetByIdAsync(int id);
    Task<ObtenerTrabajadorDTO> RegistrarAsync(RegistrarTrabajadorDTO dto);
    Task<bool> EditarAsync(EditarTrabajadorDTO dto);
    Task<bool> EliminarAsync(int id);
    Task<IEnumerable<ObtenerTrabajadorDTO>> SearchAsync(int sucursalId, string palabra);
}

public class TrabajadorService : ITrabajadorService
{
    private readonly IUnitOfWork _unitOfWork;

    public TrabajadorService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ObtenerTrabajadorDTO>> GetAllBySucursalAsync(int sucursalId)
    {
        var trabajadores = await _unitOfWork.Trabajadores.GetAllBySucursalAsync(sucursalId);
        return trabajadores.Select(MapToDTO);
    }

    public async Task<ObtenerTrabajadorDTO?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Id inválido.");

        var trabajador = await _unitOfWork.Trabajadores.GetByIdAsync(id);
        return trabajador == null ? null : MapToDTO(trabajador);
    }

    public async Task<ObtenerTrabajadorDTO> RegistrarAsync(RegistrarTrabajadorDTO dto)
    {
        // Validar DNI duplicado dentro de la misma sucursal
        var existente = await _unitOfWork.Trabajadores.GetByDniEnSucursalAsync(dto.Dni!, dto.SucursalId);
        if (existente != null)
            throw new InvalidOperationException($"Ya existe un trabajador con el DNI '{dto.Dni}' en esta sucursal.");

        _unitOfWork.BeginTransaction();
        try
        {
            var trabajador = new Trabajador
            {
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                Dni = dto.Dni,
                Celular = dto.Celular,
                Email = dto.Email,
                SucursalId = dto.SucursalId,
                Estado = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var creado = await _unitOfWork.Trabajadores.RegistrarAsync(trabajador);
            _unitOfWork.Commit();

            var completo = await _unitOfWork.Trabajadores.GetByIdAsync(creado.Id);
            return MapToDTO(completo!);
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarAsync(EditarTrabajadorDTO dto)
    {
        if (dto.Id == null || dto.Id <= 0)
            throw new ArgumentException("Id es obligatorio.");

        _unitOfWork.BeginTransaction();
        try
        {
            var trabajador = new Trabajador
            {
                Id = dto.Id.Value,
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                Dni = dto.Dni,
                Celular = dto.Celular,
                Email = dto.Email,
                SucursalId = dto.SucursalId ?? 0,
                UpdatedAt = DateTime.Now
            };

            var resultado = await _unitOfWork.Trabajadores.EditarAsync(trabajador);
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
            throw new ArgumentException("Id inválido.");

        _unitOfWork.BeginTransaction();
        try
        {
            var resultado = await _unitOfWork.Trabajadores.EliminarAsync(id);
            _unitOfWork.Commit();
            return resultado;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<ObtenerTrabajadorDTO>> SearchAsync(int sucursalId, string palabra)
    {
        var trabajadores = await _unitOfWork.Trabajadores.SearchBySucursalAsync(sucursalId, palabra);
        return trabajadores.Select(MapToDTO);
    }

    private static ObtenerTrabajadorDTO MapToDTO(Trabajador t) => new()
    {
        Id = t.Id,
        Nombres = t.Nombres,
        Apellidos = t.Apellidos,
        Dni = t.Dni,
        Celular = t.Celular,
        Email = t.Email,
        Estado = t.Estado,
        SucursalId = t.SucursalId,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}