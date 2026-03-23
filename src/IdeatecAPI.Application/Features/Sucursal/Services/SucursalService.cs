using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Sucursal.DTOs;

namespace IdeatecAPI.Application.Features.Sucursal.Services;

public interface ISucursalService
{
    Task<IEnumerable<ObtenerSucursalDTO>> GetAllSucursalAsync();
    Task<ObtenerSucursalDTO> GetByIdSucursalAsync(int sucursalId);
    Task<IEnumerable<ObtenerSucursalDTO>> GetByRucSucursalAsync(string empresaRuc, string? sucursalID = null);
    Task<ObtenerSucursalDTO> RegistrarSucursalAsync(AgregarSucursalDTO agregarSucursalDTO);
    Task<bool> EditarSucursalAsync(EditarSucursalDTO editarSucursalDTO);
    Task<bool> EliminarSucursalAsync(int SucursalId);
    Task<bool> EditarInfoSucursalAsync(int sucursalId, string? nombre, string? direccion);
}

public class SucursalService : ISucursalService
{
    private readonly IUnitOfWork _unitOfWork;

    public SucursalService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ObtenerSucursalDTO>> GetAllSucursalAsync()
    {
        var sucursales = await _unitOfWork.Sucursal.GetAllSucursalAsync();
        return sucursales.Select(MapToDTO);
    }

    public async Task<ObtenerSucursalDTO> GetByIdSucursalAsync(int sucursalId)
    {
        var sucursal = await _unitOfWork.Sucursal.GetByIdSucursalAsync(sucursalId);
        return MapToDTO(sucursal);
    }

    public async Task<IEnumerable<ObtenerSucursalDTO>> GetByRucSucursalAsync(string empresaRuc, string? sucursalID = null)
    {
        var sucursales = await _unitOfWork.Sucursal.GetByRucSucursalAsync(empresaRuc, sucursalID);
        return sucursales.Select(MapToDTO);
    }

    public async Task<ObtenerSucursalDTO> RegistrarSucursalAsync(AgregarSucursalDTO dto)
    {
        // Validar antes de abrir transacción
        if (string.IsNullOrEmpty(dto.EmpresaRuc))
            throw new InvalidOperationException("El RUC de la empresa es requerido.");

        _unitOfWork.BeginTransaction();
        try
        {
            // 1. Crear sucursal
            var sucursal = new Domain.Entities.Sucursal
            {
                EmpresaRuc = dto.EmpresaRuc,
                CodEstablecimiento = dto.CodEstablecimiento,
                Nombre = dto.Nombre,
                Direccion = dto.Direccion,
                SerieFactura = dto.SerieFactura,
                CorrelativoFactura = dto.CorrelativoFactura,
                SerieBoleta = dto.SerieBoleta,
                CorrelativoBoleta = dto.CorrelativoBoleta,
                SerieNotaCredito = dto.SerieNotaCredito,
                CorrelativoNotaCredito = dto.CorrelativoNotaCredito,
                SerieNotaDebito = dto.SerieNotaDebito,
                CorrelativoNotaDebito = dto.CorrelativoNotaDebito,
                SerieGuiaRemision = dto.SerieGuiaRemision,
                CorrelativoGuiaRemision = dto.CorrelativoGuiaRemision,
                SerieGuiaTransportista = dto.SerieGuiaTransportista,
                CorrelativoGuiaTransportista = dto.CorrelativoGuiaTransportista,
                Estado = true
            };

            var sucursalCreada = await _unitOfWork.Sucursal.RegistrarSucursalAsync(sucursal);

            // 2. Crear usuario admin de la nueva sucursal
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("12345678");

            var adminSucursal = new Domain.Entities.Usuario
            {
                Username = dto.UsernameAdminSucursal,
                Email = dto.EmailAdmin,
                Password = passwordHash,
                Rol = "admin",
                Estado = true,
                Ruc = dto.EmpresaRuc,
                SucursalID = sucursalCreada.SucursalId.ToString(),
                NombreSucursal = dto.NombreSucursal,
                TokenVersion = 0,
                FechaCreacion = DateTime.UtcNow
            };

            await _unitOfWork.Usuarios.CreateAsync(adminSucursal);

            // 3. Verificar si ya existe superadmin para este RUC
            var existeSuperadmin = await _unitOfWork.Usuarios.ExisteSuperadminAsync(dto.EmpresaRuc);

            if (!existeSuperadmin)
            {
                var superadmin = new Domain.Entities.Usuario
                {
                    Username = $"super{dto.UsernameAdminActual}",
                    Email = dto.EmailAdmin,
                    Password = passwordHash,
                    Rol = "superadmin",
                    Estado = true,
                    Ruc = dto.EmpresaRuc,
                    SucursalID = sucursalCreada.SucursalId.ToString(),
                    NombreSucursal = dto.NombreSucursal,
                    TokenVersion = 0,
                    FechaCreacion = DateTime.UtcNow
                };

                await _unitOfWork.Usuarios.CreateAsync(superadmin);
            }

            _unitOfWork.Commit();
            return MapToDTO(sucursalCreada);
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarSucursalAsync(EditarSucursalDTO dto)
    {
        var sucursal = await _unitOfWork.Sucursal.GetByIdSucursalAsync(dto.SucursalId)
                       ?? throw new KeyNotFoundException($"Sucursal con ID {dto.SucursalId} no encontrada.");

        sucursal.Nombre = dto.Nombre;
        sucursal.Direccion = dto.Nombre;
        sucursal.SerieFactura = dto.SerieFactura;
        sucursal.CorrelativoFactura = dto.CorrelativoFactura;
        sucursal.SerieBoleta = dto.SerieBoleta;
        sucursal.CorrelativoBoleta = dto.CorrelativoBoleta;
        sucursal.SerieNotaCredito = dto.SerieNotaCredito;
        sucursal.CorrelativoNotaCredito = dto.CorrelativoNotaCredito;
        sucursal.SerieNotaDebito = dto.SerieNotaDebito;
        sucursal.CorrelativoNotaDebito = dto.CorrelativoNotaDebito;
        sucursal.SerieGuiaRemision = dto.SerieGuiaRemision;
        sucursal.CorrelativoGuiaRemision = dto.CorrelativoGuiaRemision;
        sucursal.SerieGuiaTransportista = dto.SerieGuiaTransportista;
        sucursal.CorrelativoGuiaTransportista = dto.CorrelativoGuiaTransportista;

        return await _unitOfWork.Sucursal.EditarSucursalAsync(sucursal);
    }

    public async Task<bool> EliminarSucursalAsync(int sucursalId)
    {
        var sucursal = await _unitOfWork.Sucursal.GetByIdSucursalAsync(sucursalId)
                       ?? throw new KeyNotFoundException($"Sucursal con ID {sucursalId} no encontrada.");

        return await _unitOfWork.Sucursal.EliminarSucursalAsync(sucursal.SucursalId);
    }

    private static ObtenerSucursalDTO MapToDTO(Domain.Entities.Sucursal s) => new ObtenerSucursalDTO
    {
        SucursalId = s.SucursalId,
        EmpresaRuc = s.EmpresaRuc,
        CodEstablecimiento = s.CodEstablecimiento,
        Nombre = s.Nombre,
        Direccion = s.Direccion,
        SerieFactura = s.SerieFactura,
        CorrelativoFactura = s.CorrelativoFactura,
        SerieBoleta = s.SerieBoleta,
        CorrelativoBoleta = s.CorrelativoBoleta,
        SerieNotaCredito = s.SerieNotaCredito,
        CorrelativoNotaCredito = s.CorrelativoNotaCredito,
        SerieNotaDebito = s.SerieNotaDebito,
        CorrelativoNotaDebito = s.CorrelativoNotaDebito,
        SerieGuiaRemision = s.SerieGuiaRemision,
        CorrelativoGuiaRemision = s.CorrelativoGuiaRemision,
        SerieGuiaTransportista = s.SerieGuiaTransportista,
        CorrelativoGuiaTransportista = s.CorrelativoGuiaTransportista,
        Estado = s.Estado
    };

    public async Task<bool> EditarInfoSucursalAsync(int sucursalId, string? nombre, string? direccion)
    {
        return await _unitOfWork.Sucursal.EditarInfoAsync(sucursalId, nombre, direccion);
    }
}