using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Proveedor.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Proveedor.Services;

public interface IProveedorService
{
    Task<IEnumerable<ObtenerProveedorDTO>> GetAllByRucEmpresaAsync(string rucEmpresa);
    Task<ObtenerProveedorDTO?> GetByIdRucEmpresaAsync(string rucEmpresa, int proveedorId);
    Task<ObtenerProveedorDTO> RegistrarAsync(RegistrarProveedorDTO dto);
    Task<bool> EditarAsync(EditarProveedorDTO dto);
    Task<bool> EliminarAsync(int proveedorId);
    Task<IEnumerable<ObtenerProveedorDTO>> SearchAsync(string rucEmpresa, string palabra);
}

public class ProveedorService : IProveedorService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProveedorService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ObtenerProveedorDTO>> GetAllByRucEmpresaAsync(string rucEmpresa)
    {
        var proveedores = await _unitOfWork.Proveedores.GetAllByRucEmpresaAsync(rucEmpresa);
        return proveedores.Select(MapToDTO);
    }

    public async Task<ObtenerProveedorDTO?> GetByIdRucEmpresaAsync(string rucEmpresa, int proveedorId)
    {
        if (proveedorId <= 0)
            throw new ArgumentException("ProveedorId inválido");

        var proveedor = await _unitOfWork.Proveedores.GetByIdRucEmpresaAsync(rucEmpresa, proveedorId);
        return proveedor == null ? null : MapToDTO(proveedor);
    }

    public async Task<ObtenerProveedorDTO> RegistrarAsync(RegistrarProveedorDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NumDocumento))
            throw new ArgumentException("NumDocumento es obligatorio");
        if (string.IsNullOrWhiteSpace(dto.RucEmpresa))
            throw new ArgumentException("RucEmpresa es obligatorio");

        var existente = await _unitOfWork.Proveedores.GetByNumDocRucEmpresaAsync(dto.RucEmpresa, dto.NumDocumento);
        if (existente != null)
            throw new InvalidOperationException($"Ya existe un proveedor con el documento '{dto.NumDocumento}' en este RUC.");

        _unitOfWork.BeginTransaction();
        try
        {
            var proveedor = new Domain.Entities.Proveedor
            {
                NumDocumento = dto.NumDocumento,
                RazonSocial = dto.RazonSocial,
                NombreComercial = dto.NombreComercial,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                Email = dto.Email,
                PersonaContacto = dto.PersonaContacto,
                RucEmpresa = dto.RucEmpresa,
                IdUsuario = dto.IdUsuario,
                FechaCreacion = DateTime.Now,
                Estado = true
            };

            var creado = await _unitOfWork.Proveedores.RegistrarAsync(proveedor);

            _unitOfWork.Commit();
            return MapToDTO(creado);
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarAsync(EditarProveedorDTO dto)
    {
        if (dto.ProveedorId == null || dto.ProveedorId <= 0)
            throw new ArgumentException("ProveedorId es obligatorio");

        _unitOfWork.BeginTransaction();
        try
        {
            var proveedor = new Domain.Entities.Proveedor
            {
                ProveedorId = dto.ProveedorId.Value,
                NumDocumento = dto.NumDocumento,
                RazonSocial = dto.RazonSocial,
                NombreComercial = dto.NombreComercial,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                Email = dto.Email,
                PersonaContacto = dto.PersonaContacto
            };

            var result = await _unitOfWork.Proveedores.EditarAsync(proveedor);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EliminarAsync(int proveedorId)
    {
        if (proveedorId <= 0)
            throw new ArgumentException("ProveedorId inválido");

        _unitOfWork.BeginTransaction();
        try
        {
            await _unitOfWork.Proveedores.EliminarAsync(proveedorId);

            _unitOfWork.Commit();
            return true;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<ObtenerProveedorDTO>> SearchAsync(string rucEmpresa, string palabra)
    {
        var proveedores = await _unitOfWork.Proveedores.SearchByRucEmpresaAsync(rucEmpresa, palabra);
        return proveedores.Select(MapToDTO);
    }

    private ObtenerProveedorDTO MapToDTO(Domain.Entities.Proveedor p)
    {
        return new ObtenerProveedorDTO
        {
            ProveedorId = p.ProveedorId,
            NumDocumento = p.NumDocumento,
            RazonSocial = p.RazonSocial,
            NombreComercial = p.NombreComercial,
            Direccion = p.Direccion,
            Telefono = p.Telefono,
            Email = p.Email,
            PersonaContacto = p.PersonaContacto,
            RucEmpresa = p.RucEmpresa,
            Estado = p.Estado,
            FechaCreacion = p.FechaCreacion
        };
    }
}
