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

    /// <summary>
    /// Devuelve saldo en cantidad y saldo valorizado en UNA sola consulta.
    /// Equivale a llamar a GetSaldoCantidadLotesAsync + GetSaldoValorizadoAsync
    /// (misma tabla, mismo filtro), pero ahorra un viaje a la base de datos.
    /// </summary>
    Task<(decimal Cantidad, decimal Valor)> GetSaldosLotesAsync(int sucursalProductoId);

    // ── Versiones en bloque (una sola ida y vuelta para todo el comprobante) ──
    Task<IEnumerable<InventarioLote>> GetLotesConSaldoFifoPorProductosAsync(IEnumerable<int> sucursalProductoIds);
    Task<int> DescontarSaldosLotesEnBloqueAsync(IReadOnlyList<ConsumoLote> consumos);
    Task<IReadOnlyDictionary<int, SaldosLote>> GetSaldosLotesPorProductosAsync(IEnumerable<int> sucursalProductoIds);
    Task RegistrarMovimientosEnBloqueAsync(
        IReadOnlyList<KardexMovimiento> movimientos,
        IReadOnlyList<IReadOnlyList<KardexMovimientoLote>> detallesPorMovimiento);
    Task<IEnumerable<InventarioLote>> GetSaldoValorizadoSucursalAsync(int sucursalId);
    Task<KardexMovimiento> RegistrarMovimientoAsync(KardexMovimiento movimiento, IEnumerable<KardexMovimientoLote> detalleLotes);
    Task<IEnumerable<KardexMovimiento>> GetKardexAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta);
    Task<bool> ExisteLoteSaldoInicialAsync(int sucursalProductoId);
    Task<KardexMovimiento?> GetUltimoMovimientoSalidaPorReferenciaAsync(string referenciaTipo, int referenciaId, int sucursalProductoId);
    Task<IEnumerable<KardexMovimiento>> GetMovimientosPorReferenciaAsync(string referenciaTipo, int referenciaId);
    Task<IEnumerable<RentabilidadProductoDTO>> GetRentabilidadPorProductoAsync(int sucursalId, DateTime? desde, DateTime? hasta);
    Task<IEnumerable<RentabilidadDiariaDTO>> GetRentabilidadDiariaAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta);
    Task<IEnumerable<InventarioLote>> GetByCompraProveedorIdAsync(int compraProveedorId);
    Task<InventarioLote?> GetPorIdAsync(int inventarioLoteId);
    Task EliminarEntradaLoteAsync(int inventarioLoteId);
    Task<IEnumerable<InventarioLote>> GetLotesVencidosAsync(int? sucursalProductoId = null);
    Task<bool> DesactivarLoteAsync(int inventarioLoteId);
    Task<IEnumerable<LoteVencidoDTO>> GetLotesVencidosReporteAsync(int? sucursalId = null);
    Task<bool> ActualizarFechaVencimientoAsync(int inventarioLoteId, DateTime? fechaVencimiento);
    Task<IEnumerable<HistorialVencidoDTO>> GetHistorialVencidosRetiradosAsync(int sucursalId, DateTime? desde, DateTime? hasta);
}
