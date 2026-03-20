using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Sucursal.DTOs;

namespace IdeatecAPI.Application.Features.Sucursal.Services;

public interface ISucursalService
{
    Task<IEnumerable<ObtenerSucursalDTO>> GetAllSucursalAsync();
    Task<ObtenerSucursalDTO> GetByIdSucursalAsync(int sucursalId);
    Task<IEnumerable<ObtenerSucursalDTO>> GetByRucSucursalAsync(string empresaRuc);
    Task<ObtenerSucursalDTO> RegistrarSucursalAsync(AgregarSucursalDTO agregarSucursalDTO);
    Task<bool> EditarSucursalAsync(EditarSucursalDTO editarSucursalDTO);
    Task<bool> EliminarSucursalAsync(int SucursalId);
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

    public async Task<IEnumerable<ObtenerSucursalDTO>> GetByRucSucursalAsync(string empresaRuc)
    {
        var sucursales = await _unitOfWork.Sucursal.GetByRucSucursalAsync(empresaRuc);
        return sucursales.Select(MapToDTO);
    }

    public async Task<ObtenerSucursalDTO> RegistrarSucursalAsync(AgregarSucursalDTO dto)
    {
        var sucursal = new Domain.Entities.Sucursal
        {
            EmpresaRuc                   = dto.EmpresaRuc,
            CodEstablecimiento           = dto.CodEstablecimiento,
            SerieFactura                 = dto.SerieFactura,
            CorrelativoFactura           = dto.CorrelativoFactura,
            SerieBoleta                  = dto.SerieBoleta,
            CorrelativoBoleta            = dto.CorrelativoBoleta,
            SerieNotaCredito             = dto.SerieNotaCredito,
            CorrelativoNotaCredito       = dto.CorrelativoNotaCredito,
            SerieNotaDebito              = dto.SerieNotaDebito,
            CorrelativoNotaDebito        = dto.CorrelativoNotaDebito,
            SerieGuiaRemision            = dto.SerieGuiaRemision,
            CorrelativoGuiaRemision      = dto.CorrelativoGuiaRemision,
            SerieGuiaTransportista       = dto.SerieGuiaTransportista,
            CorrelativoGuiaTransportista = dto.CorrelativoGuiaTransportista
        };

        var sucursalCreada = await _unitOfWork.Sucursal.RegistrarSucursalAsync(sucursal);
        return MapToDTO(sucursalCreada);
    }

    public async Task<bool> EditarSucursalAsync(EditarSucursalDTO dto)
    {
        var sucursal = await _unitOfWork.Sucursal.GetByIdSucursalAsync(dto.SucursalId)
                       ?? throw new KeyNotFoundException($"Sucursal con ID {dto.SucursalId} no encontrada.");

        sucursal.SerieFactura                 = dto.SerieFactura;
        sucursal.CorrelativoFactura           = dto.CorrelativoFactura;
        sucursal.SerieBoleta                  = dto.SerieBoleta;
        sucursal.CorrelativoBoleta            = dto.CorrelativoBoleta;
        sucursal.SerieNotaCredito             = dto.SerieNotaCredito;
        sucursal.CorrelativoNotaCredito       = dto.CorrelativoNotaCredito;
        sucursal.SerieNotaDebito              = dto.SerieNotaDebito;
        sucursal.CorrelativoNotaDebito        = dto.CorrelativoNotaDebito;
        sucursal.SerieGuiaRemision            = dto.SerieGuiaRemision;
        sucursal.CorrelativoGuiaRemision      = dto.CorrelativoGuiaRemision;
        sucursal.SerieGuiaTransportista       = dto.SerieGuiaTransportista;
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
        SucursalId                   = s.SucursalId,
        EmpresaRuc                   = s.EmpresaRuc,
        CodEstablecimiento           = s.CodEstablecimiento,
        SerieFactura                 = s.SerieFactura,
        CorrelativoFactura           = s.CorrelativoFactura,
        SerieBoleta                  = s.SerieBoleta,
        CorrelativoBoleta            = s.CorrelativoBoleta,
        SerieNotaCredito             = s.SerieNotaCredito,
        CorrelativoNotaCredito       = s.CorrelativoNotaCredito,
        SerieNotaDebito              = s.SerieNotaDebito,
        CorrelativoNotaDebito        = s.CorrelativoNotaDebito,
        SerieGuiaRemision            = s.SerieGuiaRemision,
        CorrelativoGuiaRemision      = s.CorrelativoGuiaRemision,
        SerieGuiaTransportista       = s.SerieGuiaTransportista,
        CorrelativoGuiaTransportista = s.CorrelativoGuiaTransportista,
        Estado                       = s.Estado
    };
}