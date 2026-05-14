using IdeatecAPI.Domain.Entities;
using IdeatecAPI.Application.Features.Trabajadores.DTOs;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface ITrabajadorRepository : IRepository<Trabajador>
{
    Task<IEnumerable<Trabajador>> GetAllBySucursalAsync(int sucursalId);
    Task<Trabajador?> GetByDniAsync(string dni);
    Task<Trabajador?> GetByDniEnSucursalAsync(string dni, int sucursalId); // validación duplicado
    Task<Trabajador> RegistrarAsync(Trabajador trabajador);
    Task<bool> EditarAsync(Trabajador trabajador);
    Task<bool> EliminarAsync(int id);
    Task<IEnumerable<Trabajador>> SearchBySucursalAsync(int sucursalId, string palabra);
    Task<IEnumerable<ReporteServicioRawDTO>> GetServiciosByTrabajadorAsync(int trabajadorId, DateTime? fechaDesde, DateTime? fechaHasta);
    Task<IEnumerable<RankingTrabajadorDTO>> GetRankingBySucursalAsync(int sucursalId, DateTime? fechaDesde, DateTime? fechaHasta);
    Task<IEnumerable<ServicioTopDTO>> GetServiciosTopBySucursalAsync(int sucursalId, DateTime? fechaDesde, DateTime? fechaHasta);
    Task<IEnumerable<ReporteServicioRawDTO>> GetServiciosByClienteAsync(
    int sucursalId,
    string palabra,
    DateTime? fechaDesde,
    DateTime? fechaHasta);
    Task<IEnumerable<ReporteServicioRawDTO>> GetDetalleByServicioAsync(
    int sucursalId,
    string descripcion,
    DateTime? fechaDesde,
    DateTime? fechaHasta);
}