using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Inventario.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.UnitTests;

// Repositorio de lotes en memoria. Solo implementa de verdad lo que usa el consumo PEPS
// en lote; el resto queda sin soportar a proposito para que un uso inesperado salte.
internal sealed class FakeInventarioLoteRepository : IInventarioLoteRepository
{
    private readonly List<InventarioLote> _lotes;

    public FakeInventarioLoteRepository(IEnumerable<InventarioLote> lotes) => _lotes = [.. lotes];

    public int LecturasCombinadas { get; private set; }
    public int LecturasDeLotes { get; private set; }
    public int LecturasDeSaldos { get; private set; }
    public int EscriturasCombinadas { get; private set; }
    public int EscriturasDeKardex { get; private set; }
    public int EscriturasDeLotes { get; private set; }
    public List<(int InventarioLoteId, decimal Cantidad)> Descuentos { get; } = [];
    public List<KardexMovimientoConDetalle> MovimientosRegistrados { get; } = [];
    // Permite simular que otra transaccion toco los saldos justo antes del UPDATE.
    public Action<List<InventarioLote>>? AntesDeDescontarLotes { get; set; }

    public async Task<(IEnumerable<InventarioLote> Lotes, IEnumerable<SaldoLotesDTO> Saldos)> GetLotesYSaldosFifoAsync(
        IEnumerable<int> sucursalProductoIds)
    {
        LecturasCombinadas++;
        var ids = sucursalProductoIds.ToList();
        return (await GetLotesConSaldoFifoAsync(ids), await GetSaldosLotesAsync(ids));
    }

    public Task<IEnumerable<InventarioLote>> GetLotesConSaldoFifoAsync(IEnumerable<int> sucursalProductoIds)
    {
        LecturasDeLotes++;
        var ids = sucursalProductoIds.ToHashSet();

        // Se devuelven copias, igual que haria Dapper: el servicio ajusta los saldos en
        // memoria y eso no debe alterar el estado "de la base".
        var resultado = _lotes
            .Where(l => ids.Contains(l.SucursalProductoId) && l.Estado && l.SaldoCantidad > 0)
            .OrderBy(l => l.SucursalProductoId)
            .ThenBy(l => l.FechaVencimiento is null)
            .ThenBy(l => l.FechaVencimiento)
            .ThenBy(l => l.FechaLote)
            .ThenBy(l => l.InventarioLoteId)
            .Select(l => new InventarioLote
            {
                InventarioLoteId = l.InventarioLoteId,
                SucursalProductoId = l.SucursalProductoId,
                SaldoCantidad = l.SaldoCantidad,
                CantidadOriginal = l.CantidadOriginal,
                CostoUnitario = l.CostoUnitario,
                FechaLote = l.FechaLote,
                FechaVencimiento = l.FechaVencimiento,
                Estado = l.Estado
            })
            .ToList();

        return Task.FromResult<IEnumerable<InventarioLote>>(resultado);
    }

    public Task<IEnumerable<SaldoLotesDTO>> GetSaldosLotesAsync(IEnumerable<int> sucursalProductoIds)
    {
        LecturasDeSaldos++;
        var ids = sucursalProductoIds.ToHashSet();

        var resultado = _lotes
            .Where(l => ids.Contains(l.SucursalProductoId) && l.Estado)
            .GroupBy(l => l.SucursalProductoId)
            .Select(g => new SaldoLotesDTO
            {
                SucursalProductoId = g.Key,
                SaldoCantidad = g.Sum(l => l.SaldoCantidad),
                SaldoValor = g.Sum(l => l.SaldoCantidad * l.CostoUnitario)
            })
            .ToList();

        return Task.FromResult<IEnumerable<SaldoLotesDTO>>(resultado);
    }

    public async Task<int> AplicarConsumoPepsAsync(
        IReadOnlyDictionary<int, decimal> consumoPorLote,
        IReadOnlyList<KardexMovimientoConDetalle> movimientos)
    {
        EscriturasCombinadas++;

        // Igual que el comando real: descuento y kardex ocurren en la misma ida. Si algun
        // lote no cumplia la guardia, quien lo deshace es el rollback de la transaccion,
        // no este metodo.
        var descontados = await DescontarSaldoLotesBatchAsync(consumoPorLote);
        await RegistrarMovimientosBatchAsync(movimientos);
        return descontados;
    }

    public Task<int> DescontarSaldoLotesBatchAsync(IReadOnlyDictionary<int, decimal> consumoPorLote)
    {
        EscriturasDeLotes++;
        AntesDeDescontarLotes?.Invoke(_lotes);
        var aplicados = 0;

        // Solo cuentan las filas que cumplen la guardia de saldo suficiente, igual que el
        // UPDATE real; las que no la cumplen quedan intactas.
        foreach (var (inventarioLoteId, cantidad) in consumoPorLote)
        {
            var lote = _lotes.FirstOrDefault(l => l.InventarioLoteId == inventarioLoteId);
            if (lote is null || lote.SaldoCantidad < cantidad)
                continue;

            lote.SaldoCantidad -= cantidad;
            Descuentos.Add((inventarioLoteId, cantidad));
            aplicados++;
        }

        return Task.FromResult(aplicados);
    }

    public Task<IReadOnlyList<KardexMovimiento>> RegistrarMovimientosBatchAsync(
        IReadOnlyList<KardexMovimientoConDetalle> movimientos)
    {
        EscriturasDeKardex++;
        MovimientosRegistrados.AddRange(movimientos);

        for (var i = 0; i < movimientos.Count; i++)
            movimientos[i].Movimiento.KardexMovimientoId = 1000 + i;

        return Task.FromResult<IReadOnlyList<KardexMovimiento>>([.. movimientos.Select(m => m.Movimiento)]);
    }

    private static T NoUsado<T>() => throw new NotSupportedException("Este fake solo cubre el consumo PEPS en lote.");

    public Task<InventarioLote> CrearLoteAsync(InventarioLote lote) => NoUsado<Task<InventarioLote>>();
    public Task<bool> DescontarSaldoLoteAsync(int inventarioLoteId, decimal cantidad) => NoUsado<Task<bool>>();
    public Task<IEnumerable<InventarioLote>> GetLotesConSaldoFifoAsync(int sucursalProductoId) => NoUsado<Task<IEnumerable<InventarioLote>>>();
    public Task<IEnumerable<InventarioLote>> GetLotesReporteAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta) => NoUsado<Task<IEnumerable<InventarioLote>>>();
    public Task<decimal> GetSaldoValorizadoAsync(int sucursalProductoId) => NoUsado<Task<decimal>>();
    public Task<decimal> GetSaldoCantidadLotesAsync(int sucursalProductoId) => NoUsado<Task<decimal>>();
    public Task<IEnumerable<InventarioLote>> GetSaldoValorizadoSucursalAsync(int sucursalId) => NoUsado<Task<IEnumerable<InventarioLote>>>();
    public Task<KardexMovimiento> RegistrarMovimientoAsync(KardexMovimiento movimiento, IEnumerable<KardexMovimientoLote> detalleLotes) => NoUsado<Task<KardexMovimiento>>();
    public Task<IEnumerable<KardexMovimientoResuelto>> GetKardexAsync(int sucursalProductoId, int? productoId, DateTime? desde, DateTime? hasta) => NoUsado<Task<IEnumerable<KardexMovimientoResuelto>>>();
    public Task<bool> ExisteLoteSaldoInicialAsync(int sucursalProductoId) => NoUsado<Task<bool>>();
    public Task<KardexMovimiento?> GetUltimoMovimientoSalidaPorReferenciaAsync(string referenciaTipo, int referenciaId, int sucursalProductoId) => NoUsado<Task<KardexMovimiento?>>();
    public Task<IEnumerable<KardexMovimiento>> GetMovimientosPorReferenciaAsync(string referenciaTipo, int referenciaId) => NoUsado<Task<IEnumerable<KardexMovimiento>>>();
    public Task<KardexMovimiento?> GetMovimientoPorComprobanteDetalleAsync(int comprobanteDetalleId) => NoUsado<Task<KardexMovimiento?>>();
    public Task<IEnumerable<RentabilidadProductoDTO>> GetRentabilidadPorProductoAsync(int sucursalId, DateTime? desde, DateTime? hasta) => NoUsado<Task<IEnumerable<RentabilidadProductoDTO>>>();
    public Task<IEnumerable<RentabilidadDiariaDTO>> GetRentabilidadDiariaAsync(int sucursalId, int productoId, DateTime? desde, DateTime? hasta) => NoUsado<Task<IEnumerable<RentabilidadDiariaDTO>>>();
    public Task<RentabilidadDiariaDTO> GetRentabilidadDiaSucursalAsync(int sucursalId, DateTime desde, DateTime hasta, int? usuarioId) => NoUsado<Task<RentabilidadDiariaDTO>>();
    public Task<IEnumerable<InventarioLote>> GetByCompraProveedorIdAsync(int compraProveedorId) => NoUsado<Task<IEnumerable<InventarioLote>>>();
    public Task<InventarioLote?> GetPorIdAsync(int inventarioLoteId) => NoUsado<Task<InventarioLote?>>();
    public Task EliminarEntradaLoteAsync(int inventarioLoteId) => NoUsado<Task>();
    public Task<IEnumerable<InventarioLote>> GetLotesVencidosAsync(int? sucursalProductoId = null) => NoUsado<Task<IEnumerable<InventarioLote>>>();
    public Task<bool> DesactivarLoteAsync(int inventarioLoteId) => NoUsado<Task<bool>>();
    public Task<IEnumerable<LoteVencidoDTO>> GetLotesVencidosReporteAsync(int? sucursalId = null) => NoUsado<Task<IEnumerable<LoteVencidoDTO>>>();
    public Task<bool> ActualizarFechaVencimientoAsync(int inventarioLoteId, DateTime? fechaVencimiento) => NoUsado<Task<bool>>();
    public Task<IEnumerable<HistorialVencidoDTO>> GetHistorialVencidosRetiradosAsync(int sucursalId, DateTime? desde, DateTime? hasta) => NoUsado<Task<IEnumerable<HistorialVencidoDTO>>>();

    public Task<InventarioLote?> GetByIdAsync(int id) => NoUsado<Task<InventarioLote?>>();
    public Task<IEnumerable<InventarioLote>> GetAllAsync() => NoUsado<Task<IEnumerable<InventarioLote>>>();
    public Task<IEnumerable<InventarioLote>> QueryAsync(string sql, object? param = null) => NoUsado<Task<IEnumerable<InventarioLote>>>();
    public Task<InventarioLote?> QueryFirstOrDefaultAsync(string sql, object? param = null) => NoUsado<Task<InventarioLote?>>();
    public Task<int> ExecuteAsync(string sql, object? param = null) => NoUsado<Task<int>>();
    public Task<int> InsertAsync(InventarioLote entity) => NoUsado<Task<int>>();
    public Task<int> UpdateAsync(InventarioLote entity) => NoUsado<Task<int>>();
    public Task<int> DeleteAsync(int id) => NoUsado<Task<int>>();
}

// Unidad de trabajo minima: solo expone el repositorio de lotes.
internal sealed class FakeUnitOfWork(IInventarioLoteRepository inventarioLotes) : IUnitOfWork
{
    public IInventarioLoteRepository InventarioLotes { get; } = inventarioLotes;

    public ICategoriaRepository Categorias => throw new NotSupportedException();
    public IUsuarioRepository Usuarios => throw new NotSupportedException();
    public IClienteRepository Clientes => throw new NotSupportedException();
    public IDireccionRepository Direcciones => throw new NotSupportedException();
    public IEmpresaRepository Empresas => throw new NotSupportedException();
    public INoteRepository Notes => throw new NotSupportedException();
    public INoteDetailRepository NoteDetails => throw new NotSupportedException();
    public INoteLegendRepository NoteLegends => throw new NotSupportedException();
    public IComunicacionBajaRepository Bajas => throw new NotSupportedException();
    public IComunicacionBajaDetalleRepository BajaDetalles => throw new NotSupportedException();
    public IComprobanteRepository Comprobantes => throw new NotSupportedException();
    public IProductoRepository Productos => throw new NotSupportedException();
    public ISucursalRepository Sucursal => throw new NotSupportedException();
    public IGuiaRemisionRepository Guias => throw new NotSupportedException();
    public IGuiaRemisionDetalleRepository GuiaDetalles => throw new NotSupportedException();
    public IResumenComprobanteRepository ResumenComprobante => throw new NotSupportedException();
    public IDashboardRepository Dashboard => throw new NotSupportedException();
    public IReportesRepository Reportes => throw new NotSupportedException();
    public ICuentasPorCobrarRepository CuentasPorCobrar => throw new NotSupportedException();
    public IDeudaContadoRepository DeudaContado => throw new NotSupportedException();
    public ITrabajadorRepository Trabajadores => throw new NotSupportedException();
    public IPlantillaVelsatRepository PlantillaVelsat => throw new NotSupportedException();
    public INotificacionEnviadaRepository NotificacionesEnviadas => throw new NotSupportedException();
    public INotificacionDiasRepository NotificacionDias => throw new NotSupportedException();
    public IValeRepository Vales => throw new NotSupportedException();
    public IConfiguracionRepository Configuracion => throw new NotSupportedException();
    public IProveedorRepository Proveedores => throw new NotSupportedException();
    public ICompraProveedorRepository ComprasProveedor => throw new NotSupportedException();
    public ISireRegistroRepository SireRegistros => throw new NotSupportedException();
    public IRetryRobotRepository RetryRobot => throw new NotSupportedException();
    public ICajaRepository Caja => throw new NotSupportedException();

    public void SetEnvironment(string env) => throw new NotSupportedException();
    public void BeginTransaction() => throw new NotSupportedException();
    public void Commit() => throw new NotSupportedException();
    public void Rollback() => throw new NotSupportedException();
    public IRepository<T> Repository<T>() where T : class => throw new NotSupportedException();
    public void Dispose() { }
}
