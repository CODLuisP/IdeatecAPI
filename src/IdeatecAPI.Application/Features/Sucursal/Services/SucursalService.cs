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
    Task<bool> InhabilitarSucursalAsync(int SucursalId);
    Task<bool> EditarInfoSucursalAsync(int sucursalId, string? nombre, string? direccion);
    Task<IEnumerable<ObtenerSucursalDTO>> GetByRucTodasAsync(string empresaRuc);
    Task<bool> HabilitarSucursalAsync(int sucursalId);
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

    public async Task<IEnumerable<ObtenerSucursalDTO>> GetByRucTodasAsync(string empresaRuc)
    {
        var sucursales = await _unitOfWork.Sucursal.GetByRucTodasAsync(empresaRuc);
        return sucursales.Select(MapToDTO);
    }

    public async Task<bool> InhabilitarSucursalAsync(int sucursalId)
    {

        var sucursal = await _unitOfWork.Sucursal.GetByIdSinFiltroAsync(sucursalId)
            ?? throw new KeyNotFoundException($"Sucursal con ID {sucursalId} no encontrada.");

        _unitOfWork.BeginTransaction();
        try
        {
            await _unitOfWork.Sucursal.InhabilitarSucursalAsync(sucursalId);
            _unitOfWork.Commit();
            return true;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> HabilitarSucursalAsync(int sucursalId)
    {

        var sucursal = await _unitOfWork.Sucursal.GetByIdSinFiltroAsync(sucursalId)
            ?? throw new KeyNotFoundException($"Sucursal con ID {sucursalId} no encontrada.");

        _unitOfWork.BeginTransaction();
        try
        {
            await _unitOfWork.Sucursal.HabilitarSucursalAsync(sucursalId);
            _unitOfWork.Commit();
            return true;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<ObtenerSucursalDTO> RegistrarSucursalAsync(AgregarSucursalDTO dto)
    {
        if (string.IsNullOrEmpty(dto.EmpresaRuc))
            throw new InvalidOperationException("El RUC de la empresa es requerido.");

        _unitOfWork.BeginTransaction();
        try
        {
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
                SerieNotaCreditoFactura = dto.SerieNotaCreditoFactura,
                CorrelativoNotaCreditoFactura = dto.CorrelativoNotaCreditoFactura,
                SerieNotaCreditoBoleta = dto.SerieNotaCreditoBoleta,
                CorrelativoNotaCreditoBoleta = dto.CorrelativoNotaCreditoBoleta,
                SerieNotaDebitoFactura = dto.SerieNotaDebitoFactura,
                CorrelativoNotaDebitoFactura = dto.CorrelativoNotaDebitoFactura,
                SerieNotaDebitoBoleta = dto.SerieNotaDebitoBoleta,
                CorrelativoNotaDebitoBoleta = dto.CorrelativoNotaDebitoBoleta,
                SerieGuiaRemision = dto.SerieGuiaRemision,
                CorrelativoGuiaRemision = dto.CorrelativoGuiaRemision,
                SerieGuiaTransportista = dto.SerieGuiaTransportista,
                CorrelativoGuiaTransportista = dto.CorrelativoGuiaTransportista,
                Estado = true
            };

            var sucursalCreada = await _unitOfWork.Sucursal.RegistrarSucursalAsync(sucursal);

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
                    SucursalID = null,
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
        sucursal.Direccion = dto.Direccion; // ← también corregí este bug (antes asignaba dto.Nombre)
        sucursal.SerieFactura = dto.SerieFactura;
        sucursal.CorrelativoFactura = dto.CorrelativoFactura;
        sucursal.SerieBoleta = dto.SerieBoleta;
        sucursal.CorrelativoBoleta = dto.CorrelativoBoleta;
        sucursal.SerieNotaCreditoFactura = dto.SerieNotaCreditoFactura;
        sucursal.CorrelativoNotaCreditoFactura = dto.CorrelativoNotaCreditoFactura;
        sucursal.SerieNotaCreditoBoleta = dto.SerieNotaCreditoBoleta;
        sucursal.CorrelativoNotaCreditoBoleta = dto.CorrelativoNotaCreditoBoleta;
        sucursal.SerieNotaDebitoFactura = dto.SerieNotaDebitoFactura;
        sucursal.CorrelativoNotaDebitoFactura = dto.CorrelativoNotaDebitoFactura;
        sucursal.SerieNotaDebitoBoleta = dto.SerieNotaDebitoBoleta;
        sucursal.CorrelativoNotaDebitoBoleta = dto.CorrelativoNotaDebitoBoleta;
        sucursal.SerieGuiaRemision = dto.SerieGuiaRemision;
        sucursal.CorrelativoGuiaRemision = dto.CorrelativoGuiaRemision;
        sucursal.SerieGuiaTransportista = dto.SerieGuiaTransportista;
        sucursal.CorrelativoGuiaTransportista = dto.CorrelativoGuiaTransportista;

        return await _unitOfWork.Sucursal.EditarSucursalAsync(sucursal);
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
        SerieNotaCreditoFactura = s.SerieNotaCreditoFactura,
        CorrelativoNotaCreditoFactura = s.CorrelativoNotaCreditoFactura,
        SerieNotaCreditoBoleta = s.SerieNotaCreditoBoleta,
        CorrelativoNotaCreditoBoleta = s.CorrelativoNotaCreditoBoleta,
        SerieNotaDebitoFactura = s.SerieNotaDebitoFactura,
        CorrelativoNotaDebitoFactura = s.CorrelativoNotaDebitoFactura,
        SerieNotaDebitoBoleta = s.SerieNotaDebitoBoleta,
        CorrelativoNotaDebitoBoleta = s.CorrelativoNotaDebitoBoleta,
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