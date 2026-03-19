using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.SerieCorrelativo.DTOs;

namespace IdeatecAPI.Application.Features.SerieCorrelativo.Services;
public interface ISerieCorrelativoService
{
    Task<IEnumerable<ObtenerSerieCorrelativoDTO>> GetSerieCorrelativoAsync(string EmpresaId, string TipoComprobante);
    Task<int> RegistrarSerieCorrelativoAsync(AgregarSerieCorrelativoDTO agregarSerieCorrelativoDTO);
}
public class SerieCorrelativoService : ISerieCorrelativoService
{
    private readonly IUnitOfWork _unitOfWork;

    public SerieCorrelativoService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ObtenerSerieCorrelativoDTO>> GetSerieCorrelativoAsync(string EmpresaRuc, string TipoComprobante)
    {
        var series = await _unitOfWork.SerieCorrelativo.GetSerieCorrelativoAsync(EmpresaRuc, TipoComprobante);
        
        return series.Select(s => new ObtenerSerieCorrelativoDTO
        {
            SerieId            = s.SerieId,
            EmpresaRuc          = s.EmpresaRuc,
            TipoComprobante    = s.TipoComprobante,
            Serie              = s.Serie,
            CorrelativoActual  = s.CorrelativoActual,
            Estado             = s.Estado,
            FechaActualizacion = s.FechaActualizacion
        });
    }

    public async Task<int> RegistrarSerieCorrelativoAsync(AgregarSerieCorrelativoDTO dto)
    {
        var serie = new Domain.Entities.SerieCorrelativo
        {
            EmpresaRuc         = dto.EmpresaRuc,
            TipoComprobante   = dto.TipoComprobante,
            Serie             = dto.Serie,
            CorrelativoActual = dto.correlativoActual,
            Estado            = dto.Estado
        };

        return await _unitOfWork.SerieCorrelativo.RegistrarSerieCorrelativoAsync(serie);
    }
}