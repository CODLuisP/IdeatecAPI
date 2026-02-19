using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Clientes.DTOs;

namespace IdeatecAPI.Application.Features.Clientes.Services;

public interface IClienteService {
    Task<IEnumerable<ObtenerClientesDTO>> GetAllClientesAsync();
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
        var Clientes = await _unitOfWork.clientes.GetAllClientesAsync();
        

        return Clientes.Select(c => new ObtenerClientesDTO
        {
            RazonSocialNombre = c.RazonSocialNombre,
            NumeroDocumento = c.NumeroDocumento,
            NombreComercial = c.NombreComercial,
            FechaCreacion = c.FechaCreacion,
            Telefono = c.Telefono,
            Correo = c.Correo,
            Estado = c.Estado,
            
            Direccion = c.Direcciones
            .Select(d => new ObtenerClientesDireccionDTO
            {
                Ubigeo = d.Ubigeo,
                Departamento = d.Departamento,
                Provincia = d.Provincia,
                Distrito = d.Distrito,
                TipoDireccion = d.TipoDireccion
            }).ToList() ?? new List<ObtenerClientesDireccionDTO>(),

            TipoDocumento = new ObtenerClientesTipoDocumentoDTO
            {
                TipoDocumentoNombre = c.TipoDocumentoCliente?.TipoDocumentoNombre
            }
            });
    }
}
