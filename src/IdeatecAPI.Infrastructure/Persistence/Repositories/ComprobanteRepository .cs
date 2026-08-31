using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories.Comprobantes;

public class ComprobanteRepository : DapperRepository<Comprobante>, IComprobanteRepository
{
    public ComprobanteRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction) { }

    public async Task<int> GenerarComprobanteAsync(Comprobante comprobante) //guardar comprobante en BD
    {
        var sql = @"
            INSERT INTO comprobante (
                tipoOperacion, tipoComprobante, serie, correlativo,
                fechaEmision, horaEmision, fechaVencimiento, tipoMoneda,
                tipoPago, tipoCambio,
                empresaID, empresaRuc, empresaRazonSocial, empresaNombreComercial,
                empresaDireccion, empresaProvincia, empresaDepartamento,
                empresaDistrito, empresaUbigeo, establecimientoAnexo,
                clienteID, clienteTipoDoc, clienteNumDoc, clienteRznSocial,
                clienteDireccion, clienteProvincia, clienteDepartamento,
                clienteDistrito, clienteUbigeo, clienteCorreo, enviadoPorCorreo, clienteWhatsApp, enviadoPorWhatsApp,
                codigoTipoDescGlobal, descuentoGlobal, totalOperacionesGravadas, totalOperacionesExoneradas, 
                totalOperacionesInafectas, totalOperacionesGratuitas, totalIgvGratuitas, totalIGV, totalDescuentos, totalOtrosCargos,
                totalIcbper, totalImpuestos, valorVenta, subTotal, importeTotal, montoCredito, totalComisionPagoTarjeta,
                estadoSunat, enviadoEnResumen, xmlGenerado, usuarioCreacion, fechaCreacion, codigoHashCPE, observaciones
            ) VALUES (
                @TipoOperacion, @TipoComprobante, @Serie, @Correlativo,
                @FechaEmision, @HoraEmision, @FechaVencimiento, @TipoMoneda,
                @TipoPago, @TipoCambio,
                @EmpresaId, @EmpresaRuc, @EmpresaRazonSocial, @EmpresaNombreComercial,
                @EmpresaDireccion, @EmpresaProvincia, @EmpresaDepartamento,
                @EmpresaDistrito, @EmpresaUbigeo, @EmpresaEstablecimientoAnexo,
                @ClienteId, @ClienteTipoDoc, @ClienteNumDoc, @ClienteRazonSocial,
                @ClienteDireccion, @ClienteProvincia, @ClienteDepartamento,
                @ClienteDistrito, @ClienteUbigeo, @ClienteCorreo, @EnviadoPorCorreo, @ClienteWhatsApp, @EnviadoPorWhatsApp,
                @codigoTipoDescGlobal,  @DescuentoGlobal, @TotalOperacionesGravadas, @TotalOperacionesExoneradas, 
                @TotalOperacionesInafectas, @TotalOperacionesGratuitas, @TotalIgvGratuitas, @TotalIGV, @TotalDescuentos, @TotalOtrosCargos,
                @TotalIcbper, @TotalImpuestos, @ValorVenta, @SubTotal, @ImporteTotal, @MontoCredito, @TotalComisionPagoTarjeta,
                @EstadoSunat,  @EnviadoEnResumen, @XmlGenerado, @UsuarioCreacion, @FechaCreacion, @CodigoHashCPE, @Observaciones
            );";

        var parameters = new
        {
            comprobante.TipoOperacion,
            comprobante.TipoComprobante,
            comprobante.Serie,
            comprobante.Correlativo,
            FechaEmision = comprobante.FechaEmision.Date,
            HoraEmision = comprobante.HoraEmision.TimeOfDay,
            FechaVencimiento = comprobante.FechaVencimiento.Date,
            comprobante.TipoMoneda,
            comprobante.TipoPago,
            comprobante.TipoCambio,
            comprobante.EmpresaId,
            comprobante.EmpresaRuc,
            comprobante.EmpresaRazonSocial,
            comprobante.EmpresaNombreComercial,
            comprobante.EmpresaDireccion,
            comprobante.EmpresaProvincia,
            comprobante.EmpresaDepartamento,
            comprobante.EmpresaDistrito,
            comprobante.EmpresaUbigeo,
            comprobante.EmpresaEstablecimientoAnexo,
            comprobante.ClienteId,
            comprobante.ClienteTipoDoc,
            comprobante.ClienteNumDoc,
            comprobante.ClienteRazonSocial,
            comprobante.ClienteDireccion,
            comprobante.ClienteProvincia,
            comprobante.ClienteDepartamento,
            comprobante.ClienteDistrito,
            comprobante.ClienteUbigeo,
            comprobante.ClienteCorreo,
            comprobante.EnviadoPorCorreo,
            comprobante.ClienteWhatsApp,
            comprobante.EnviadoPorWhatsApp,
            comprobante.CodigoTipoDescGlobal,
            comprobante.DescuentoGlobal,
            comprobante.TotalOperacionesGravadas,
            comprobante.TotalOperacionesExoneradas,
            comprobante.TotalOperacionesInafectas,
            comprobante.TotalOperacionesGratuitas,
            comprobante.TotalIgvGratuitas,
            comprobante.TotalIGV,
            comprobante.TotalDescuentos,
            comprobante.TotalOtrosCargos,
            comprobante.TotalIcbper,
            comprobante.TotalImpuestos,
            comprobante.ValorVenta,
            comprobante.SubTotal,
            comprobante.ImporteTotal,
            comprobante.MontoCredito,
            comprobante.TotalComisionPagoTarjeta,
            comprobante.EstadoSunat,
            comprobante.XmlGenerado,
            comprobante.EnviadoEnResumen,
            comprobante.UsuarioCreacion,
            comprobante.FechaCreacion,
            comprobante.CodigoHashCPE,
            comprobante.Observaciones
        };

        // Cabecera e hijos viajan en un unico comando. La variable @comprobanteNuevo arrastra
        // el ID recien insertado hacia los INSERT de detalles, pagos, etc., de modo que no hay
        // que volver del servidor solo para conocerlo. Antes esto costaba un viaje de red por
        // cada tabla hija; ahora es uno para todas.
        var parametros = new DynamicParameters();
        parametros.AddDynamicParams(parameters);

        const string idNuevo = "@comprobanteNuevo";

        var sentencias = new List<string>
        {
            sql,
            $" SET {idNuevo} = LAST_INSERT_ID(); ",
            ConstruirDetallesInsert(comprobante.Detalles, parametros, idNuevo),
            ConstruirLeyendasInsert(comprobante.Leyendas, parametros, idNuevo),
            ConstruirPagosInsert(comprobante.Pagos, parametros, idNuevo),
            ConstruirCuotasInsert(comprobante.Cuotas, parametros, idNuevo),
            ConstruirGuiasInsert(comprobante.Guias, parametros, idNuevo),
            ConstruirDetraccionesInsert(comprobante.Detracciones, parametros, idNuevo),
            $" SELECT {idNuevo};"
        };

        return await _connection.ExecuteScalarAsync<int>(string.Concat(sentencias), parametros, _transaction);
    }

    private async Task RegistrarDetalleAsync(ComprobanteDetalle d)
    {
        var sql = @"
            INSERT INTO comprobantedetalle (
                comprobanteId, trabajadorID, item, productoId, codigo, descripcion, cantidad,
                unidadMedida, precioUnitario, tipoAfectacionIGV, porcentajeIGV,
                montoIGV, baseIgv, codigoTipoDescuento, descuentoUnitario, descuentoTotal,
                valorVenta, precioVenta, totalVentaItem, icbper, factorIcbper
            ) VALUES (
                @ComprobanteId, @TrabajadorID, @Item, @ProductoId, @Codigo, @Descripcion, @Cantidad,
                @UnidadMedida, @PrecioUnitario, @TipoAfectacionIGV, @PorcentajeIGV,
                @MontoIGV, @BaseIgv, @codigoTipoDescuento, @DescuentoUnitario, @DescuentoTotal,
                @ValorVenta, @PrecioVenta, @TotalVentaItem, @Icbper, @FactorIcbper
            );";

        await _connection.ExecuteAsync(sql, d, _transaction);
    }

    private async Task RegistrarLeyendaAsync(NoteLegend l)
    {
        var sql = @"
            INSERT INTO notelegend (comprobanteId, code, value)
            VALUES (@ComprobanteId, @Code, @Value);";

        await _connection.ExecuteAsync(sql, l, _transaction);
    }

    private async Task RegistrarPagoAsync(Pago p)
    {
        var sql = @"
            INSERT INTO pago (
                comprobanteId, medioPago, monto, fechaPago,
                numeroOperacion, entidadFinanciera, observaciones
            ) VALUES (
                @ComprobanteId, @MedioPago, @Monto, @FechaPago,
                @NumeroOperacion, @EntidadFinanciera, @Observaciones
            );";

        await _connection.ExecuteAsync(sql, p, _transaction);
    }

    private async Task RegistrarCuotaAsync(Cuota c)
    {
        var sql = @"
            INSERT INTO cuota (
                comprobanteId, numeroCuota, monto, fechaVencimiento,
                montoPagado, fechaPago, estado
            ) VALUES (
                @ComprobanteId, @NumeroCuota, @Monto, @FechaVencimiento,
                @MontoPagado, @FechaPago, @Estado
            );";
        await _connection.ExecuteAsync(sql, c, _transaction);
    }

    private async Task RegistrarGuiaAsync(GuiaComprobante g)
    {
        var sql = @"
            INSERT INTO guiacomprobante (
                comprobanteID, guiaTipoDoc, guiaNumeroCompleto
            ) VALUES (
                @ComprobanteId, @GuiaTipoDoc, @GuiaNumeroCompleto
            );";

        await _connection.ExecuteAsync(sql, g, _transaction);
    }

    private async Task RegistrarDetraccionAsync(Detraccion d)
    {
        var sql = @"
            INSERT INTO detraccion (
                comprobanteID, codigoBienDetraccion, codigoMedioPago,
                cuentaBancoDetraccion, porcentajeDetraccion, montoDetraccion, observacion
            ) VALUES (
                @ComprobanteID, @CodigoBienDetraccion, @CodigoMedioPago,
                @CuentaBancoDetraccion, @PorcentajeDetraccion, @MontoDetraccion, @Observacion
            );";

        await _connection.ExecuteAsync(sql, d, _transaction);
    }

    private static string ConstruirDetallesInsert(ICollection<ComprobanteDetalle> detalles, DynamicParameters parametros, string idExpresion) =>
        ConstruirInsertMasivo(
            "comprobantedetalle",
            [
                "comprobanteId", "trabajadorID", "item", "productoId", "codigo", "descripcion", "cantidad",
                "unidadMedida", "precioUnitario", "tipoAfectacionIGV", "porcentajeIGV",
                "montoIGV", "baseIgv", "codigoTipoDescuento", "descuentoUnitario", "descuentoTotal",
                "valorVenta", "precioVenta", "totalVentaItem", "icbper", "factorIcbper"
            ],
            [.. detalles],
            d =>
            [
                d.TrabajadorID, d.Item, d.ProductoId, d.Codigo, d.Descripcion, d.Cantidad,
                d.UnidadMedida, d.PrecioUnitario, d.TipoAfectacionIGV, d.PorcentajeIGV,
                d.MontoIGV, d.BaseIgv, d.CodigoTipoDescuento, d.DescuentoUnitario, d.DescuentoTotal,
                d.ValorVenta, d.PrecioVenta, d.TotalVentaItem, d.Icbper, d.FactorIcbper
            ],
            parametros, "det", idExpresion);

    private static string ConstruirLeyendasInsert(ICollection<NoteLegend> leyendas, DynamicParameters parametros, string idExpresion) =>
        ConstruirInsertMasivo(
            "notelegend",
            ["comprobanteId", "code", "value"],
            [.. leyendas],
            l => [l.Code, l.Value],
            parametros, "ley", idExpresion);

    private static string ConstruirPagosInsert(ICollection<Pago> pagos, DynamicParameters parametros, string idExpresion) =>
        ConstruirInsertMasivo(
            "pago",
            [
                "comprobanteId", "medioPago", "monto", "fechaPago",
                "numeroOperacion", "entidadFinanciera", "observaciones"
            ],
            [.. pagos],
            p => [p.MedioPago, p.Monto, p.FechaPago, p.NumeroOperacion, p.EntidadFinanciera, p.Observaciones],
            parametros, "pag", idExpresion);

    private static string ConstruirCuotasInsert(ICollection<Cuota> cuotas, DynamicParameters parametros, string idExpresion) =>
        ConstruirInsertMasivo(
            "cuota",
            [
                "comprobanteId", "numeroCuota", "monto", "fechaVencimiento",
                "montoPagado", "fechaPago", "estado"
            ],
            [.. cuotas],
            c => [c.NumeroCuota, c.Monto, c.FechaVencimiento, c.MontoPagado, c.FechaPago, c.Estado],
            parametros, "cuo", idExpresion);

    private static string ConstruirGuiasInsert(ICollection<GuiaComprobante> guias, DynamicParameters parametros, string idExpresion) =>
        ConstruirInsertMasivo(
            "guiacomprobante",
            ["comprobanteID", "guiaTipoDoc", "guiaNumeroCompleto"],
            [.. guias],
            g => [g.GuiaTipoDoc, g.GuiaNumeroCompleto],
            parametros, "gui", idExpresion);

    private static string ConstruirDetraccionesInsert(ICollection<Detraccion> detracciones, DynamicParameters parametros, string idExpresion) =>
        ConstruirInsertMasivo(
            "detraccion",
            [
                "comprobanteID", "codigoBienDetraccion", "codigoMedioPago",
                "cuentaBancoDetraccion", "porcentajeDetraccion", "montoDetraccion", "observacion"
            ],
            [.. detracciones],
            d =>
            [
                d.CodigoBienDetraccion, d.CodigoMedioPago,
                d.CuentaBancoDetraccion, d.PorcentajeDetraccion, d.MontoDetraccion, d.Observacion
            ],
            parametros, "dtr", idExpresion);

    private async Task ActualizarSerieCorrelativoAsync(Comprobante comprobante)
    {
        var sucursalId = await GetSucursalIdByRucAndAnexoAsync(comprobante.EmpresaRuc!, comprobante.EmpresaEstablecimientoAnexo!);
        if (sucursalId == null)
            throw new InvalidOperationException($"No se encontró una sucursal activa para el RUC {comprobante.EmpresaRuc} y establecimiento {comprobante.EmpresaEstablecimientoAnexo}");

        string sql = comprobante.TipoComprobante switch
        {
            "01" => @"UPDATE sucursal SET serieFactura = @Serie, correlativoFactura = correlativoFactura + 1 WHERE sucursalID = @SucursalId",
            "03" => @"UPDATE sucursal SET serieBoleta  = @Serie, correlativoBoleta  = correlativoBoleta  + 1 WHERE sucursalID = @SucursalId",
            _    => throw new InvalidOperationException($"Tipo de comprobante '{comprobante.TipoComprobante}' no soportado para incremento automático.")
        };

        await _connection.ExecuteAsync(sql, new { comprobante.Serie, SucursalId = sucursalId }, _transaction);
    }

    public async Task<AsignacionSerieDTO?> AsignarSerieYCorrelativoAsync(
        string empresaRuc, string codEstablecimiento, string tipoComprobante)
    {
        var (columnaSerie, columnaCorrelativo) = tipoComprobante switch
        {
            "01" => ("serieFactura",   "correlativoFactura"),
            "03" => ("serieBoleta",    "correlativoBoleta"),
            "NV" => ("serieNotaVenta", "correlativoNotaVenta"),
            _    => throw new InvalidOperationException($"Tipo de comprobante '{tipoComprobante}' no soportado.")
        };

        // Las tres sentencias viajan en un unico comando. Antes esto eran cuatro consultas
        // separadas (empresa, sucursalID, fila de sucursal, y el par SELECT FOR UPDATE +
        // UPDATE del correlativo), que contra una BD remota costaban cuatro viajes de red.
        //
        // Quien serializa dos ventas simultaneas de la misma serie es el UPDATE: toma el
        // bloqueo exclusivo de la fila y la segunda transaccion espera ahi hasta el commit
        // de la primera. El SELECT final lee el valor ya incrementado dentro de la propia
        // transaccion, por lo que "-1" es el numero que le corresponde a esta venta.
        // El JOIN con empresa conserva la exigencia de que la empresa este activa.
        var sql = $@"
            SET @sucursalReservada = (
                SELECT s.sucursalID
                FROM sucursal s
                INNER JOIN empresa e ON e.ruc = s.empresaRuc AND e.activo = 1
                WHERE s.empresaRuc = @EmpresaRuc
                  AND s.codEstablecimiento = @CodEstablecimiento
                  AND s.estado = 1
                LIMIT 1);

            UPDATE sucursal
            SET {columnaCorrelativo} = {columnaCorrelativo} + 1
            WHERE sucursalID = @sucursalReservada;

            SELECT sucursalID                 AS SucursalId,
                   {columnaSerie}             AS Serie,
                   {columnaCorrelativo} - 1   AS Correlativo
            FROM sucursal
            WHERE sucursalID = @sucursalReservada;";

        return await _connection.QueryFirstOrDefaultAsync<AsignacionSerieDTO>(
            sql,
            new { EmpresaRuc = empresaRuc, CodEstablecimiento = codEstablecimiento },
            _transaction);
    }

    public async Task<int> ObtenerYIncrementarCorrelativoAsync(int sucursalId, string tipoComprobante, string serie)
    {
        // Bloquea la fila hasta que la transacción haga Commit — ninguna otra transacción puede leer FOR UPDATE hasta entonces
        string sqlSelect = tipoComprobante switch
        {
            "01" => "SELECT correlativoFactura    FROM sucursal WHERE sucursalID = @SucursalId FOR UPDATE",
            "03" => "SELECT correlativoBoleta     FROM sucursal WHERE sucursalID = @SucursalId FOR UPDATE",
            "NV" => "SELECT correlativoNotaVenta  FROM sucursal WHERE sucursalID = @SucursalId FOR UPDATE",
            _    => throw new InvalidOperationException($"Tipo de comprobante '{tipoComprobante}' no soportado.")
        };

        int correlativoActual = await _connection.ExecuteScalarAsync<int>(sqlSelect, new { SucursalId = sucursalId }, _transaction);

        string sqlUpdate = tipoComprobante switch
        {
            "01" => "UPDATE sucursal SET serieFactura   = @Serie, correlativoFactura   = correlativoFactura   + 1 WHERE sucursalID = @SucursalId",
            "03" => "UPDATE sucursal SET serieBoleta    = @Serie, correlativoBoleta    = correlativoBoleta    + 1 WHERE sucursalID = @SucursalId",
            "NV" => "UPDATE sucursal SET serieNotaVenta = @Serie, correlativoNotaVenta = correlativoNotaVenta + 1 WHERE sucursalID = @SucursalId",
            _    => throw new InvalidOperationException($"Tipo de comprobante '{tipoComprobante}' no soportado.")
        };

        await _connection.ExecuteAsync(sqlUpdate, new { Serie = serie, SucursalId = sucursalId }, _transaction);

        return correlativoActual;
    }

    public async Task<IEnumerable<Comprobante>> GetByRucAndFechasAsync(string ruc, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null)
    {
        var sql = BaseSelect + @"
            WHERE empresaRuc = @Ruc
            AND (@FechaDesde IS NULL OR fechaEmision >= @FechaDesde)
            AND (@FechaHasta IS NULL OR fechaEmision <= @FechaHasta)
            ORDER BY fechaEmision DESC"
        + (limit.HasValue ? " LIMIT @Limit" : "")
        + (limit.HasValue && offset.HasValue ? " OFFSET @Offset" : "");

        return await _connection.QueryAsync<Comprobante>(
            sql, new { Ruc = ruc, FechaDesde = fechaDesde, FechaHasta = fechaHasta, Limit = limit, Offset = offset }, _transaction);
    }

    public async Task<IEnumerable<Comprobante>> GetBySucursalAndFechasAsync(string empresaRuc, string codEstablecimiento, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null, int? usuarioId = null)
    {
        var sql = BaseSelect + @"
        WHERE empresaRuc = @EmpresaRuc
        AND establecimientoAnexo = @CodEstablecimiento
        AND (@FechaDesde IS NULL OR fechaEmision >= @FechaDesde)
        AND (@FechaHasta IS NULL OR fechaEmision <= @FechaHasta)
        AND (@UsuarioId IS NULL OR usuarioCreacion = @UsuarioId)
        ORDER BY fechaEmision DESC"
        + (limit.HasValue ? " LIMIT @Limit" : "")
        + (limit.HasValue && offset.HasValue ? " OFFSET @Offset" : "");

        return await _connection.QueryAsync<Comprobante>(
            sql, new { EmpresaRuc = empresaRuc, CodEstablecimiento = codEstablecimiento, FechaDesde = fechaDesde, FechaHasta = fechaHasta, Limit = limit, Offset = offset, UsuarioId = usuarioId }, _transaction);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetCantidadItemsPorComprobantesAsync(IEnumerable<int> comprobanteIds)
    {
        var ids = comprobanteIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<int, int>();

        var sql = @"
            SELECT comprobanteId AS ComprobanteId, COUNT(*) AS Cantidad
            FROM comprobantedetalle
            WHERE comprobanteId IN @Ids
            GROUP BY comprobanteId;";

        var filas = await _connection.QueryAsync<(int ComprobanteId, int Cantidad)>(sql, new { Ids = ids }, _transaction);
        return filas.ToDictionary(f => f.ComprobanteId, f => f.Cantidad);
    }

    public async Task<IEnumerable<Comprobante>> GetByDocClienteAndFechasAsync(string rucEmpresa, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null)
    {
        var sql = BaseSelect + @"
        WHERE empresaRuc = @RucEmpresa
        AND clienteNumDoc = @ClienteNumDoc
        AND (@FechaDesde IS NULL OR fechaEmision >= @FechaDesde)
        AND (@FechaHasta IS NULL OR fechaEmision <= @FechaHasta)
        ORDER BY fechaEmision DESC"
        + (limit.HasValue ? " LIMIT @Limit" : "")
        + (limit.HasValue && offset.HasValue ? " OFFSET @Offset" : "");

        return await _connection.QueryAsync<Comprobante>(
            sql, new { RucEmpresa = rucEmpresa, ClienteNumDoc = clienteNumDoc, FechaDesde = fechaDesde, FechaHasta = fechaHasta, Limit = limit, Offset = offset }, _transaction);
    }

    public async Task<IEnumerable<Comprobante>> GetByDocUsuarioAndFechasAsync(string rucEmpresa, int usuarioCreacion, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null)
    {
        var sql = BaseSelect + @"
        WHERE empresaRuc = @RucEmpresa
        AND usuarioCreacion = @UsuarioCreacion
        AND (@FechaDesde IS NULL OR fechaEmision >= @FechaDesde)
        AND (@FechaHasta IS NULL OR fechaEmision <= @FechaHasta)
        ORDER BY fechaEmision DESC"
        + (limit.HasValue ? " LIMIT @Limit" : "")
        + (limit.HasValue && offset.HasValue ? " OFFSET @Offset" : "");

        return await _connection.QueryAsync<Comprobante>(
            sql, new { RucEmpresa = rucEmpresa, UsuarioCreacion = usuarioCreacion, FechaDesde = fechaDesde, FechaHasta = fechaHasta, Limit = limit, Offset = offset }, _transaction);
    }

    public async Task<IEnumerable<Comprobante>> GetByClienteAndSucursalAsync(string empresaRuc, string codEstablecimiento, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null)
    {
        var sql = BaseSelect + @"
        WHERE empresaRuc = @EmpresaRuc
        AND establecimientoAnexo = @CodEstablecimiento
        AND clienteNumDoc = @ClienteNumDoc
        AND (@FechaDesde IS NULL OR fechaEmision >= @FechaDesde)
        AND (@FechaHasta IS NULL OR fechaEmision <= @FechaHasta)
        ORDER BY fechaEmision DESC"
        + (limit.HasValue ? " LIMIT @Limit" : "")
        + (limit.HasValue && offset.HasValue ? " OFFSET @Offset" : "");

        return await _connection.QueryAsync<Comprobante>(
            sql,
            new { EmpresaRuc = empresaRuc, CodEstablecimiento = codEstablecimiento, ClienteNumDoc = clienteNumDoc, FechaDesde = fechaDesde, FechaHasta = fechaHasta, Limit = limit, Offset = offset },
            _transaction);
    }

    public async Task UpdateCorreoWhatsappAsync(int comprobanteId, string? correo, bool? enviadoPorCorreo, string? whatsApp, bool? enviadoPorWhatsApp)
    {
        var sql = @"
            UPDATE comprobante SET
                clienteCorreo        = @Correo,
                enviadoPorCorreo     = @EnviadoPorCorreo,
                clienteWhatsApp      = @WhatsApp,
                enviadoPorWhatsApp   = @EnviadoPorWhatsApp
            WHERE comprobanteID = @ComprobanteId";

        await _connection.ExecuteAsync(sql, new
        {
            ComprobanteId = comprobanteId,
            Correo = correo,
            EnviadoPorCorreo = enviadoPorCorreo,
            WhatsApp = whatsApp,
            EnviadoPorWhatsApp = enviadoPorWhatsApp
        }, _transaction);
    }

    public async Task<int> GetCantidadByClienteNumDocAsync(string clienteNumDoc)
    {
        var sql = @"
        SELECT COUNT(*)
        FROM comprobante
        WHERE clienteNumDoc = @ClienteNumDoc";

        return await _connection.ExecuteScalarAsync<int>(
            sql,
            new { ClienteNumDoc = clienteNumDoc },
            _transaction);
    }

    // ──Obtener comprobante por ID ────────────────────────────────────
    public new async Task<Comprobante?> GetByIdAsync(int comprobanteId)
    {
        var sql = BaseSelect + " WHERE comprobanteID = @ComprobanteId";

        return await _connection.QueryFirstOrDefaultAsync<Comprobante>(
            sql,
            new { ComprobanteId = comprobanteId },
            _transaction
        );
    }
    public async Task<IEnumerable<Comprobante>> GetByEstadoAsync(string estadoSunat)
    {
        var sql = BaseSelect + " WHERE estadoSunat = @EstadoSunat";

        return await _connection.QueryAsync<Comprobante>(
            sql,
            new { EstadoSunat = estadoSunat },
            _transaction
        );
    }

    public async Task<IEnumerable<ComprobanteDetalle>> GetDetallesByIdAsync(int comprobanteId)
    {
        var sql = @"
            SELECT 
                comprobanteId, trabajadorID, item, productoId, codigo, descripcion, cantidad,
                unidadMedida, precioUnitario, tipoAfectacionIGV, porcentajeIGV,
                montoIGV, baseIgv, codigoTipoDescuento, descuentoUnitario, descuentoTotal,
                valorVenta, precioVenta, totalVentaItem, icbper, factorIcbper
            FROM comprobantedetalle
            WHERE comprobanteId = @ComprobanteId";

        return await _connection.QueryAsync<ComprobanteDetalle>(
            sql, new { ComprobanteId = comprobanteId }, _transaction);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetItemDetalleIdMapAsync(int comprobanteId)
    {
        var sql = @"
            SELECT item AS Item, detalleID AS DetalleId
            FROM comprobantedetalle
            WHERE comprobanteId = @ComprobanteId";

        var filas = await _connection.QueryAsync<(int Item, int DetalleId)>(
            sql, new { ComprobanteId = comprobanteId }, _transaction);

        return filas.ToDictionary(f => f.Item, f => f.DetalleId);
    }

    public async Task<IEnumerable<Cuota>> GetCuotasByIdAsync(int comprobanteId)
    {
        var sql = @"
            SELECT 
                cuotaId, comprobanteId, numeroCuota, monto,
                fechaVencimiento, montoPagado, fechaPago, estado
            FROM cuota
            WHERE comprobanteId = @ComprobanteId";

        return await _connection.QueryAsync<Cuota>(
            sql, new { ComprobanteId = comprobanteId }, _transaction);
    }

    public async Task<IEnumerable<Pago>> GetPagosByIdAsync(int comprobanteId)
    {
        var sql = @"
            SELECT 
                pagoID         AS PagoId,
                comprobanteID  AS ComprobanteId,
                medioPago      AS MedioPago,
                monto          AS Monto,
                fechaPago      AS FechaPago,
                numeroOperacion AS NumeroOperacion,
                entidadFinanciera AS EntidadFinanciera,
                observaciones  AS Observaciones
            FROM pago
            WHERE comprobanteID = @ComprobanteId";

        return await _connection.QueryAsync<Pago>(
            sql,
            new { ComprobanteId = comprobanteId },
            _transaction
        );
    }
    public async Task<IEnumerable<NoteLegend>> GetLeyendasByIdAsync(int comprobanteId)
    {
        var sql = @"
            SELECT comprobanteId, code, value
            FROM notelegend
            WHERE comprobanteId = @ComprobanteId";

        return await _connection.QueryAsync<NoteLegend>(
            sql, new { ComprobanteId = comprobanteId }, _transaction);
    }

    public async Task<IEnumerable<GuiaComprobante>> GetGuiasByIdAsync(int comprobanteId)
    {
        var sql = @"
            SELECT 
                guiaComprobanteID  AS GuiaComprobanteId,
                comprobanteID      AS ComprobanteId,
                guiaTipoDoc        AS GuiaTipoDoc,
                guiaNumeroCompleto AS GuiaNumeroCompleto
            FROM guiacomprobante
            WHERE comprobanteID = @ComprobanteId";

        return await _connection.QueryAsync<GuiaComprobante>(
            sql, new { ComprobanteId = comprobanteId }, _transaction);
    }

    public async Task<IEnumerable<Detraccion>> GetDetraccionesByIdAsync(int comprobanteId)
    {
        var sql = @"
            SELECT 
                detraccionID          AS DetraccionID,
                comprobanteID         AS ComprobanteID,
                codigoBienDetraccion  AS CodigoBienDetraccion,
                codigoMedioPago       AS CodigoMedioPago,
                cuentaBancoDetraccion AS CuentaBancoDetraccion,
                porcentajeDetraccion  AS PorcentajeDetraccion,
                montoDetraccion       AS MontoDetraccion,
                observacion           AS Observacion
            FROM detraccion
            WHERE comprobanteID = @ComprobanteId";

        return await _connection.QueryAsync<Detraccion>(
            sql, new { ComprobanteId = comprobanteId }, _transaction);
    }

    // ── NUEVO: Obtener Datos Completos en un solo viaje (Optimización) ───────
    public async Task<(
        IEnumerable<ComprobanteDetalle> Detalles,
        IEnumerable<Pago> Pagos,
        IEnumerable<Cuota> Cuotas,
        IEnumerable<NoteLegend> Leyendas,
        IEnumerable<GuiaComprobante> Guias,
        IEnumerable<Detraccion> Detracciones
    )> GetDatosCompletosByComprobanteIdAsync(int comprobanteId)
    {
        var sql = @"
            SELECT cd.comprobanteId, cd.trabajadorID, cd.item, cd.productoId, cd.codigo, cd.descripcion, cd.cantidad,
                   cd.unidadMedida, cd.precioUnitario, cd.tipoAfectacionIGV, cd.porcentajeIGV,
                   cd.montoIGV, cd.baseIgv, cd.codigoTipoDescuento, cd.descuentoUnitario, cd.descuentoTotal,
                   cd.valorVenta, cd.precioVenta, cd.totalVentaItem, cd.icbper, cd.factorIcbper,
                   CONCAT(COALESCE(t.nombres, ''), ' ', COALESCE(t.apellidos, '')) AS NombreTrabajador,
                   p.codigoSunat AS CodigoSunat
            FROM comprobantedetalle cd
            LEFT JOIN trabajador t ON t.id = cd.trabajadorID
            LEFT JOIN producto p ON p.productoId = cd.productoId
            WHERE cd.comprobanteId = @Id;

            SELECT pagoID AS PagoId, comprobanteID AS ComprobanteId, medioPago AS MedioPago,
                   monto AS Monto, fechaPago AS FechaPago, numeroOperacion AS NumeroOperacion,
                   entidadFinanciera AS EntidadFinanciera, observaciones AS Observaciones
            FROM pago WHERE comprobanteID = @Id;

            SELECT cuotaId, comprobanteId, numeroCuota, monto, fechaVencimiento,
                   montoPagado, fechaPago, estado
            FROM cuota WHERE comprobanteId = @Id;

            SELECT comprobanteId, code, value
            FROM notelegend WHERE comprobanteId = @Id;

            SELECT guiaComprobanteID AS GuiaComprobanteId, comprobanteID AS ComprobanteId,
                   guiaTipoDoc AS GuiaTipoDoc, guiaNumeroCompleto AS GuiaNumeroCompleto
            FROM guiacomprobante WHERE comprobanteID = @Id;

            SELECT detraccionID AS DetraccionID, comprobanteID AS ComprobanteID,
                   codigoBienDetraccion AS CodigoBienDetraccion, codigoMedioPago AS CodigoMedioPago,
                   cuentaBancoDetraccion AS CuentaBancoDetraccion, porcentajeDetraccion AS PorcentajeDetraccion,
                   montoDetraccion AS MontoDetraccion, observacion AS Observacion
            FROM detraccion WHERE comprobanteID = @Id;
        ";

        using var multi = await _connection.QueryMultipleAsync(sql, new { Id = comprobanteId }, _transaction);

        var detalles = await multi.ReadAsync<ComprobanteDetalle>();
        var pagos = await multi.ReadAsync<Pago>();
        var cuotas = await multi.ReadAsync<Cuota>();
        var leyendas = await multi.ReadAsync<NoteLegend>();
        var guias = await multi.ReadAsync<GuiaComprobante>();
        var detracciones = await multi.ReadAsync<Detraccion>();

        return (detalles, pagos, cuotas, leyendas, guias, detracciones);
    }

    // ── NUEVO: Actualizar estado SUNAT ───────────────────────────────────────
    public async Task UpdateEstadoSunatAsync(
        int comprobanteId,
        string estado,
        string? codigo,
        string? mensaje,
        string? xmlFirmado,
        string? cdrBase64,
        string? hash = null,
        string? mensajeAdicional = null)
    {
        var sql = @"
            UPDATE comprobante SET
                estadoSunat           = @Estado,
                codigoRespuestaSunat  = @Codigo,
                mensajeRespuestaSunat = CASE 
                    WHEN @MensajeAdicional IS NOT NULL 
                    THEN CONCAT(COALESCE(mensajeRespuestaSunat, ''), '. ', @MensajeAdicional)
                    ELSE @Mensaje
                END,
                codigoHashCPE         = COALESCE(@Hash, codigoHashCPE),
                fechaEnvioSunat       = @FechaEnvio
            WHERE comprobanteID = @ComprobanteId";

        await _connection.ExecuteAsync(sql, new
        {
            ComprobanteId    = comprobanteId,
            Estado           = estado,
            Codigo           = codigo,
            Mensaje          = mensaje,
            Hash             = hash,
            MensajeAdicional = mensajeAdicional,
            FechaEnvio       = AhoraLima()
        }, _transaction);
    }

    // ── NUEVO: Anular comprobante (Nota de Venta) ────────────────────────────
    public async Task AnularComprobanteAsync(int comprobanteId, string? motivo, int? usuarioId)
    {
        var sql = @"
            UPDATE comprobante SET
                estadoSunat           = 'ANULADO',
                mensajeRespuestaSunat = @Motivo,
                usuarioModificacion   = @UsuarioId,
                fechaModificacion     = @Fecha
            WHERE comprobanteID = @ComprobanteId";

        await _connection.ExecuteAsync(sql, new
        {
            ComprobanteId = comprobanteId,
            Motivo        = motivo,
            UsuarioId     = usuarioId,
            Fecha         = AhoraLima()
        }, _transaction);
    }

    public Task<Comprobante?> GetComprobanteByIdAsync(int comprobanteId)
    {
        return GetByIdAsync(comprobanteId);
    }

    public Task<IEnumerable<Comprobante>> GetComprobanteByEstadoAsync(string estado)
    {
        return GetByEstadoAsync(estado);
    }

    public async Task<Comprobante?> GetByRucSerieNumeroAsync(string ruc, string serie, int numero)
    {
        var sql = BaseSelect + @"
        WHERE empresaRuc  = @Ruc
          AND serie       = @Serie
          AND correlativo = @Numero
          AND estadoSunat = 'ACEPTADO'";

        return await _connection.QueryFirstOrDefaultAsync<Comprobante>(
            sql,
            new { Ruc = ruc, Serie = serie, Numero = numero },
            _transaction
        );
    }

    public async Task<Comprobante?> GetByComprobanteUnicoAsync(string ruc, string serie, int numero)
    {
        var sql = BaseSelect + @"
        WHERE empresaRuc  = @Ruc
        AND serie       = @Serie
        AND correlativo = @Numero";

        return await _connection.QueryFirstOrDefaultAsync<Comprobante>(
            sql,
            new { Ruc = ruc, Serie = serie, Numero = numero },
            _transaction
        );
    }

    public async Task<int?> GetSucursalIdByRucAndAnexoAsync(string empresaRuc, string codEstablecimiento)
    {
        var sql = @"
        SELECT sucursalID 
        FROM sucursal 
        WHERE empresaRuc = @EmpresaRuc 
          AND codEstablecimiento = @CodEstablecimiento
          AND estado = 1
        LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<int?>(sql, new
        {
            EmpresaRuc = empresaRuc,
            CodEstablecimiento = codEstablecimiento
        }, _transaction);
    }

    public async Task UpdateXmlGeneradoAsync(int comprobanteId, string rutaZip)
    {
        var sql = @"
        UPDATE comprobante SET
            xmlGenerado = @RutaZip
        WHERE comprobanteID = @ComprobanteId";

        await _connection.ExecuteAsync(sql, new { ComprobanteId = comprobanteId, RutaZip = rutaZip }, _transaction);
    }

    public async Task UpdateXmlRespuestaSunatAsync(int comprobanteId, string rutaCdr)
    {
        var sql = @"
        UPDATE comprobante SET
            xmlRespuestaSunat = @RutaCdr
        WHERE comprobanteID = @ComprobanteId";

        await _connection.ExecuteAsync(sql, new { ComprobanteId = comprobanteId, RutaCdr = rutaCdr }, _transaction);
    }

    public async Task InsertValesAsync(int comprobanteId, IEnumerable<int> valeIds)
    {
        var sql = @"
            INSERT IGNORE INTO comprobantevale (comprobanteId, valeId)
            VALUES (@ComprobanteId, @ValeId);";

        var batch = valeIds.Select(valeId => new { ComprobanteId = comprobanteId, ValeId = valeId });
        await _connection.ExecuteAsync(sql, batch, _transaction);
    }

    public async Task<IEnumerable<int>> GetValesByComprobanteIdAsync(int comprobanteId)
    {
        var sql = "SELECT valeId FROM comprobantevale WHERE comprobanteId = @ComprobanteId";
        return await _connection.QueryAsync<int>(sql, new { ComprobanteId = comprobanteId }, _transaction);
    }

    public async Task<IEnumerable<Vale>> GetValesFullByComprobanteIdAsync(int comprobanteId)
    {
        var sql = @"
            SELECT v.idvale       AS IdVale,
                   v.nombre       AS Nombre,
                   v.descripcion  AS Descripcion,
                   v.fechaemision AS FechaEmision,
                   v.duracion     AS Duracion,
                   v.estado       AS Estado
            FROM vale v
            INNER JOIN comprobantevale cv ON cv.valeId = v.idvale
            WHERE cv.comprobanteId = @ComprobanteId";
        return await _connection.QueryAsync<Vale>(sql, new { ComprobanteId = comprobanteId }, _transaction);
    }

    public async Task<bool> UpdateOrdenServicioSpotAsync(
        string ruc, string serie, int correlativo, string? ordenServicio, bool? spot,
        string? spotLeyenda = null, string? spotBienServicio = null, string? spotMedioPago = null,
        string? spotCuentaBanco = null, decimal? spotPorcentaje = null)
    {
        var setClauses = new List<string>();
        var parameters = new Dapper.DynamicParameters();

        if (ordenServicio is not null)
        {
            setClauses.Add("ordenservicio = @OrdenServicio");
            parameters.Add("OrdenServicio", ordenServicio);
        }

        if (spot is not null)
        {
            setClauses.Add("spot = @Spot");
            parameters.Add("Spot", spot);
        }

        if (spotLeyenda is not null)
        {
            setClauses.Add("spotleyenda = @SpotLeyenda");
            parameters.Add("SpotLeyenda", spotLeyenda);
        }

        if (spotBienServicio is not null)
        {
            setClauses.Add("spotbienservicio = @SpotBienServicio");
            parameters.Add("SpotBienServicio", spotBienServicio);
        }

        if (spotMedioPago is not null)
        {
            setClauses.Add("spotmediopago = @SpotMedioPago");
            parameters.Add("SpotMedioPago", spotMedioPago);
        }

        if (spotCuentaBanco is not null)
        {
            setClauses.Add("spotcuentabanco = @SpotCuentaBanco");
            parameters.Add("SpotCuentaBanco", spotCuentaBanco);
        }

        if (spotPorcentaje is not null)
        {
            setClauses.Add("spotporcentaje = @SpotPorcentaje");
            parameters.Add("SpotPorcentaje", spotPorcentaje);
        }

        if (setClauses.Count == 0)
            return false;

        parameters.Add("Ruc",         ruc);
        parameters.Add("Serie",       serie);
        parameters.Add("Correlativo", correlativo);

        var sql = $@"
            UPDATE comprobante
            SET {string.Join(", ", setClauses)}
            WHERE empresaRuc  = @Ruc
              AND serie       = @Serie
              AND correlativo = @Correlativo;";

        var result = await _connection.ExecuteAsync(sql, parameters, _transaction);
        return result > 0;
    }

    private const string BaseSelect = @"
    SELECT
        comprobante.comprobanteID           AS ComprobanteId,
        comprobante.tipoOperacion           AS TipoOperacion,
        comprobante.tipoComprobante         AS TipoComprobante,
        comprobante.serie                   AS Serie,
        comprobante.correlativo             AS Correlativo,
        comprobante.numeroCompleto          AS NumeroCompleto,
        comprobante.tipoCambio              AS TipoCambio,
        comprobante.fechaEmision            AS FechaEmision,
        TIMESTAMP(comprobante.fechaEmision, comprobante.horaEmision) AS HoraEmision,
        comprobante.fechaVencimiento        AS FechaVencimiento,
        comprobante.tipoMoneda              AS TipoMoneda,
        comprobante.tipoPago                AS TipoPago,
        comprobante.ordenservicio           AS OrdenServicio,
        comprobante.spot                    AS Spot,
        comprobante.spotleyenda             AS SpotLeyenda,
        comprobante.spotbienservicio        AS SpotBienServicio,
        comprobante.spotmediopago           AS SpotMedioPago,
        comprobante.spotcuentabanco         AS SpotCuentaBanco,
        comprobante.spotporcentaje          AS SpotPorcentaje,
        comprobante.empresaID               AS EmpresaId,
        comprobante.empresaRuc              AS EmpresaRuc,
        comprobante.empresaRazonSocial      AS EmpresaRazonSocial,
        comprobante.empresaNombreComercial  AS EmpresaNombreComercial,
        comprobante.establecimientoAnexo    AS EmpresaEstablecimientoAnexo,
        comprobante.empresaDireccion        AS EmpresaDireccion,
        comprobante.empresaProvincia        AS EmpresaProvincia,
        comprobante.empresaDepartamento     AS EmpresaDepartamento,
        comprobante.empresaDistrito         AS EmpresaDistrito,
        comprobante.empresaUbigeo           AS EmpresaUbigeo,
        comprobante.clienteID               AS ClienteId,
        comprobante.clienteTipoDoc          AS ClienteTipoDoc,
        comprobante.clienteNumDoc           AS ClienteNumDoc,
        comprobante.clienteRznSocial        AS ClienteRazonSocial,
        comprobante.clienteDireccion        AS ClienteDireccion,
        comprobante.clienteProvincia        AS ClienteProvincia,
        comprobante.clienteDepartamento     AS ClienteDepartamento,
        comprobante.clienteDistrito         AS ClienteDistrito,
        comprobante.clienteUbigeo           AS ClienteUbigeo,
        comprobante.clienteCorreo           AS ClienteCorreo,
        comprobante.enviadoPorCorreo        AS EnviadoPorCorreo,
        comprobante.clienteWhatsApp         AS ClienteWhatsApp,
        comprobante.enviadoPorWhatsApp      AS EnviadoPorWhatsApp,
        comprobante.codigoTipoDescGlobal    AS CodigoTipoDescGlobal,
        comprobante.descuentoGlobal         AS DescuentoGlobal,
        comprobante.totalOperacionesGravadas   AS TotalOperacionesGravadas,
        comprobante.totalOperacionesExoneradas AS TotalOperacionesExoneradas,
        comprobante.totalOperacionesInafectas  AS TotalOperacionesInafectas,
        comprobante.totalOperacionesGratuitas  AS TotalOperacionesGratuitas,
        comprobante.totalIgvGratuitas       AS TotalIgvGratuitas,
        comprobante.totalIGV                AS TotalIGV,
        comprobante.totalImpuestos          AS TotalImpuestos,
        comprobante.totalDescuentos         AS TotalDescuentos,
        comprobante.totalOtrosCargos        AS TotalOtrosCargos,
        comprobante.totalIcbper             AS TotalIcbper,
        comprobante.valorVenta              AS ValorVenta,
        comprobante.subTotal                AS SubTotal,
        comprobante.importeTotal            AS ImporteTotal,
        comprobante.montoCredito                AS MontoCredito,
        comprobante.totalComisionPagoTarjeta    AS TotalComisionPagoTarjeta,
        comprobante.tipDocAfectado              AS TipDocAfectado,
        comprobante.numDocAfectado          AS NumDocAfectado,
        comprobante.tipoNotaCreditoDebito   AS TipoNotaCreditoDebito,
        comprobante.motivoNota              AS MotivoNota,
        comprobante.comprobanteAfectadoID   AS ComprobanteAfectadoId,
        comprobante.observaciones           AS Observaciones,
        comprobante.estadoSunat             AS EstadoSunat,
        comprobante.pdfGenerado             AS PdfGenerado,
        comprobante.enviadoEnResumen        AS EnviadoEnResumen,
        comprobante.codigoHashCPE           AS CodigoHashCPE,
        comprobante.codigoRespuestaSunat    AS CodigoRespuestaSunat,
        comprobante.mensajeRespuestaSunat   AS MensajeRespuestaSunat,
        comprobante.fechaEnvioSunat         AS FechaEnvioSunat,
        comprobante.xmlGenerado             AS XmlGenerado,
        comprobante.usuarioCreacion         AS UsuarioCreacion,
        comprobante.fechaCreacion           AS FechaCreacion,
        comprobante.usuarioModificacion     AS UsuarioModificacion,
        comprobante.fechaModificacion       AS FechaModificacion,
        comprobante.xmlRespuestaSunat       AS XmlRespuestaSunat,
        u.username                          AS NombreCajero
    FROM comprobante
    LEFT JOIN usuario u ON u.usuarioID = comprobante.usuarioCreacion
    ";

    // Siempre devuelve la hora actual en zona horaria Lima (UTC-5), sin importar
    // dónde esté desplegado el servidor (DigitalOcean usa UTC por defecto).
    private static DateTime AhoraLima()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Lima");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
    }

    public async Task<IEnumerable<Domain.Entities.Comprobante>> GetNotasByComprobanteAfectadoIdAsync(
        int comprobanteAfectadoId, string tipoComprobante)
    {
        var sql = @"
            SELECT
                comprobanteID           AS ComprobanteId,
                tipoComprobante         AS TipoComprobante,
                tipoNotaCreditoDebito   AS TipoNotaCreditoDebito,
                importeTotal            AS ImporteTotal,
                estadoSunat             AS EstadoSunat
            FROM comprobante
            WHERE comprobanteAfectadoID = @ComprobanteAfectadoId
            AND tipoComprobante = @TipoComprobante";

        return await _connection.QueryAsync<Domain.Entities.Comprobante>(sql, new
        {
            ComprobanteAfectadoId = comprobanteAfectadoId,
            TipoComprobante       = tipoComprobante
        }, _transaction);
    }

    public async Task<IEnumerable<Comprobante>> GetNotasVentaBySucursalAsync(
        string empresaRuc, string codEstablecimiento,
        DateTime? fechaDesde, DateTime? fechaHasta,
        int? limit = null, int? offset = null)
    {
        var sql = BaseSelect + @"
            WHERE empresaRuc            = @EmpresaRuc
            AND establecimientoAnexo    = @CodEstablecimiento
            AND tipoComprobante         = 'NV'
            AND (@FechaDesde IS NULL OR fechaEmision >= @FechaDesde)
            AND (@FechaHasta IS NULL OR fechaEmision <= @FechaHasta)
            ORDER BY fechaEmision DESC"
        + (limit.HasValue ? " LIMIT @Limit" : "")
        + (limit.HasValue && offset.HasValue ? " OFFSET @Offset" : "");

        return await _connection.QueryAsync<Comprobante>(sql, new
        {
            EmpresaRuc          = empresaRuc,
            CodEstablecimiento  = codEstablecimiento,
            FechaDesde          = fechaDesde,
            FechaHasta          = fechaHasta,
            Limit               = limit,
            Offset              = offset
        }, _transaction);
    }

}