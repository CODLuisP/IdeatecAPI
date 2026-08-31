using IdeatecAPI.Application.Common.Exceptions;
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

    // Procesa varias salidas PEPS de golpe (todos los productos de una venta). Lee lotes y
    // saldos en dos consultas y escribe el kardex en dos INSERT, en vez de repetir ~8
    // consultas por producto.
    Task<IReadOnlyList<ConsumoPepsResultDTO>> ConsumirFifoBatchAsync(
        IReadOnlyList<ConsumoPepsRequestDTO> consumos, int? idUsuario);

    Task DevolverAFifoAsync(int sucursalProductoId, decimal cantidad, decimal? costoUnitarioRespaldo,
        string? referenciaTipo, int? referenciaId, int? idUsuario);

    Task<IEnumerable<KardexMovimientoDTO>> GetKardexAsync(int sucursalProductoId, int? productoId, DateTime? desde, DateTime? hasta);
    Task<StockValorizadoDTO> GetStockValorizadoAsync(int sucursalProductoId, string? nomProducto = null, string? codigo = null);
    Task<IEnumerable<StockValorizadoDTO>> GetStockValorizadoSucursalAsync(int sucursalId);
    Task<bool> ExisteLoteSaldoInicialAsync(int sucursalProductoId);
    Task<int> RegistrarSaldoInicialAsync(IEnumerable<RegistrarSaldoInicialDTO> items);
    Task<IEnumerable<RentabilidadProductoDTO>> GetRentabilidadPorProductoAsync(int sucursalId, DateTime? desde, DateTime? hasta);
    Task<IEnumerable<RentabilidadDiariaDTO>> GetRentabilidadDiariaAsync(int sucursalId, int productoId, DateTime? desde, DateTime? hasta);
    Task<RetirarVencidosResultDTO> RetirarLotesVencidosAsync(int? sucursalProductoId = null, int? idUsuario = null);
    Task<IEnumerable<LoteVencidoDTO>> GetLotesVencidosReporteAsync(int? sucursalId = null);
    Task<ActualizarFechaVencimientoResultDTO> ActualizarFechaVencimientoLoteAsync(int inventarioLoteId, DateTime? fechaVencimiento, bool confirmar = false);
    Task<IEnumerable<HistorialVencidoDTO>> GetHistorialVencidosRetiradosAsync(int sucursalId, DateTime? desde, DateTime? hasta);
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
            "AJUSTE_INVENTARIO" => "ENTRADA_AJUSTE",
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
        var resultados = await ConsumirFifoBatchAsync(
            [
                new ConsumoPepsRequestDTO
                {
                    SucursalProductoId = sucursalProductoId,
                    Cantidad = cantidad,
                    TipoMovimiento = tipoMovimiento,
                    ReferenciaTipo = referenciaTipo,
                    ReferenciaId = referenciaId
                }
            ],
            idUsuario);

        return resultados[0];
    }

    public async Task<IReadOnlyList<ConsumoPepsResultDTO>> ConsumirFifoBatchAsync(
        IReadOnlyList<ConsumoPepsRequestDTO> consumos, int? idUsuario)
    {
        if (consumos.Count == 0)
            return [];

        if (consumos.Any(c => c.Cantidad <= 0))
            throw new ArgumentException("La cantidad a consumir debe ser mayor a 0.");

        var ids = consumos.Select(c => c.SucursalProductoId).Distinct().ToList();

        // Un solo viaje trae las dos cosas que hacen falta para toda la venta: los lotes con
        // saldo (bloqueados con FOR UPDATE, igual que antes) y el saldo acumulado previo de
        // cada producto.
        var (lotesLeidos, saldosLeidos) = await _unitOfWork.InventarioLotes.GetLotesYSaldosFifoAsync(ids);

        var lotesPorProducto = lotesLeidos
            .GroupBy(l => l.SucursalProductoId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var saldos = saldosLeidos
            .ToDictionary(s => s.SucursalProductoId, s => (Cantidad: s.SaldoCantidad, Valor: s.SaldoValor));

        var consumoPorLote = new Dictionary<int, decimal>();
        var movimientos = new List<KardexMovimientoConDetalle>(consumos.Count);
        var resultados = new List<ConsumoPepsResultDTO>(consumos.Count);
        var fechaMovimiento = DateTime.Now;

        foreach (var consumo in consumos)
        {
            var lotes = lotesPorProducto.TryGetValue(consumo.SucursalProductoId, out var encontrados)
                ? encontrados
                : [];

            var restante = consumo.Cantidad;
            var costoTotal = 0m;
            var detalle = new List<KardexMovimientoLote>();

            // El saldo de cada lote baja en memoria, de modo que si el mismo producto aparece
            // en varias lineas de la venta la segunda arranca donde termino la primera.
            foreach (var lote in lotes)
            {
                if (restante <= 0) break;
                if (lote.SaldoCantidad <= 0) continue;

                var tomar = Math.Min(lote.SaldoCantidad, restante);
                lote.SaldoCantidad -= tomar;

                consumoPorLote[lote.InventarioLoteId] =
                    consumoPorLote.GetValueOrDefault(lote.InventarioLoteId) + tomar;

                costoTotal += tomar * lote.CostoUnitario;
                detalle.Add(new KardexMovimientoLote
                {
                    InventarioLoteId = lote.InventarioLoteId,
                    Cantidad = tomar,
                    CostoUnitario = lote.CostoUnitario
                });
                restante -= tomar;
            }

            if (restante > 0)
                throw new InvalidOperationException(
                    $"Stock insuficiente en lotes PEPS para SucursalProductoId {consumo.SucursalProductoId}: faltan {restante} unidades por cubrir.");

            // El saldo posterior se calcula restando sobre el saldo previo ya leido, en vez de
            // volver a sumar los lotes en la base despues de cada producto.
            var saldoPrevio = saldos.GetValueOrDefault(consumo.SucursalProductoId);
            var saldoCantidad = saldoPrevio.Cantidad - consumo.Cantidad;
            var saldoValor = saldoPrevio.Valor - costoTotal;
            saldos[consumo.SucursalProductoId] = (saldoCantidad, saldoValor);

            var costoUnitarioPromedio = costoTotal / consumo.Cantidad;

            movimientos.Add(new KardexMovimientoConDetalle
            {
                Movimiento = new KardexMovimiento
                {
                    SucursalProductoId = consumo.SucursalProductoId,
                    TipoMovimiento = consumo.TipoMovimiento,
                    ReferenciaTipo = consumo.ReferenciaTipo,
                    ReferenciaId = consumo.ReferenciaId,
                    ComprobanteDetalleId = consumo.ComprobanteDetalleId,
                    Cantidad = consumo.Cantidad,
                    CostoUnitarioPromedio = costoUnitarioPromedio,
                    CostoTotal = costoTotal,
                    SaldoCantidadPost = saldoCantidad,
                    SaldoValorPost = saldoValor,
                    FechaMovimiento = fechaMovimiento,
                    IdUsuario = idUsuario
                },
                Lotes = detalle
            });

            resultados.Add(new ConsumoPepsResultDTO
            {
                SucursalProductoId = consumo.SucursalProductoId,
                CantidadConsumida = consumo.Cantidad,
                CostoUnitarioPromedio = costoUnitarioPromedio,
                CostoTotal = costoTotal
            });
        }

        // Descuento de lotes y escritura del kardex viajan juntos en un solo comando. Si
        // varias lineas consumen del mismo lote, sus cantidades ya vienen acumuladas en una
        // unica resta.
        var lotesDescontados = await _unitOfWork.InventarioLotes.AplicarConsumoPepsAsync(consumoPorLote, movimientos);
        if (lotesDescontados != consumoPorLote.Count)
            throw new InvalidOperationException(
                $"Se esperaba descontar {consumoPorLote.Count} lotes y solo {lotesDescontados} tenian saldo suficiente; algun saldo cambio de forma concurrente.");

        for (var i = 0; i < resultados.Count; i++)
            resultados[i].KardexMovimientoId = movimientos[i].Movimiento.KardexMovimientoId;

        return resultados;
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

    public async Task<IEnumerable<KardexMovimientoDTO>> GetKardexAsync(int sucursalProductoId, int? productoId, DateTime? desde, DateTime? hasta)
    {
        var movimientos = await _unitOfWork.InventarioLotes.GetKardexAsync(sucursalProductoId, productoId, desde, hasta);
        return movimientos.Select(m => new KardexMovimientoDTO
        {
            KardexMovimientoId = m.Movimiento.KardexMovimientoId,
            SucursalProductoId = m.Movimiento.SucursalProductoId,
            TipoMovimiento = m.Movimiento.TipoMovimiento,
            ReferenciaTipo = m.Movimiento.ReferenciaTipo,
            ReferenciaId = m.Movimiento.ReferenciaId,
            ComprobanteDetalleId = m.Movimiento.ComprobanteDetalleId,
            Cantidad = m.Movimiento.Cantidad,
            CostoUnitarioPromedio = m.Movimiento.CostoUnitarioPromedio,
            CostoTotal = m.Movimiento.CostoTotal,
            SaldoCantidadPost = m.Movimiento.SaldoCantidadPost,
            SaldoValorPost = m.Movimiento.SaldoValorPost,
            FechaMovimiento = m.Movimiento.FechaMovimiento,
            LotesConsumidos = m.Movimiento.LotesConsumidos,
            ProductoId = m.ProductoId,
            NomProducto = m.NomProducto,
            Codigo = m.Codigo,
            EsPaquete = m.EsPaquete,
            CantidadVenta = m.CantidadVenta,
            CostoVenta = m.CostoVenta
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
                SaldoCantidad = l.SaldoCantidad,
                FechaVencimiento = l.FechaVencimiento
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
                    SaldoCantidad = l.SaldoCantidad,
                    FechaVencimiento = l.FechaVencimiento
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

    public async Task<IEnumerable<RentabilidadDiariaDTO>> GetRentabilidadDiariaAsync(int sucursalId, int productoId, DateTime? desde, DateTime? hasta)
    {
        return await _unitOfWork.InventarioLotes.GetRentabilidadDiariaAsync(sucursalId, productoId, desde, hasta);
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

                var stockOk = await _unitOfWork.Productos.ActualizarStockAsync(spId, cantidadProducto);
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

    public async Task<IEnumerable<LoteVencidoDTO>> GetLotesVencidosReporteAsync(int? sucursalId = null)
    {
        return await _unitOfWork.InventarioLotes.GetLotesVencidosReporteAsync(sucursalId);
    }

    // No bloquea si el lote no tiene ventas o si ya se vendió por completo (saldoCantidad == 0):
    // en ambos casos no hay stock físico con fecha ambigua. Solo pide confirmación cuando hay
    // venta parcial (una parte del lote sigue en stock y otra ya se vendió con la fecha vieja),
    // porque ahí el cambio sí puede afectar tanto el stock actual como el historial de ventas.
    public async Task<ActualizarFechaVencimientoResultDTO> ActualizarFechaVencimientoLoteAsync(int inventarioLoteId, DateTime? fechaVencimiento, bool confirmar = false)
    {
        _unitOfWork.BeginTransaction();
        try
        {
            var lote = await _unitOfWork.InventarioLotes.GetPorIdAsync(inventarioLoteId);
            if (lote == null || !lote.Estado)
            {
                _unitOfWork.Rollback();
                return new ActualizarFechaVencimientoResultDTO { Encontrado = false };
            }

            var cantidadVendida = lote.CantidadOriginal - lote.SaldoCantidad;
            var ventaParcial = lote.SaldoCantidad > 0 && cantidadVendida > 0;

            if (ventaParcial && !confirmar)
            {
                _unitOfWork.Rollback();
                return new ActualizarFechaVencimientoResultDTO
                {
                    RequiereConfirmacion = true,
                    CantidadVendida = cantidadVendida,
                    CantidadOriginal = lote.CantidadOriginal,
                    SaldoCantidad = lote.SaldoCantidad
                };
            }

            var actualizado = await _unitOfWork.InventarioLotes.ActualizarFechaVencimientoAsync(inventarioLoteId, fechaVencimiento);
            _unitOfWork.Commit();
            return new ActualizarFechaVencimientoResultDTO { Actualizado = actualizado };
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<HistorialVencidoDTO>> GetHistorialVencidosRetiradosAsync(int sucursalId, DateTime? desde, DateTime? hasta)
    {
        return await _unitOfWork.InventarioLotes.GetHistorialVencidosRetiradosAsync(sucursalId, desde, hasta);
    }
}
