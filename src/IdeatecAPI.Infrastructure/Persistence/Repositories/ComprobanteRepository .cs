using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
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
                totalIcbper, totalImpuestos, valorVenta, subTotal, importeTotal, montoCredito,
                estadoSunat, enviadoEnResumen, xmlGenerado, usuarioCreacion, fechaCreacion
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
                @TotalIcbper, @TotalImpuestos, @ValorVenta, @SubTotal, @ImporteTotal, @MontoCredito,
                @EstadoSunat,  @EnviadoEnResumen, @XmlGenerado, @UsuarioCreacion, @FechaCreacion
            );
            SELECT LAST_INSERT_ID();";

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
            comprobante.EstadoSunat,
            comprobante.XmlGenerado,
            comprobante.EnviadoEnResumen,
            comprobante.UsuarioCreacion,
            comprobante.FechaCreacion
        };

        int comprobanteId = await _connection.ExecuteScalarAsync<int>(sql, parameters, _transaction);

        foreach (var detalle in comprobante.Detalles)
        {
            detalle.ComprobanteId = comprobanteId;
            await RegistrarDetalleAsync(detalle);
        }

        foreach (var leyenda in comprobante.Leyendas)
        {
            leyenda.ComprobanteId = comprobanteId;
            await RegistrarLeyendaAsync(leyenda);
        }

        foreach (var pago in comprobante.Pagos)
        {
            pago.ComprobanteId = comprobanteId;
            await RegistrarPagoAsync(pago);
        }

        foreach (var cuota in comprobante.Cuotas)
        {
            cuota.ComprobanteId = comprobanteId;
            await RegistrarCuotaAsync(cuota);
        }

        foreach (var guia in comprobante.Guias)
        {
            guia.ComprobanteId = comprobanteId;
            await RegistrarGuiaAsync(guia);
        }

        foreach (var detraccion in comprobante.Detracciones)
        {
            detraccion.ComprobanteID = comprobanteId;
            await RegistrarDetraccionAsync(detraccion);
        }

        await ActualizarSerieCorrelativoAsync(comprobante);

        return comprobanteId;
    }

    private async Task RegistrarDetalleAsync(ComprobanteDetalle d)
    {
        var sql = @"
            INSERT INTO comprobantedetalle (
                comprobanteId, item, productoId, codigo, descripcion, cantidad,
                unidadMedida, precioUnitario, tipoAfectacionIGV, porcentajeIGV,
                montoIGV, baseIgv, codigoTipoDescuento, descuentoUnitario, descuentoTotal,
                valorVenta, precioVenta, totalVentaItem, icbper, factorIcbper
            ) VALUES (
                @ComprobanteId, @Item, @ProductoId, @Codigo, @Descripcion, @Cantidad,
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

    private async Task ActualizarSerieCorrelativoAsync(Comprobante comprobante)
    {
        string sql = comprobante.TipoComprobante switch
        {
            "01" => @"UPDATE sucursal SET 
                        serieFactura       = @Serie,
                        correlativoFactura = correlativoFactura + 1
                    WHERE empresaRuc           = @EmpresaRuc 
                    AND codEstablecimiento     = @EmpresaEstablecimientoAnexo
                    AND estado                = 1",

            "03" => @"UPDATE sucursal SET 
                        serieBoleta       = @Serie,
                        correlativoBoleta = correlativoBoleta + 1
                    WHERE empresaRuc           = @EmpresaRuc 
                    AND codEstablecimiento     = @EmpresaEstablecimientoAnexo
                    AND estado                = 1",

            _ => throw new InvalidOperationException($"Tipo de comprobante '{comprobante.TipoComprobante}' no soportado.")
        };

        var parameters = new
        {
            comprobante.Serie,
            comprobante.EmpresaRuc,
            comprobante.EmpresaEstablecimientoAnexo
        };

        await _connection.ExecuteAsync(sql, parameters, _transaction);
    }

    public async Task<IEnumerable<Comprobante>> GetByRucAndFechasAsync(string ruc, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null)
    {
        var sql = BaseSelect + @"
    WHERE empresaRuc = @Ruc
    AND (@FechaDesde IS NULL OR fechaEmision >= @FechaDesde)
    AND (@FechaHasta IS NULL OR fechaEmision <= @FechaHasta)
    ORDER BY fechaEmision DESC"
        + (limit.HasValue ? " LIMIT @Limit" : "");

        return await _connection.QueryAsync<Comprobante>(
            sql, new { Ruc = ruc, FechaDesde = fechaDesde, FechaHasta = fechaHasta, Limit = limit }, _transaction);
    }

    public async Task<IEnumerable<Comprobante>> GetBySucursalAndFechasAsync(string empresaRuc, string codEstablecimiento, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null)
    {
        var sql = BaseSelect + @"
        WHERE empresaRuc = @EmpresaRuc
        AND establecimientoAnexo = @CodEstablecimiento
        AND (@FechaDesde IS NULL OR fechaEmision >= @FechaDesde)
        AND (@FechaHasta IS NULL OR fechaEmision <= @FechaHasta)
        ORDER BY fechaEmision DESC"
        + (limit.HasValue ? " LIMIT @Limit" : "");

        return await _connection.QueryAsync<Comprobante>(
            sql, new { EmpresaRuc = empresaRuc, CodEstablecimiento = codEstablecimiento, FechaDesde = fechaDesde, FechaHasta = fechaHasta, Limit = limit }, _transaction);
    }

    public async Task<IEnumerable<Comprobante>> GetByDocClienteAndFechasAsync(string rucEmpresa, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var sql = BaseSelect + @"
        WHERE empresaRuc = @RucEmpresa
        AND clienteNumDoc = @ClienteNumDoc
        AND (@FechaDesde IS NULL OR fechaEmision >= @FechaDesde)
        AND (@FechaHasta IS NULL OR fechaEmision <= @FechaHasta)
        ORDER BY fechaEmision DESC";

        return await _connection.QueryAsync<Comprobante>(
            sql, new { RucEmpresa = rucEmpresa, ClienteNumDoc = clienteNumDoc, FechaDesde = fechaDesde, FechaHasta = fechaHasta }, _transaction);
    }

    public async Task<IEnumerable<Comprobante>> GetByDocUsuarioAndFechasAsync(string rucEmpresa, int usuarioCreacion, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var sql = BaseSelect + @"
        WHERE empresaRuc = @RucEmpresa
        AND usuarioCreacion = @UsuarioCreacion
        AND (@FechaDesde IS NULL OR fechaEmision >= @FechaDesde)
        AND (@FechaHasta IS NULL OR fechaEmision <= @FechaHasta)
        ORDER BY fechaEmision DESC";

        return await _connection.QueryAsync<Comprobante>(
            sql, new { RucEmpresa = rucEmpresa, UsuarioCreacion = usuarioCreacion, FechaDesde = fechaDesde, FechaHasta = fechaHasta }, _transaction);
    }

    public async Task<IEnumerable<Comprobante>> GetByClienteAndSucursalAsync(string empresaRuc, string codEstablecimiento, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var sql = BaseSelect + @"
        WHERE empresaRuc = @EmpresaRuc
        AND establecimientoAnexo = @CodEstablecimiento
        AND clienteNumDoc = @ClienteNumDoc
        AND (@FechaDesde IS NULL OR fechaEmision >= @FechaDesde)
        AND (@FechaHasta IS NULL OR fechaEmision <= @FechaHasta)
        ORDER BY fechaEmision DESC";

        return await _connection.QueryAsync<Comprobante>(
            sql,
            new { EmpresaRuc = empresaRuc, CodEstablecimiento = codEstablecimiento, ClienteNumDoc = clienteNumDoc, FechaDesde = fechaDesde, FechaHasta = fechaHasta },
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

    // ── NUEVO: Obtener comprobante por ID ────────────────────────────────────
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
                comprobanteId, item, productoId, codigo, descripcion, cantidad,
                unidadMedida, precioUnitario, tipoAfectacionIGV, porcentajeIGV,
                montoIGV, baseIgv, codigoTipoDescuento, descuentoUnitario, descuentoTotal,
                valorVenta, precioVenta, totalVentaItem, icbper, factorIcbper
            FROM comprobantedetalle
            WHERE comprobanteId = @ComprobanteId";

        return await _connection.QueryAsync<ComprobanteDetalle>(
            sql, new { ComprobanteId = comprobanteId }, _transaction);
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

    // ── NUEVO: Actualizar estado SUNAT ───────────────────────────────────────
    public async Task UpdateEstadoSunatAsync(
        int comprobanteId,
        string estado,
        string? codigo,
        string? mensaje,
        string? xmlFirmado,
        string? cdrBase64)
    {
        var sql = @"
            UPDATE comprobante SET
                estadoSunat           = @Estado,
                codigoRespuestaSunat  = @Codigo,
                mensajeRespuestaSunat = @Mensaje,
                xmlRespuestaSunat     = @XmlFirmado,
                cdrSunat              = @CdrBase64,
                fechaEnvioSunat       = @FechaEnvio
            WHERE comprobanteID = @ComprobanteId";

        await _connection.ExecuteAsync(sql, new
        {
            ComprobanteId = comprobanteId,
            Estado = estado,
            Codigo = codigo,
            Mensaje = mensaje,
            XmlFirmado = xmlFirmado,
            CdrBase64 = cdrBase64,
            FechaEnvio = DateTime.Now
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

    private const string BaseSelect = @"
    SELECT 
        comprobanteID           AS ComprobanteId,
        tipoOperacion           AS TipoOperacion,
        tipoComprobante         AS TipoComprobante,
        serie                   AS Serie,
        correlativo             AS Correlativo,
        numeroCompleto          AS NumeroCompleto,
        tipoCambio              AS TipoCambio,
        fechaEmision            AS FechaEmision,
        TIMESTAMP(fechaEmision, horaEmision) AS HoraEmision,
        fechaVencimiento        AS FechaVencimiento,
        tipoMoneda              AS TipoMoneda,
        tipoPago                AS TipoPago,

        empresaID               AS EmpresaId,
        empresaRuc              AS EmpresaRuc,
        empresaRazonSocial      AS EmpresaRazonSocial,
        empresaNombreComercial  AS EmpresaNombreComercial,
        establecimientoAnexo    AS EmpresaEstablecimientoAnexo,
        empresaDireccion        AS EmpresaDireccion,
        empresaProvincia        AS EmpresaProvincia,
        empresaDepartamento     AS EmpresaDepartamento,
        empresaDistrito         AS EmpresaDistrito,
        empresaUbigeo           AS EmpresaUbigeo,

        clienteID               AS ClienteId,
        clienteTipoDoc          AS ClienteTipoDoc,
        clienteNumDoc           AS ClienteNumDoc,
        clienteRznSocial        AS ClienteRazonSocial,
        clienteDireccion        AS ClienteDireccion,
        clienteProvincia        AS ClienteProvincia,
        clienteDepartamento     AS ClienteDepartamento,
        clienteDistrito         AS ClienteDistrito,
        clienteUbigeo           AS ClienteUbigeo,
        clienteCorreo          AS ClienteCorreo,
        enviadoPorCorreo       AS EnviadoPorCorreo,
        clienteWhatsApp        AS ClienteWhatsApp,
        enviadoPorWhatsApp     AS EnviadoPorWhatsApp,

        codigoTipoDescGlobal        AS CodigoTipoDescGlobal,
        descuentoGlobal        AS DescuentoGlobal,
        totalOperacionesGravadas   AS TotalOperacionesGravadas,
        totalOperacionesExoneradas AS TotalOperacionesExoneradas,
        totalOperacionesInafectas  AS TotalOperacionesInafectas,
        totalOperacionesGratuitas  AS TotalOperacionesGratuitas,
        totalIgvGratuitas       AS TotalIgvGratuitas,
        totalIGV                AS TotalIGV,
        totalImpuestos          AS TotalImpuestos,
        totalDescuentos         AS TotalDescuentos,
        totalOtrosCargos        AS TotalOtrosCargos,
        totalIcbper             AS TotalIcbper,
        valorVenta              AS ValorVenta,
        subTotal                AS SubTotal,
        importeTotal            AS ImporteTotal,
        montoCredito            AS MontoCredito,

        tipDocAfectado         AS TipDocAfectado,
        numDocAfectado         AS NumDocAfectado,
        tipoNotaCreditoDebito  AS TipoNotaCreditoDebito,
        motivoNota             AS MotivoNota,
        comprobanteAfectadoID  AS ComprobanteAfectadoId,
        observaciones          AS Observaciones,

        estadoSunat             AS EstadoSunat,
        pdfGenerado            AS PdfGenerado,
        enviadoEnResumen       AS EnviadoEnResumen,
        codigoHashCPE           AS CodigoHashCPE,
        codigoRespuestaSunat    AS CodigoRespuestaSunat,
        mensajeRespuestaSunat   AS MensajeRespuestaSunat,
        fechaEnvioSunat         AS FechaEnvioSunat,

        xmlGenerado             AS XmlGenerado,

        usuarioCreacion         AS UsuarioCreacion,
        fechaCreacion           AS FechaCreacion,
        usuarioModificacion     AS UsuarioModificacion,
        fechaModificacion       AS FechaModificacion

    FROM comprobante
    ";

}