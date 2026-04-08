using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.CatalogoSunat.DTOs;
using IdeatecAPI.Application.Features.Clientes.DTOs;
using IdeatecAPI.Application.Features.Direccion.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Clientes.Services;

public interface IClienteService
{
    Task<IEnumerable<ObtenerClientesDTO>> GetAllClientesRucAsync(string empresaRuc); // Todos los clientes de un RUC
    Task<IEnumerable<ObtenerClientesDTO>> GetAllClientesSucursalAsync(int sucursalId); // Clientes registrados en una sucursal
    Task<ObtenerClientesDTO?> GetClienteByIdEmpresaAsync(string empresaRuc, int clienteId); // Cliente unico de una empresa
    Task<ObtenerClientesDTO> RegistrarClienteAsync(RegistrarClienteDTO cliente);
    Task<bool> EditarClienteAsync(EditarClienteDTO cliente);
    Task<bool> EliminarClienteAsync(int clienteId);
    Task<IEnumerable<ObtenerClientesDTO>> SearchClientesAsync(string empresaRuc, string palabra);
}

public class ClienteService : IClienteService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClienteService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ObtenerClientesDTO>> GetAllClientesRucAsync(string empresaRuc)
    {
        var clientes = await _unitOfWork.Clientes.GetAllClientesRucAsync(empresaRuc);
        return clientes.Select(MapToDTO);
    }

    public async Task<IEnumerable<ObtenerClientesDTO>> GetAllClientesSucursalAsync(int sucursalId)
    {
        var clientes = await _unitOfWork.Clientes.GetAllClientesSucursalAsync(sucursalId);
        return clientes.Select(MapToDTO);
    }

    public async Task<ObtenerClientesDTO?> GetClienteByIdEmpresaAsync(string empresaRuc, int clienteId)
    {
        if (clienteId <= 0)
            throw new ArgumentException("ClienteId inválido");

        var cliente = await _unitOfWork.Clientes.GetClienteByIdEmpresaAsync(empresaRuc, clienteId);

        if (cliente == null)
            return null;

        return MapToDTO(cliente);
    }

    public async Task<ObtenerClientesDTO> RegistrarClienteAsync(RegistrarClienteDTO dto)
    {
        var existente = await _unitOfWork.Clientes.GetByClienteRepetidoEmpresaAsync(dto.NumeroDocumento!, dto.SucursalID!.Value);
        if (existente != null)
            throw new InvalidOperationException($"Ya existe un cliente con el documento '{dto.NumeroDocumento}' en este RUC.");

        _unitOfWork.BeginTransaction();
        try
        {
            var cliente = new Cliente
            {
                SucursalID = dto.SucursalID!.Value,
                TipoDocumentoId = dto.TipoDocumentoId,
                NumeroDocumento = dto.NumeroDocumento,
                RazonSocialNombre = dto.RazonSocialNombre,
                NombreComercial = dto.NombreComercial,
                Telefono = dto.Telefono,
                Correo = dto.Correo,
                FechaCreacion = DateTime.Now,
                Estado = true
            };

            var clienteCreado = await _unitOfWork.Clientes.RegistrarClienteAsync(cliente);

            if (dto.Direccion != null && !string.IsNullOrEmpty(dto.Direccion.DireccionLineal))
            {
                var direccion = new Domain.Entities.Direccion
                {
                    ClienteId = clienteCreado.ClienteId,
                    Ubigeo = dto.Direccion.Ubigeo,
                    DireccionLineal = dto.Direccion.DireccionLineal,
                    Departamento = dto.Direccion.Departamento,
                    Provincia = dto.Direccion.Provincia,
                    Distrito = dto.Direccion.Distrito,
                    TipoDireccion = dto.Direccion.TipoDireccion
                };

                await _unitOfWork.Direcciones.CrearDireccionAsync(direccion);
            }

            _unitOfWork.Commit();
            var clienteCompleto = await _unitOfWork.Clientes.GetClienteByIdAsync(clienteCreado.ClienteId);
            return MapToDTO(clienteCompleto!);
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarClienteAsync(EditarClienteDTO dto)
    {
        if (dto.ClienteId == null || dto.ClienteId <= 0)
            throw new ArgumentException("ClienteId es obligatorio");

        _unitOfWork.BeginTransaction();
        try
        {
            var cliente = new Cliente
            {
                ClienteId = dto.ClienteId.Value,
                RazonSocialNombre = dto.RazonSocialNombre,
                NumeroDocumento = dto.NumeroDocumento,
                NombreComercial = dto.NombreComercial,
                Telefono = dto.Telefono,
                Correo = dto.Correo
            };

            var result = await _unitOfWork.Clientes.EditarClienteAsync(cliente);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EliminarClienteAsync(int clienteId)
    {
        if (clienteId <= 0)
            throw new ArgumentException("ClienteId inválido");

        _unitOfWork.BeginTransaction();
        try
        {
            await _unitOfWork.Clientes.EliminarClienteAsync(clienteId);

            _unitOfWork.Commit();
            return true;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // Métodos privados
    private ObtenerClientesDTO MapToDTO(Cliente c)
    {
        return new ObtenerClientesDTO
        {
            ClienteId = c.ClienteId,
            SucursalID = c.SucursalID,
            RazonSocialNombre = c.RazonSocialNombre,
            NumeroDocumento = c.NumeroDocumento,
            NombreComercial = c.NombreComercial,
            FechaCreacion = c.FechaCreacion,
            Telefono = c.Telefono,
            Correo = c.Correo,
            Estado = c.Estado,

            Direccion = c.Direcciones?
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

            TipoDocumento = c.TipoDocumentoCliente == null
                ? null
                : new TipoDocumentoDTO
                {
                    TipoDocumentoId = c.TipoDocumentoCliente.TipoDocumentoId,
                    TipoDocumentoNombre = c.TipoDocumentoCliente.TipoDocumentoNombre
                }
        };
    }

    public async Task<IEnumerable<ObtenerClientesDTO>> SearchClientesAsync(string empresaRuc, string palabra)
    {
        var clientes = await _unitOfWork.Clientes.SearchByRucAsync(empresaRuc, palabra);
        return clientes.Select(MapToDTO);
    }
}