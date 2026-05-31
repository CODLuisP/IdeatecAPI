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
                estadoSunat, enviadoEnResumen, xmlGenerado, usuarioCreacion, fechaCreacion, codigoHashCPE
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
                @EstadoSunat,  @EnviadoEnResumen, @XmlGenerado, @UsuarioCreacion, @FechaCreacion, @CodigoHashCPE
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
            comprobante.FechaCreacion,
            comprobante.CodigoHashCPE
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

    private async Task ActualizarSerieCorrelativoAsync(Comprobante comprobante)
    {
        // 1. Obtener el ID de la sucursal para asegurar precisión
        var sucursalId = await GetSucursalIdByRucAndAnexoAsync(comprobante.EmpresaRuc!, comprobante.EmpresaEstablecimientoAnexo!);
        
        if (sucursalId == null)
            throw new InvalidOperationException($"No se encontró una sucursal activa para el RUC {comprobante.EmpresaRuc} y establecimiento {comprobante.EmpresaEstablecimientoAnexo}");

        // 2. Definir SQL de actualización por ID (Independencia total por entorno ya que _connection es dinámica)
        string sql = comprobante.TipoComprobante switch
        {
            "01" => @"UPDATE sucursal SET 
                        serieFactura       = @Serie,
                        correlativoFactura = correlativoFactura + 1
                    WHERE sucursalID = @SucursalId",

            "03" => @"UPDATE sucursal SET 
                        serieBoleta       = @Serie,
                        correlativoBoleta = correlativoBoleta + 1
                    WHERE sucursalID = @SucursalId",

            _ => throw new InvalidOperationException($"Tipo de comprobante '{comprobante.TipoComprobante}' no soportado para incremento automático.")
        };

        var parameters = new
        {
            comprobante.Serie,
            SucursalId = sucursalId
        };

        await _connection.ExecuteAsync(sql, parameters, _transaction);
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
            SELECT comprobanteId, trabajadorID, item, productoId, codigo, descripcion, cantidad,
                   unidadMedida, precioUnitario, tipoAfectacionIGV, porcentajeIGV,
                   montoIGV, baseIgv, codigoTipoDescuento, descuentoUnitario, descuentoTotal,
                   valorVenta, precioVenta, totalVentaItem, icbper, factorIcbper
            FROM comprobantedetalle WHERE comprobanteId = @Id;

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

        foreach (var valeId in valeIds)
            await _connection.ExecuteAsync(sql, new { ComprobanteId = comprobanteId, ValeId = valeId }, _transaction);
    }

    public async Task<IEnumerable<int>> GetValesByComprobanteIdAsync(int comprobanteId)
    {
        var sql = "SELECT valeId FROM comprobantevale WHERE comprobanteId = @ComprobanteId";
        return await _connection.QueryAsync<int>(sql, new { ComprobanteId = comprobanteId }, _transaction);
    }

    public async Task<bool> UpdateOrdenServicioSpotAsync(string ruc, string serie, int correlativo, string? ordenServicio, bool? spot)
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
        ordenservicio           AS OrdenServicio,
        spot                    AS Spot,
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
        fechaModificacion       AS FechaModificacion,
        xmlGenerado             AS XmlGenerado,
        xmlRespuestaSunat       AS XmlRespuestaSunat
    FROM comprobante
    ";

    // Siempre devuelve la hora actual en zona horaria Lima (UTC-5), sin importar
    // dónde esté desplegado el servidor (DigitalOcean usa UTC por defecto).
    private static DateTime AhoraLima()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Lima");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
    }
}