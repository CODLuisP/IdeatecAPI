
using IdeatecAPI.Application.Features.Sucursal.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface ISucursalRepository : IRepository<Sucursal>
{
    Task<IEnumerable<Sucursal>> GetAllSucursalAsync();
    Task<Sucursal> GetByIdSucursalAsync(int SucursalId);
    Task<IEnumerable<Sucursal>> GetByRucSucursalAsync(string empresaRuc, string? sucursalID = null);
    Task<Sucursal> RegistrarSucursalAsync(Sucursal sucursal);
    Task<bool> EditarSucursalAsync(Sucursal sucursal);
    Task<bool> InhabilitarSucursalAsync(int SucursalId);
    Task<bool> EditarInfoAsync(int sucursalId, string? nombre, string? direccion);
    Task<IEnumerable<Sucursal>> GetByRucTodasAsync(string empresaRuc);
    Task<Sucursal?> GetByIdSinFiltroAsync(int sucursalId);
    Task<bool> HabilitarSucursalAsync(int sucursalId);
}