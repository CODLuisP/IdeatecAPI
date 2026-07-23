using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Inventario.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class InventarioLoteRepository : DapperRepository<InventarioLote>, IInventarioLoteRepository
{
    public InventarioLoteRepository(IDbConnection connection, IDbTransaction? transaction = null) : base(connection, transaction)
    {
    }

    private const string SelectLoteBase = @"
        SELECT
            il.inventarioLoteID   AS InventarioLoteId,
            il.sucursalProductoID AS SucursalProductoId,
            il.compraProveedorID  AS CompraProveedorId,
            il.origen             AS Origen,
            il.fechaLote          AS FechaLote,
            il.cantidadOriginal   AS CantidadOriginal,
            il.costoUnitario      AS CostoUnitario,
            il.saldoCantidad      AS SaldoCantidad,
            il.estado             AS Estado,
            il.fechaCreacion      AS FechaCreacion,
            il.fechaVencimiento   AS FechaVencimiento
        FROM inventario_lote il";

    public async Task<InventarioLote> CrearLoteAsync(InventarioLote lote)
    {
        var sql = @"
            INSERT INTO inventario_lote
                (sucursalProductoID, compraProveedorID, origen, fechaLote, cantidadOriginal, costoUnitario, saldoCantidad, estado, fechaCreacion, fechaVencimiento)
            VALUES
                (@SucursalProductoId, @CompraProveedorId, @Origen, @FechaLote, @CantidadOriginal, @CostoUnitario, @SaldoCantidad, @Estado, NOW(), @FechaVencimiento);
            SELECT LAST_INSERT_ID();";

        var newId = await _connection.ExecuteScalarAsync<int>(sql, lote, _transaction);
        lote.InventarioLoteId = newId;
        return lote;
    }

    public async Task<IEnumerable<InventarioLote>> GetLotesConSaldoFifoAsync(int sucursalProductoId)
    {
        var sql = $@"{SelectLoteBase}
            WHERE il.sucursalProductoID = @SucursalProductoId
            AND il.estado = 1
            AND il.saldoCantidad > 0
            ORDER BY (il.fechaVencimiento IS NULL), il.fechaVencimiento ASC, il.fechaLote ASC, il.inventarioLoteID ASC
            FOR UPDATE;";

        return await _connection.QueryAsync<InventarioLote>(sql, new { SucursalProductoId = sucursalProductoId }, _transaction);
    }

    public async Task<bool> DescontarSaldoLoteAsync(int inventarioLoteId, decimal cantidad)
    {
        var sql = @"
            UPDATE inventario_lote
            SET saldoCantidad = saldoCantidad - @Cantidad
            WHERE inventarioLoteID = @InventarioLoteId
            AND saldoCantidad >= @Cantidad";

        var filas = await _connection.ExecuteAsync(sql, new { InventarioLoteId = inventarioLoteId, Cantidad = cantidad }, _transaction);
        return filas > 0;
    }

    public async Task<IEnumerable<InventarioLote>> GetLotesReporteAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta)
    {
        var sql = $@"{SelectLoteBase}
            WHERE il.sucursalProductoID = @SucursalProductoId
            AND il.estado = 1
            AND (@Desde IS NULL OR il.fechaLote >= @Desde)
            AND (@Hasta IS NULL OR il.fechaLote <= @Hasta)
            ORDER BY il.fechaLote ASC, il.inventarioLoteID ASC;";

        return await _connection.QueryAsync<InventarioLote>(sql, new { SucursalProductoId = sucursalProductoId, Desde = desde, Hasta = FinDeDia(hasta) }, _transaction);
    }

    public async Task<decimal> GetSaldoValorizadoAsync(int sucursalProductoId)
    {
        var sql = @"
            SELECT COALESCE(SUM(saldoCantidad * costoUnitario), 0)
            FROM inventario_lote
            WHERE sucursalProductoID = @SucursalProductoId
            AND estado = 1";

        return await _connection.ExecuteScalarAsync<decimal>(sql, new { SucursalProductoId = sucursalProductoId }, _transaction);
    }

    public async Task<decimal> GetSaldoCantidadLotesAsync(int sucursalProductoId)
    {
        var sql = @"
            SELECT COALESCE(SUM(saldoCantidad), 0)
            FROM inventario_lote
            WHERE sucursalProductoID = @SucursalProductoId
            AND estado = 1";

        return await _connection.ExecuteScalarAsync<decimal>(sql, new { SucursalProductoId = sucursalProductoId }, _transaction);
    }

    public async Task<IEnumerable<InventarioLote>> GetSaldoValorizadoSucursalAsync(int sucursalId)
    {
        var sql = @"
            SELECT
                il.inventarioLoteID   AS InventarioLoteId,
                il.sucursalProductoID AS SucursalProductoId,
                il.compraProveedorID  AS CompraProveedorId,
                il.origen             AS Origen,
                il.fechaLote          AS FechaLote,
                il.cantidadOriginal   AS CantidadOriginal,
                il.costoUnitario      AS CostoUnitario,
                il.saldoCantidad      AS SaldoCantidad,
                il.estado             AS Estado,
                il.fechaCreacion      AS FechaCreacion,
                il.fechaVencimiento   AS FechaVencimiento,
                p.nomProducto         AS NomProducto,
                p.codigo              AS Codigo
            FROM inventario_lote il
            INNER JOIN sucursalproducto sp ON sp.sucursalProductoID = il.sucursalProductoID
            INNER JOIN producto p ON p.productoID = sp.productoID
            WHERE sp.sucursalID = @SucursalId
            AND il.estado = 1
            AND il.saldoCantidad > 0
            ORDER BY p.nomProducto ASC, il.fechaLote ASC;";

        return await _connection.QueryAsync<InventarioLote>(sql, new { SucursalId = sucursalId }, _transaction);
    }

    public async Task<KardexMovimiento> RegistrarMovimientoAsync(KardexMovimiento movimiento, IEnumerable<KardexMovimientoLote> detalleLotes)
    {
        var sqlHeader = @"
            INSERT INTO kardex_movimiento
                (sucursalProductoID, tipoMovimiento, referenciaTipo, referenciaID, cantidad,
                 costoUnitarioPromedio, costoTotal, saldoCantidadPost, saldoValorPost, fechaMovimiento, idUsuario)
            VALUES
                (@SucursalProductoId, @TipoMovimiento, @ReferenciaTipo, @ReferenciaId, @Cantidad,
                 @CostoUnitarioPromedio, @CostoTotal, @SaldoCantidadPost, @SaldoValorPost, @FechaMovimiento, @IdUsuario);
            SELECT LAST_INSERT_ID();";

        var newId = await _connection.ExecuteScalarAsync<int>(sqlHeader, movimiento, _transaction);
        movimiento.KardexMovimientoId = newId;

        var detalles = detalleLotes.ToList();
        if (detalles.Count > 0)
        {
            foreach (var detalle in detalles)
                detalle.KardexMovimientoId = newId;

            var sqlDetalle = @"
                INSERT INTO kardex_movimiento_lote
                    (kardexMovimientoID, inventarioLoteID, cantidad, costoUnitario)
                VALUES
                    (@KardexMovimientoId, @InventarioLoteId, @Cantidad, @CostoUnitario);";

            await _connection.ExecuteAsync(sqlDetalle, detalles, _transaction);
        }

        return movimiento;
    }

    public async Task<IEnumerable<KardexMovimiento>> GetKardexAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta)
    {
        var sql = @"
            SELECT
                km.kardexMovimientoID    AS KardexMovimientoId,
                km.sucursalProductoID    AS SucursalProductoId,
                km.tipoMovimiento        AS TipoMovimiento,
                km.referenciaTipo        AS ReferenciaTipo,
                km.referenciaID          AS ReferenciaId,
                km.cantidad              AS Cantidad,
                km.costoUnitarioPromedio AS CostoUnitarioPromedio,
                km.costoTotal            AS CostoTotal,
                km.saldoCantidadPost     AS SaldoCantidadPost,
                km.saldoValorPost        AS SaldoValorPost,
                km.fechaMovimiento       AS FechaMovimiento,
                km.idUsuario             AS IdUsuario,
                (SELECT COUNT(*) FROM kardex_movimiento_lote kml WHERE kml.kardexMovimientoID = km.kardexMovimientoID) AS LotesConsumidos
            FROM kardex_movimiento km
            WHERE km.sucursalProductoID = @SucursalProductoId
            AND (@Desde IS NULL OR km.fechaMovimiento >= @Desde)
            AND (@Hasta IS NULL OR km.fechaMovimiento <= @Hasta)
            ORDER BY km.fechaMovimiento ASC, km.kardexMovimientoID ASC;";

        return await _connection.QueryAsync<KardexMovimiento>(sql, new { SucursalProductoId = sucursalProductoId, Desde = desde, Hasta = FinDeDia(hasta) }, _transaction);
    }

    public async Task<bool> ExisteLoteSaldoInicialAsync(int sucursalProductoId)
    {
        var sql = @"
            SELECT COUNT(1) FROM inventario_lote
            WHERE sucursalProductoID = @SucursalProductoId
            AND origen = 'SALDO_INICIAL'";

        var count = await _connection.ExecuteScalarAsync<int>(sql, new { SucursalProductoId = sucursalProductoId }, _transaction);
        return count > 0;
    }

    public async Task<KardexMovimiento?> GetUltimoMovimientoSalidaPorReferenciaAsync(string referenciaTipo, int referenciaId, int sucursalProductoId)
    {
        var sql = @"
            SELECT
                km.kardexMovimientoID    AS KardexMovimientoId,
                km.sucursalProductoID    AS SucursalProductoId,
                km.tipoMovimiento        AS TipoMovimiento,
                km.referenciaTipo        AS ReferenciaTipo,
                km.referenciaID          AS ReferenciaId,
                km.cantidad              AS Cantidad,
                km.costoUnitarioPromedio AS CostoUnitarioPromedio,
                km.costoTotal            AS CostoTotal,
                km.saldoCantidadPost     AS SaldoCantidadPost,
                km.saldoValorPost        AS SaldoValorPost,
                km.fechaMovimiento       AS FechaMovimiento,
                km.idUsuario             AS IdUsuario
            FROM kardex_movimiento km
            WHERE km.referenciaTipo = @ReferenciaTipo
            AND km.referenciaID = @ReferenciaId
            AND km.sucursalProductoID = @SucursalProductoId
            AND km.tipoMovimiento LIKE 'SALIDA%'
            ORDER BY km.fechaMovimiento DESC, km.kardexMovimientoID DESC
            LIMIT 1;";

        return await _connection.QueryFirstOrDefaultAsync<KardexMovimiento>(sql,
            new { ReferenciaTipo = referenciaTipo, ReferenciaId = referenciaId, SucursalProductoId = sucursalProductoId }, _transaction);
    }

    public async Task<IEnumerable<RentabilidadProductoDTO>> GetRentabilidadPorProductoAsync(int sucursalId, DateTime? desde, DateTime? hasta)
    {
        // Costo (COGS) sale del Kardex PEPS de las salidas por venta; el ingreso se toma de
        // comprobantedetalle (con IGV incluido, vía totalVentaItem) cruzando por comprobante+producto.
        // Se incluyen comprobantes tributarios aceptados/pendientes y Notas de Venta (tipoComprobante
        // 'NV', estadoSunat 'NO_APLICA' porque no tributan ante SUNAT) para que toda venta real
        // (facture o no) cuente como ingreso.
        // Nota: en productos tipo paquete, el costo PEPS queda registrado en el producto BASE
        // (por diseño, el stock/lotes viven ahí), mientras que la venta se registra sobre el
        // productoId del paquete — para esos casos el cruce de ingreso no calza con el costo,
        // así que el paquete puede no verse en este reporte hasta que se trate ese caso aparte.
        var sql = @"
            SELECT
                sp.productoID                    AS ProductoId,
                MAX(sp.sucursalProductoID)        AS SucursalProductoId,
                p.nomProducto                     AS NomProducto,
                p.codigo                          AS Codigo,
                SUM(km.cantidad)                  AS CantidadVendida,
                COALESCE(SUM(ventas.ingreso), 0)   AS IngresoVentas,
                SUM(km.costoTotal)                AS CostoVentas
            FROM kardex_movimiento km
            INNER JOIN sucursalproducto sp ON sp.sucursalProductoID = km.sucursalProductoID
            INNER JOIN producto p ON p.productoID = sp.productoID
            LEFT JOIN (
                SELECT cd.comprobanteId, cd.productoId, SUM(cd.totalVentaItem) AS ingreso
                FROM comprobantedetalle cd
                INNER JOIN comprobante c ON c.comprobanteID = cd.comprobanteId
                WHERE (
                    (c.tipoComprobante <> 'NV' AND c.estadoSunat IN ('ACEPTADO', 'ACEPTADO_CON_OBSERVACIONES', 'PENDIENTE'))
                    OR (c.tipoComprobante = 'NV' AND c.estadoSunat = 'NO_APLICA')
                )
                GROUP BY cd.comprobanteId, cd.productoId
            ) ventas ON ventas.comprobanteId = km.referenciaID AND ventas.productoId = sp.productoID
            WHERE km.tipoMovimiento = 'SALIDA_VENTA'
            AND km.referenciaTipo = 'COMPROBANTE'
            AND sp.sucursalID = @SucursalId
            AND (@Desde IS NULL OR km.fechaMovimiento >= @Desde)
            AND (@Hasta IS NULL OR km.fechaMovimiento <= @Hasta)
            GROUP BY sp.productoID, p.nomProducto, p.codigo
            ORDER BY p.nomProducto ASC;";

        return await _connection.QueryAsync<RentabilidadProductoDTO>(sql, new { SucursalId = sucursalId, Desde = desde, Hasta = FinDeDia(hasta) }, _transaction);
    }

    public async Task<IEnumerable<RentabilidadDiariaDTO>> GetRentabilidadDiariaAsync(int sucursalProductoId, DateTime? desde, DateTime? hasta)
    {
        // Misma lógica de costo/ingreso que GetRentabilidadPorProductoAsync, pero agrupada por día
        // y acotada a un único sucursalProductoId, para graficar la evolución de costo/utilidad.
        var sql = @"
            SELECT
                DATE(km.fechaMovimiento)          AS Fecha,
                SUM(km.cantidad)                  AS CantidadVendida,
                COALESCE(SUM(ventas.ingreso), 0)   AS IngresoVentas,
                SUM(km.costoTotal)                AS CostoVentas
            FROM kardex_movimiento km
            LEFT JOIN (
                SELECT cd.comprobanteId, cd.productoId, SUM(cd.totalVentaItem) AS ingreso
                FROM comprobantedetalle cd
                INNER JOIN comprobante c ON c.comprobanteID = cd.comprobanteId
                WHERE (
                    (c.tipoComprobante <> 'NV' AND c.estadoSunat IN ('ACEPTADO', 'ACEPTADO_CON_OBSERVACIONES', 'PENDIENTE'))
                    OR (c.tipoComprobante = 'NV' AND c.estadoSunat = 'NO_APLICA')
                )
                GROUP BY cd.comprobanteId, cd.productoId
            ) ventas ON ventas.comprobanteId = km.referenciaID
                AND ventas.productoId = (SELECT sp.productoID FROM sucursalproducto sp WHERE sp.sucursalProductoID = km.sucursalProductoID)
            WHERE km.tipoMovimiento = 'SALIDA_VENTA'
            AND km.referenciaTipo = 'COMPROBANTE'
            AND km.sucursalProductoID = @SucursalProductoId
            AND (@Desde IS NULL OR km.fechaMovimiento >= @Desde)
            AND (@Hasta IS NULL OR km.fechaMovimiento <= @Hasta)
            GROUP BY DATE(km.fechaMovimiento)
            ORDER BY DATE(km.fechaMovimiento) ASC;";

        return await _connection.QueryAsync<RentabilidadDiariaDTO>(sql, new { SucursalProductoId = sucursalProductoId, Desde = desde, Hasta = FinDeDia(hasta) }, _transaction);
    }

    // Normaliza el filtro "hasta" para que incluya todo el día seleccionado (23:59:59.999...),
    // ya que el input de fecha llega sin hora (medianoche) y de lo contrario excluiría los
    // movimientos del propio día "hasta" (incluido el caso desde == hasta).
    private static DateTime? FinDeDia(DateTime? hasta) => hasta?.Date.AddDays(1).AddTicks(-1);

    public async Task<IEnumerable<InventarioLote>> GetByCompraProveedorIdAsync(int compraProveedorId)
    {
        // FOR UPDATE: al usarse dentro de una transacción (ver CompraProveedorService.EliminarAsync),
        // bloquea la fila contra ConsumirFifoAsync (que también lockea con FOR UPDATE), evitando que
        // una venta concurrente consuma el lote justo entre el chequeo de "¿ya se vendió?" y el borrado.
        var sql = $@"{SelectLoteBase}
            WHERE il.compraProveedorID = @CompraProveedorId
            FOR UPDATE;";

        return await _connection.QueryAsync<InventarioLote>(sql, new { CompraProveedorId = compraProveedorId }, _transaction);
    }

    public async Task<IEnumerable<InventarioLote>> GetLotesVencidosAsync(int? sucursalProductoId = null)
    {
        var sql = $@"{SelectLoteBase}
            WHERE il.fechaVencimiento < CURDATE()
            AND il.estado = 1
            AND il.saldoCantidad > 0
            AND (@SucursalProductoId IS NULL OR il.sucursalProductoID = @SucursalProductoId)
            ORDER BY il.sucursalProductoID ASC, il.fechaVencimiento ASC;";

        return await _connection.QueryAsync<InventarioLote>(sql,
            new { SucursalProductoId = sucursalProductoId }, _transaction);
    }

    // Vista previa de solo lectura (no descuenta ni desactiva nada) para mostrar en el frontend
    // antes de confirmar el retiro real vía RetirarLotesVencidosAsync.
    public async Task<IEnumerable<LoteVencidoDTO>> GetLotesVencidosReporteAsync(int? sucursalId = null)
    {
        var sql = @"
            SELECT
                il.inventarioLoteID   AS InventarioLoteId,
                il.sucursalProductoID AS SucursalProductoId,
                p.nomProducto         AS NomProducto,
                p.codigo              AS Codigo,
                il.origen             AS Origen,
                il.fechaLote          AS FechaLote,
                il.fechaVencimiento   AS FechaVencimiento,
                il.saldoCantidad      AS SaldoCantidad,
                il.costoUnitario      AS CostoUnitario
            FROM inventario_lote il
            INNER JOIN sucursalproducto sp ON sp.sucursalProductoID = il.sucursalProductoID
            INNER JOIN producto p ON p.productoID = sp.productoID
            WHERE il.fechaVencimiento < CURDATE()
            AND il.estado = 1
            AND il.saldoCantidad > 0
            AND (@SucursalId IS NULL OR sp.sucursalID = @SucursalId)
            ORDER BY il.fechaVencimiento ASC, p.nomProducto ASC;";

        return await _connection.QueryAsync<LoteVencidoDTO>(sql, new { SucursalId = sucursalId }, _transaction);
    }

    // Corrige la fecha de vencimiento de un lote ya registrado (p.ej. error de tipeo al comprar).
    // No afecta cantidad/costo ni ningún movimiento de Kardex ya registrado, solo el dato del lote
    // en sí (usado para el orden FEFO y para mostrarlo). Solo se permite sobre lotes activos:
    // uno ya dado de baja (estado = 0) es historia cerrada.
    public async Task<IEnumerable<HistorialVencidoDTO>> GetHistorialVencidosRetiradosAsync(int sucursalId, DateTime? desde, DateTime? hasta)
    {
        var sql = @"
            SELECT
                km.kardexMovimientoID    AS KardexMovimientoId,
                km.sucursalProductoID    AS SucursalProductoId,
                p.nomProducto            AS NomProducto,
                p.codigo                 AS Codigo,
                km.cantidad              AS Cantidad,
                km.costoUnitarioPromedio AS CostoUnitarioPromedio,
                km.costoTotal            AS CostoTotal,
                km.fechaMovimiento       AS FechaMovimiento
            FROM kardex_movimiento km
            INNER JOIN sucursalproducto sp ON sp.sucursalProductoID = km.sucursalProductoID
            INNER JOIN producto p ON p.productoID = sp.productoID
            WHERE km.tipoMovimiento = 'SALIDA_VENCIMIENTO'
            AND sp.sucursalID = @SucursalId
            AND (@Desde IS NULL OR km.fechaMovimiento >= @Desde)
            AND (@Hasta IS NULL OR km.fechaMovimiento <= @Hasta)
            ORDER BY km.fechaMovimiento DESC, km.kardexMovimientoID DESC;";

        return await _connection.QueryAsync<HistorialVencidoDTO>(sql,
            new { SucursalId = sucursalId, Desde = desde, Hasta = FinDeDia(hasta) }, _transaction);
    }

    public async Task<bool> ActualizarFechaVencimientoAsync(int inventarioLoteId, DateTime? fechaVencimiento)
    {
        var sql = @"
            UPDATE inventario_lote
            SET fechaVencimiento = @FechaVencimiento
            WHERE inventarioLoteID = @InventarioLoteId
            AND estado = 1;";

        var result = await _connection.ExecuteAsync(sql,
            new { InventarioLoteId = inventarioLoteId, FechaVencimiento = fechaVencimiento }, _transaction);
        return result > 0;
    }

    public async Task<bool> DesactivarLoteAsync(int inventarioLoteId)
    {
        var sql = @"
            UPDATE inventario_lote
            SET saldoCantidad = 0, estado = 0
            WHERE inventarioLoteID = @InventarioLoteId
            AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql,
            new { InventarioLoteId = inventarioLoteId }, _transaction);
        return filas > 0;
    }

    /// <summary>
    /// Deshace la entrada PEPS de una compra (kardex_movimiento_lote → kardex_movimiento → inventario_lote,
    /// en ese orden por las FK). Solo debe invocarse cuando saldoCantidad == cantidadOriginal, es decir,
    /// nada se vendió todavía de ese lote (si algo se vendió, el consumo generó otro kardex_movimiento_lote
    /// sobre el mismo lote, y este método lo dejaría huérfano).
    /// </summary>
    public async Task EliminarEntradaLoteAsync(int inventarioLoteId)
    {
        var kardexMovimientoIds = await _connection.QueryAsync<int>(
            "SELECT DISTINCT kardexMovimientoID FROM kardex_movimiento_lote WHERE inventarioLoteID = @InventarioLoteId;",
            new { InventarioLoteId = inventarioLoteId }, _transaction);

        await _connection.ExecuteAsync(
            "DELETE FROM kardex_movimiento_lote WHERE inventarioLoteID = @InventarioLoteId;",
            new { InventarioLoteId = inventarioLoteId }, _transaction);

        var ids = kardexMovimientoIds.ToList();
        if (ids.Count > 0)
        {
            await _connection.ExecuteAsync(
                "DELETE FROM kardex_movimiento WHERE kardexMovimientoID IN @Ids;",
                new { Ids = ids }, _transaction);
        }

        await _connection.ExecuteAsync(
            "DELETE FROM inventario_lote WHERE inventarioLoteID = @InventarioLoteId;",
            new { InventarioLoteId = inventarioLoteId }, _transaction);
    }
}
