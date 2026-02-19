using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.CatalogoSunat.DTOs;
using IdeatecAPI.Application.Features.Clientes.DTOs;
using IdeatecAPI.Application.Features.Direccion.DTOs;
using IdeatecAPI.Domain.Entities.Cliente;
namespace IdeatecAPI.Application.Features.Clientes.Services;

public interface IClienteService {
    Task<IEnumerable<ObtenerClientesDTO>> GetAllClientesAsync();
    Task<int> RegistrarClienteAsync(RegistrarClienteDTO cliente);
}

public class ClienteService : IClienteService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClienteService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<IEnumerable<ObtenerClientesDTO>> GetAllClientesAsync()
    {
        var Clientes = await _unitOfWork.Clientes.GetAllClientesAsync();
        return Clientes.Select(c => new ObtenerClientesDTO
        {
            ClienteId = c.ClienteId,
            RazonSocialNombre = c.RazonSocialNombre,
            NumeroDocumento = c.NumeroDocumento,
            NombreComercial = c.NombreComercial,
            FechaCreacion = c.FechaCreacion,
            Telefono = c.Telefono,
            Correo = c.Correo,
            Estado = c.Estado,
            
            Direccion = c.Direcciones
            .Select(d => new DireccionDTO
            {
                DireccionId = d.DireccionId,
                DireccionLineal = d.DireccionLineal,
                Ubigeo = d.Ubigeo,
                Departamento = d.Departamento,
                Provincia = d.Provincia,
                Distrito = d.Distrito,
                TipoDireccion = d.TipoDireccion
            }).ToList() ?? new List<DireccionDTO>(),

            TipoDocumento = new TipoDocumentoDTO
            {
                TipoDocumentoId = c.TipoDocumentoCliente?.TipoDocumentoId,
                TipoDocumentoNombre = c.TipoDocumentoCliente?.TipoDocumentoNombre
            }
            });
    }

    public async Task<int> RegistrarClienteAsync(RegistrarClienteDTO dto)
{
    _unitOfWork.BeginTransaction();
    try
    {
        var cliente = new Cliente
        {
            TipoDocumentoId = dto.TipoDocumentoId,
            NumeroDocumento = dto.NumeroDocumento,
            RazonSocialNombre = dto.RazonSocialNombre,
            NombreComercial = dto.NombreComercial,
            Telefono = dto.Telefono,
            Correo = dto.Correo,
            FechaCreacion = DateTime.Now,
            Estado = true
        };

        int clienteId = await _unitOfWork.Clientes.RegistrarClienteAsync(cliente);

        var direccion = new Domain.Entities.Cliente.Direccion
        {
            ClienteId = clienteId,
            Ubigeo = dto.Direccion?.Ubigeo,
            DireccionLineal = dto.Direccion?.DireccionLineal,
            Departamento = dto.Direccion?.Departamento,
            Provincia = dto.Direccion?.Provincia,
            Distrito = dto.Direccion?.Distrito,
            TipoDireccion = dto.Direccion?.TipoDireccion
        };

        await _unitOfWork.Direcciones.CrearDireccionAsync(direccion);

        _unitOfWork.Commit();
        return clienteId;
    }
    catch
    {
        _unitOfWork.Rollback();
        throw;
    }
}
}

