using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Inventario.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Inventario.Services;

public interface IInventarioPepsService
{
    Task<InventarioLote> RegistrarEntradaLoteAsync(int sucursalProductoId, int? compraProveedorId, string origen,
        decimal cantidad, decimal costoUnitario, DateTime fecha, int? idUsuario,
        string? referenciaTipo = null, int? referenciaId = null, DateTime? fechaVencimiento = null);

    Task<ConsumoPepsResultDTO> ConsumirFifoAsync(int sucursalProductoId, decimal cantidad, string tipoMovimiento,
        string? referenciaTipo, int? referenciaId, int? idUsuario);

    Task DevolverAFifoAsync(int sucursalProductoId, decimal cantidad, decimal? costoUnitarioRespaldo,
        string? referenciaTipo, int? referenciaId, int? idUsuario);

    Task<IEnumerable<KardexMovimientoDTO>> GetKardexAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta);
    Task<StockValorizadoDTO> GetStockValorizadoAsync(int sucursalProductoId, string? nomProducto = null, string? codigo = null);
    Task<IEnumerable<StockValorizadoDTO>> GetStockValorizadoSucursalAsync(int sucursalId);
    Task<bool> ExisteLoteSaldoInicialAsync(int sucursalProductoId);
    Task<int> RegistrarSaldoInicialAsync(IEnumerable<RegistrarSaldoInicialDTO> items);
    Task<IEnumerable<RentabilidadProductoDTO>> GetRentabilidadPorProductoAsync(int sucursalId, DateTime? desde, DateTime? hasta);
    Task<IEnumerable<RentabilidadDiariaDTO>> GetRentabilidadDiariaAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta);
    Task<RetirarVencidosResultDTO> RetirarLotesVencidosAsync(int? sucursalProductoId = null, int? idUsuario = null);
}

public class InventarioPepsService : IInventarioPepsService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventarioPepsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<InventarioLote> RegistrarEntradaLoteAsync(int sucursalProductoId, int? compraProveedorId, string origen,
        decimal cantidad, decimal costoUnitario, DateTime fecha, int? idUsuario,
        string? referenciaTipo = null, int? referenciaId = null, DateTime? fechaVencimiento = null)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad del lote debe ser mayor a 0.");
        if (costoUnitario < 0)
            throw new ArgumentException("El costo unitario del lote no puede ser negativo.");

        var lote = new InventarioLote
        {
            SucursalProductoId = sucursalProductoId,
            CompraProveedorId = compraProveedorId,
            Origen = origen,
            FechaLote = fecha,
            CantidadOriginal = cantidad,
            CostoUnitario = costoUnitario,
            SaldoCantidad = cantidad,
            Estado = true,
            FechaVencimiento = fechaVencimiento
        };

        var loteCreado = await _unitOfWork.InventarioLotes.CrearLoteAsync(lote);

        var saldoCantidad = await _unitOfWork.InventarioLotes.GetSaldoCantidadLotesAsync(sucursalProductoId);
        var saldoValor = await _unitOfWork.InventarioLotes.GetSaldoValorizadoAsync(sucursalProductoId);

        var tipoMovimiento = origen switch
        {
            "SALDO_INICIAL" => "ENTRADA_SALDO_INICIAL",
            "DEVOLUCION_VENTA" => "ENTRADA_DEVOLUCION",
            _ => "ENTRADA_COMPRA",
        };

        var movimiento = new KardexMovimiento
        {
            SucursalProductoId = sucursalProductoId,
            TipoMovimiento = tipoMovimiento,
            ReferenciaTipo = referenciaTipo,
            ReferenciaId = referenciaId,
            Cantidad = cantidad,
            CostoUnitarioPromedio = costoUnitario,
            CostoTotal = cantidad * costoUnitario,
            SaldoCantidadPost = saldoCantidad,
            SaldoValorPost = saldoValor,
            FechaMovimiento = fecha,
            IdUsuario = idUsuario
        };

        var detalle = new List<KardexMovimientoLote>
        {
            new() { InventarioLoteId = loteCreado.InventarioLoteId, Cantidad = cantidad, CostoUnitario = costoUnitario }
        };

        await _unitOfWork.InventarioLotes.RegistrarMovimientoAsync(movimiento, detalle);

        return loteCreado;
    }

    public async Task<ConsumoPepsResultDTO> ConsumirFifoAsync(int sucursalProductoId, decimal cantidad, string tipoMovimiento,
        string? referenciaTipo, int? referenciaId, int? idUsuario)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a consumir debe ser mayor a 0.");

        var lotes = await _unitOfWork.InventarioLotes.GetLotesConSaldoFifoAsync(sucursalProductoId);

        var restante = cantidad;
        var costoTotal = 0m;
        var detalle = new List<KardexMovimientoLote>();

        foreach (var lote in lotes)
        {
            if (restante <= 0) break;

            var tomar = Math.Min(lote.SaldoCantidad, restante);
            var descontado = await _unitOfWork.InventarioLotes.DescontarSaldoLoteAsync(lote.InventarioLoteId, tomar);
            if (!descontado)
                throw new InvalidOperationException($"No se pudo descontar el lote {lote.InventarioLoteId}, saldo modificado concurrentemente.");

            costoTotal += tomar * lote.CostoUnitario;
            detalle.Add(new KardexMovimientoLote { InventarioLoteId = lote.InventarioLoteId, Cantidad = tomar, CostoUnitario = lote.CostoUnitario });
            restante -= tomar;
        }

        if (restante > 0)
            throw new InvalidOperationException(
                $"Stock insuficiente en lotes PEPS para SucursalProductoId {sucursalProductoId}: faltan {restante} unidades por cubrir.");

        var costoUnitarioPromedio = costoTotal / cantidad;

        var saldoCantidad = await _unitOfWork.InventarioLotes.GetSaldoCantidadLotesAsync(sucursalProductoId);
        var saldoValor = await _unitOfWork.InventarioLotes.GetSaldoValorizadoAsync(sucursalProductoId);

        var movimiento = new KardexMovimiento
        {
            SucursalProductoId = sucursalProductoId,
            TipoMovimiento = tipoMovimiento,
            ReferenciaTipo = referenciaTipo,
            ReferenciaId = referenciaId,
            Cantidad = cantidad,
            CostoUnitarioPromedio = costoUnitarioPromedio,
            CostoTotal = costoTotal,
            SaldoCantidadPost = saldoCantidad,
            SaldoValorPost = saldoValor,
            FechaMovimiento = DateTime.Now,
            IdUsuario = idUsuario
        };

        var movimientoCreado = await _unitOfWork.InventarioLotes.RegistrarMovimientoAsync(movimiento, detalle);

        return new ConsumoPepsResultDTO
        {
            SucursalProductoId = sucursalProductoId,
            CantidadConsumida = cantidad,
            CostoUnitarioPromedio = costoUnitarioPromedio,
            CostoTotal = costoTotal,
            KardexMovimientoId = movimientoCreado.KardexMovimientoId
        };
    }

    public async Task DevolverAFifoAsync(int sucursalProductoId, decimal cantidad, decimal? costoUnitarioRespaldo,
        string? referenciaTipo, int? referenciaId, int? idUsuario)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a devolver debe ser mayor a 0.");

        // Se busca a qué costo salió originalmente la venta/nota referenciada, para reingresar
        // el lote al mismo costo (trazabilidad PEPS). Si no hay referencia o no se encuentra,
        // se usa el costo de respaldo (p.ej. último costo de compra) para no bloquear la devolución.
        decimal costoUnitario = costoUnitarioRespaldo ?? 0;

        if (!string.IsNullOrWhiteSpace(referenciaTipo) && referenciaId is int refId)
        {
            var movimientoOriginal = await _unitOfWork.InventarioLotes.GetUltimoMovimientoSalidaPorReferenciaAsync(
                referenciaTipo, refId, sucursalProductoId);

            if (movimientoOriginal?.CostoUnitarioPromedio is decimal costoOriginal)
                costoUnitario = costoOriginal;
        }

        await RegistrarEntradaLoteAsync(
            sucursalProductoId,
            compraProveedorId: null,
            origen: "DEVOLUCION_VENTA",
            cantidad: cantidad,
            costoUnitario: costoUnitario,
            fecha: DateTime.Now,
            idUsuario: idUsuario,
            referenciaTipo: referenciaTipo,
            referenciaId: referenciaId);
    }

    public async Task<IEnumerable<KardexMovimientoDTO>> GetKardexAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta)
    {
        var movimientos = await _unitOfWork.InventarioLotes.GetKardexAsync(sucursalProductoId, desde, hasta);
        return movimientos.Select(m => new KardexMovimientoDTO
        {
            KardexMovimientoId = m.KardexMovimientoId,
            SucursalProductoId = m.SucursalProductoId,
            TipoMovimiento = m.TipoMovimiento,
            ReferenciaTipo = m.ReferenciaTipo,
            ReferenciaId = m.ReferenciaId,
            Cantidad = m.Cantidad,
            CostoUnitarioPromedio = m.CostoUnitarioPromedio,
            CostoTotal = m.CostoTotal,
            SaldoCantidadPost = m.SaldoCantidadPost,
            SaldoValorPost = m.SaldoValorPost,
            FechaMovimiento = m.FechaMovimiento,
            LotesConsumidos = m.LotesConsumidos
        });
    }

    public async Task<StockValorizadoDTO> GetStockValorizadoAsync(int sucursalProductoId, string? nomProducto = null, string? codigo = null)
    {
        var lotes = (await _unitOfWork.InventarioLotes.GetLotesReporteAsync(sucursalProductoId, null, null))
            .Where(l => l.SaldoCantidad > 0)
            .ToList();

        return new StockValorizadoDTO
        {
            SucursalProductoId = sucursalProductoId,
            NomProducto = nomProducto,
            Codigo = codigo,
            StockActual = lotes.Sum(l => l.SaldoCantidad),
            ValorTotal = lotes.Sum(l => l.SaldoCantidad * l.CostoUnitario),
            Lotes = lotes.Select(l => new LoteReporteDTO
            {
                InventarioLoteId = l.InventarioLoteId,
                Origen = l.Origen,
                FechaLote = l.FechaLote,
                CantidadOriginal = l.CantidadOriginal,
                CostoUnitario = l.CostoUnitario,
                SaldoCantidad = l.SaldoCantidad
            })
        };
    }

    public async Task<IEnumerable<StockValorizadoDTO>> GetStockValorizadoSucursalAsync(int sucursalId)
    {
        var lotes = await _unitOfWork.InventarioLotes.GetSaldoValorizadoSucursalAsync(sucursalId);

        return lotes
            .GroupBy(l => l.SucursalProductoId)
            .Select(g => new StockValorizadoDTO
            {
                SucursalProductoId = g.Key,
                NomProducto = g.First().NomProducto,
                Codigo = g.First().Codigo,
                StockActual = g.Sum(l => l.SaldoCantidad),
                ValorTotal = g.Sum(l => l.SaldoCantidad * l.CostoUnitario),
                Lotes = g.Select(l => new LoteReporteDTO
                {
                    InventarioLoteId = l.InventarioLoteId,
                    Origen = l.Origen,
                    FechaLote = l.FechaLote,
                    CantidadOriginal = l.CantidadOriginal,
                    CostoUnitario = l.CostoUnitario,
                    SaldoCantidad = l.SaldoCantidad
                })
            });
    }

    public async Task<bool> ExisteLoteSaldoInicialAsync(int sucursalProductoId)
    {
        return await _unitOfWork.InventarioLotes.ExisteLoteSaldoInicialAsync(sucursalProductoId);
    }

    public async Task<IEnumerable<RentabilidadProductoDTO>> GetRentabilidadPorProductoAsync(int sucursalId, DateTime? desde, DateTime? hasta)
    {
        return await _unitOfWork.InventarioLotes.GetRentabilidadPorProductoAsync(sucursalId, desde, hasta);
    }

    public async Task<IEnumerable<RentabilidadDiariaDTO>> GetRentabilidadDiariaAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta)
    {
        return await _unitOfWork.InventarioLotes.GetRentabilidadDiariaAsync(sucursalProductoId, desde, hasta);
    }

    public async Task<int> RegistrarSaldoInicialAsync(IEnumerable<RegistrarSaldoInicialDTO> items)
    {
        var lista = items.ToList();
        if (lista.Count == 0)
            throw new ArgumentException("La lista de saldo inicial no puede estar vacía.");
        if (lista.Any(i => i.Cantidad <= 0))
            throw new ArgumentException("Todas las cantidades deben ser mayores a 0.");
        if (lista.Any(i => i.CostoUnitario < 0))
            throw new ArgumentException("El costo unitario no puede ser negativo.");

        var creados = 0;

        _unitOfWork.BeginTransaction();
        try
        {
            foreach (var item in lista)
            {
                // Evita duplicar el saldo inicial si ya se corrió el backfill para este producto/sucursal.
                var yaExiste = await _unitOfWork.InventarioLotes.ExisteLoteSaldoInicialAsync(item.SucursalProductoId);
                if (yaExiste)
                    continue;

                await RegistrarEntradaLoteAsync(
                    item.SucursalProductoId,
                    compraProveedorId: null,
                    origen: "SALDO_INICIAL",
                    cantidad: item.Cantidad,
                    costoUnitario: item.CostoUnitario,
                    fecha: item.Fecha ?? DateTime.Now,
                    idUsuario: item.IdUsuario);

                creados++;
            }

            _unitOfWork.Commit();
            return creados;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<RetirarVencidosResultDTO> RetirarLotesVencidosAsync(int? sucursalProductoId = null, int? idUsuario = null)
    {
        _unitOfWork.BeginTransaction();
        try
        {
            var lotesVencidos = await _unitOfWork.InventarioLotes.GetLotesVencidosAsync(sucursalProductoId);
            var lista = lotesVencidos.ToList();

            if (lista.Count == 0)
            {
                _unitOfWork.Commit();
                return new RetirarVencidosResultDTO();
            }

            var porProducto = lista.GroupBy(l => l.SucursalProductoId);
            var totalLotes = 0;
            var totalCantidad = 0m;
            var totalCosto = 0m;

            foreach (var grupo in porProducto)
            {
                var spId = grupo.Key;
                var cantidadProducto = grupo.Sum(l => l.SaldoCantidad);
                var costoProducto = grupo.Sum(l => l.SaldoCantidad * l.CostoUnitario);

                var detalle = new List<KardexMovimientoLote>();

                foreach (var lote in grupo)
                {
                    var ok = await _unitOfWork.InventarioLotes.DesactivarLoteAsync(lote.InventarioLoteId);
                    if (!ok)
                        throw new InvalidOperationException(
                            $"No se pudo desactivar el lote vencido {lote.InventarioLoteId}, ya fue procesado.");

                    detalle.Add(new KardexMovimientoLote
                    {
                        InventarioLoteId = lote.InventarioLoteId,
                        Cantidad = lote.SaldoCantidad,
                        CostoUnitario = lote.CostoUnitario
                    });

                    totalLotes++;
                    totalCantidad += lote.SaldoCantidad;
                    totalCosto += lote.SaldoCantidad * lote.CostoUnitario;
                }

                var saldoCantidadPost = await _unitOfWork.InventarioLotes.GetSaldoCantidadLotesAsync(spId);
                var saldoValorPost = await _unitOfWork.InventarioLotes.GetSaldoValorizadoAsync(spId);

                var movimiento = new KardexMovimiento
                {
                    SucursalProductoId = spId,
                    TipoMovimiento = "SALIDA_VENCIMIENTO",
                    ReferenciaTipo = null,
                    ReferenciaId = null,
                    Cantidad = cantidadProducto,
                    CostoUnitarioPromedio = costoProducto / cantidadProducto,
                    CostoTotal = costoProducto,
                    SaldoCantidadPost = saldoCantidadPost,
                    SaldoValorPost = saldoValorPost,
                    FechaMovimiento = DateTime.Now,
                    IdUsuario = idUsuario
                };

                await _unitOfWork.InventarioLotes.RegistrarMovimientoAsync(movimiento, detalle);

                var stockOk = await _unitOfWork.Productos.ActualizarStockAsync(spId, (int)cantidadProducto);
                if (!stockOk)
                    throw new InvalidOperationException(
                        $"Stock insuficiente para descontar productos vencidos del SucursalProductoId {spId}.");
            }

            _unitOfWork.Commit();

            return new RetirarVencidosResultDTO
            {
                TotalLotesRetirados = totalLotes,
                TotalProductosAfectados = porProducto.Count(),
                TotalCantidadRetirada = totalCantidad,
                TotalCostoRetirado = totalCosto
            };
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }
}
