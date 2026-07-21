using IdeatecAPI.Application.Features.Inventario.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IInventarioLoteRepository : IRepository<InventarioLote>
{
    Task<InventarioLote> CrearLoteAsync(InventarioLote lote);
    Task<IEnumerable<InventarioLote>> GetLotesConSaldoFifoAsync(int sucursalProductoId);
    Task<bool> DescontarSaldoLoteAsync(int inventarioLoteId, decimal cantidad);
    Task<IEnumerable<InventarioLote>> GetLotesReporteAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta);
    Task<decimal> GetSaldoValorizadoAsync(int sucursalProductoId);
    Task<decimal> GetSaldoCantidadLotesAsync(int sucursalProductoId);
    Task<IEnumerable<InventarioLote>> GetSaldoValorizadoSucursalAsync(int sucursalId);
    Task<KardexMovimiento> RegistrarMovimientoAsync(KardexMovimiento movimiento, IEnumerable<KardexMovimientoLote> detalleLotes);
    Task<IEnumerable<KardexMovimiento>> GetKardexAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta);
    Task<bool> ExisteLoteSaldoInicialAsync(int sucursalProductoId);
    Task<KardexMovimiento?> GetUltimoMovimientoSalidaPorReferenciaAsync(string referenciaTipo, int referenciaId, int sucursalProductoId);
    Task<IEnumerable<RentabilidadProductoDTO>> GetRentabilidadPorProductoAsync(int sucursalId, DateTime? desde, DateTime? hasta);
    Task<IEnumerable<RentabilidadDiariaDTO>> GetRentabilidadDiariaAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta);
    Task<IEnumerable<InventarioLote>> GetByCompraProveedorIdAsync(int compraProveedorId);
    Task EliminarEntradaLoteAsync(int inventarioLoteId);
}
