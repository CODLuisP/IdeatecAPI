using IdeatecAPI.Application.Features.Inventario.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IInventarioLoteRepository : IRepository<InventarioLote>
{
    Task<InventarioLote> CrearLoteAsync(InventarioLote lote);
    Task<IEnumerable<InventarioLote>> GetLotesConSaldoFifoAsync(int sucursalProductoId);
    Task<bool> DescontarSaldoLoteAsync(int inventarioLoteId, decimal cantidad);
    // Descuenta el saldo de todos los lotes consumidos por una venta en un unico UPDATE.
    // Devuelve cuantos lotes cumplieron la guardia de saldo suficiente.
    Task<int> DescontarSaldoLotesBatchAsync(IReadOnlyDictionary<int, decimal> consumoPorLote);
    Task<IEnumerable<InventarioLote>> GetLotesReporteAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta);
    Task<decimal> GetSaldoValorizadoAsync(int sucursalProductoId);
    Task<decimal> GetSaldoCantidadLotesAsync(int sucursalProductoId);
    Task<IEnumerable<InventarioLote>> GetSaldoValorizadoSucursalAsync(int sucursalId);
    Task<IEnumerable<InventarioLote>> GetLotesConSaldoFifoAsync(IEnumerable<int> sucursalProductoIds);
    Task<IEnumerable<SaldoLotesDTO>> GetSaldosLotesAsync(IEnumerable<int> sucursalProductoIds);
    // Lotes FIFO y saldos previos en un unico comando: son dos consultas independientes,
    // asi que no hay razon para pagar dos viajes de red por ellas.
    Task<(IEnumerable<InventarioLote> Lotes, IEnumerable<SaldoLotesDTO> Saldos)> GetLotesYSaldosFifoAsync(
        IEnumerable<int> sucursalProductoIds);
    Task<KardexMovimiento> RegistrarMovimientoAsync(KardexMovimiento movimiento, IEnumerable<KardexMovimientoLote> detalleLotes);
    // Inserta todas las cabeceras de kardex y todo su detalle de lotes en dos sentencias,
    // en vez de dos por movimiento. Devuelve los movimientos con su ID ya asignado.
    Task<IReadOnlyList<KardexMovimiento>> RegistrarMovimientosBatchAsync(IReadOnlyList<KardexMovimientoConDetalle> movimientos);

    // Descuento de lotes + kardex (cabecera y detalle) en un unico comando. Devuelve cuantos
    // lotes cumplieron la guardia de saldo, y deja los movimientos con su ID asignado.
    Task<int> AplicarConsumoPepsAsync(
        IReadOnlyDictionary<int, decimal> consumoPorLote,
        IReadOnlyList<KardexMovimientoConDetalle> movimientos);
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
