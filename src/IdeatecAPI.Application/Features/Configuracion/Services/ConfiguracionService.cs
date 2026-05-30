using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Configuracion.DTOs;

namespace IdeatecAPI.Application.Features.Configuracion.Services;

public interface IConfiguracionService
{
    Task<ConfiguracionDto?> GetByRucAsync(int ruc);
    Task<bool> RegistrarConfiguracionAsync(RegistrarConfiguracionDto dto);
    Task<bool> EditarConfiguracionAsync(int ruc, EditarConfiguracionDto dto);
}

public class ConfiguracionService : IConfiguracionService
{
    private readonly IUnitOfWork _unitOfWork;

    public ConfiguracionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ConfiguracionDto?> GetByRucAsync(int ruc)
    {
        var configuracion = await _unitOfWork.Configuracion.GetByRucAsync(ruc);

        if (configuracion == null)
            return null;

        return new ConfiguracionDto
        {
            Ruc               = configuracion.Ruc,
            IsImprime         = configuracion.IsImprime,
            TamañoImpresion   = configuracion.TamañoImpresion,
            Igv               = configuracion.Igv,
            IsConsumo         = configuracion.IsConsumo,
            GuiaRemision      = configuracion.GuiaRemision,
            IsCredito         = configuracion.IsCredito,
            ItemsDefecto      = configuracion.ItemsDefecto,
            IsBoletaOrFactura = configuracion.IsBoletaOrFactura,
            IsEnvioResumen    = configuracion.IsEnvioResumen,
            IsVale            = configuracion.IsVale,
            DeudasCobrar      = configuracion.DeudasCobrar,
            Trabajadores      = configuracion.Trabajadores,
            CargaComprobantes = configuracion.CargaComprobantes
        };
    }

    public async Task<bool> RegistrarConfiguracionAsync(RegistrarConfiguracionDto dto)
    {
        _unitOfWork.BeginTransaction();

        try
        {
            var configuracion = new Domain.Entities.Configuracion
            {
                Ruc               = dto.Ruc,
                IsImprime         = dto.IsImprime,
                TamañoImpresion   = dto.TamañoImpresion,
                Igv               = dto.Igv,
                IsConsumo         = dto.IsConsumo,
                GuiaRemision      = dto.GuiaRemision,
                IsCredito         = dto.IsCredito,
                ItemsDefecto      = dto.ItemsDefecto,
                IsBoletaOrFactura = dto.IsBoletaOrFactura,
                IsEnvioResumen    = dto.IsEnvioResumen,
                IsVale            = dto.IsVale,
                DeudasCobrar      = dto.DeudasCobrar,
                Trabajadores      = dto.Trabajadores,
                CargaComprobantes = dto.CargaComprobantes
            };

            var result = await _unitOfWork.Configuracion.RegistrarConfiguracionAsync(configuracion);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarConfiguracionAsync(int ruc, EditarConfiguracionDto dto)
    {
        if (ruc <= 0)
            throw new ArgumentException("Ruc inválido");

        _unitOfWork.BeginTransaction();

        try
        {
            var configuracion = new Domain.Entities.Configuracion
            {
                IsImprime         = dto.IsImprime,
                TamañoImpresion   = dto.TamañoImpresion,
                Igv               = dto.Igv,
                IsConsumo         = dto.IsConsumo,
                GuiaRemision      = dto.GuiaRemision,
                IsCredito         = dto.IsCredito,
                ItemsDefecto      = dto.ItemsDefecto,
                IsBoletaOrFactura = dto.IsBoletaOrFactura,
                IsEnvioResumen    = dto.IsEnvioResumen,
                IsVale            = dto.IsVale,
                DeudasCobrar      = dto.DeudasCobrar,
                Trabajadores      = dto.Trabajadores,
                CargaComprobantes = dto.CargaComprobantes
            };

            var result = await _unitOfWork.Configuracion.EditarConfiguracionAsync(ruc, configuracion);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }
}
