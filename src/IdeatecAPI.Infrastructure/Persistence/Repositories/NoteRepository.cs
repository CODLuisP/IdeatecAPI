using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class NoteRepository : DapperRepository<Note>, INoteRepository
{
    public NoteRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    public async Task<IEnumerable<Note>> GetAllNotesAsync(int empresaId)
    {
        var sql = @"
            SELECT 
            comprobanteID           AS ComprobanteId,
            empresaID               AS EmpresaId,
            clienteID               AS ClienteId,
            tipoComprobante         AS TipoDoc,
            serie                   AS Serie,
            correlativo             AS Correlativo,
            numeroCompleto          AS NumeroCompleto,
            DATE_ADD(fechaEmision, INTERVAL TIME_TO_SEC(horaEmision) SECOND) AS FechaEmision,
            tipoMoneda              AS TipoMoneda,
            comprobanteAfectadoID   AS ComprobanteAfectadoId,
            tipDocAfectado          AS TipDocAfectado,
            numDocAfectado          AS NumDocAfectado,
            tipoNotaCreditoDebito   AS TipoNotaCreditoDebito,
            motivoNota              AS MotivoNota,
            totalOperacionesGravadas AS MtoOperGravadas,
            totalOperacionesExoneradas AS MtoOperExoneradas,
            totalOperacionesInafectas AS MtoOperInafectas,
            totalIGV                AS MtoIGV,
            totalIcbper             AS TotalIcbper,
            valorVenta              AS ValorVenta,
            subTotal                AS SubTotal,
            importeTotal            AS MtoImpVenta,
            estadoSunat             AS EstadoSunat,
            codigoRespuestaSunat    AS CodigoRespuestaSunat,
            mensajeRespuestaSunat   AS MensajeRespuestaSunat,
            xmlGenerado             AS XmlGenerado,
            cdrSunat                AS CdrSunat,
            fechaEnvioSunat         AS FechaEnvioSunat,
            usuarioCreacion         AS UsuarioCreacion,
            fechaCreacion           AS FechaCreacion,
            usuarioModificacion     AS UsuarioModificacion,
            fechaModificacion       AS FechaModificacion,
            clienteTipoDoc          AS ClienteTipoDoc,
            clienteNumDoc           AS ClienteNumDoc,
            clienteRznSocial        AS ClienteRznSocial,
            clienteDireccion        AS ClienteDireccion,
            clienteProvincia        AS ClienteProvincia,
            clienteDepartamento     AS ClienteDepartamento,
            clienteDistrito         AS ClienteDistrito,
            clienteUbigeo           AS ClienteUbigeo,
            empresaRuc            AS EmpresaRuc,
            empresaRazonSocial    AS EmpresaRazonSocial,
            establecimientoAnexo  AS EstablecimientoAnexo,
            empresaNombreComercial AS EmpresaNombreComercial,
            empresaDireccion      AS EmpresaDireccion,
            empresaProvincia      AS EmpresaProvincia,
            empresaDepartamento   AS EmpresaDepartamento,
            empresaDistrito       AS EmpresaDistrito,
            empresaUbigeo         AS EmpresaUbigeo
        FROM comprobante 
            WHERE empresaID = @EmpresaId 
              AND tipoComprobante IN ('07', '08')
              AND estadoSunat != 'ANULADO'
            ORDER BY fechaEmision DESC, comprobanteID DESC";

        return await _connection.QueryAsync<Note>(sql, new { EmpresaId = empresaId }, _transaction);
    }

    public async Task<Note?> GetNoteByIdAsync(int comprobanteId)
    {
        var sql = @"
        SELECT 
            comprobanteID           AS ComprobanteId,
            empresaID               AS EmpresaId,
            clienteID               AS ClienteId,
            tipoComprobante         AS TipoDoc,
            serie                   AS Serie,
            correlativo             AS Correlativo,
            numeroCompleto          AS NumeroCompleto,
            DATE_ADD(fechaEmision, INTERVAL TIME_TO_SEC(horaEmision) SECOND) AS FechaEmision,
            tipoMoneda              AS TipoMoneda,
            comprobanteAfectadoID   AS ComprobanteAfectadoId,
            tipDocAfectado          AS TipDocAfectado,
            numDocAfectado          AS NumDocAfectado,
            tipoNotaCreditoDebito   AS TipoNotaCreditoDebito,
            motivoNota              AS MotivoNota,
            totalOperacionesGravadas AS MtoOperGravadas,
            totalOperacionesExoneradas AS MtoOperExoneradas,
            totalOperacionesInafectas AS MtoOperInafectas,
            totalIGV                AS MtoIGV,
            totalIcbper             AS TotalIcbper,
            valorVenta              AS ValorVenta,
            subTotal                AS SubTotal,
            importeTotal            AS MtoImpVenta,
            estadoSunat             AS EstadoSunat,
            codigoRespuestaSunat    AS CodigoRespuestaSunat,
            mensajeRespuestaSunat   AS MensajeRespuestaSunat,
            xmlGenerado             AS XmlGenerado,
            cdrSunat                AS CdrSunat,
            fechaEnvioSunat         AS FechaEnvioSunat,
            usuarioCreacion         AS UsuarioCreacion,
            fechaCreacion           AS FechaCreacion,
            usuarioModificacion     AS UsuarioModificacion,
            fechaModificacion       AS FechaModificacion,
            clienteTipoDoc          AS ClienteTipoDoc,
            clienteNumDoc           AS ClienteNumDoc,
            clienteRznSocial        AS ClienteRznSocial,
            clienteDireccion        AS ClienteDireccion,
            clienteProvincia        AS ClienteProvincia,
            clienteDepartamento     AS ClienteDepartamento,
            clienteDistrito         AS ClienteDistrito,
            clienteUbigeo           AS ClienteUbigeo,
            empresaRuc            AS EmpresaRuc,
            establecimientoAnexo  AS EstablecimientoAnexo,
            empresaRazonSocial    AS EmpresaRazonSocial,
            empresaNombreComercial AS EmpresaNombreComercial,
            empresaDireccion      AS EmpresaDireccion,
            empresaProvincia      AS EmpresaProvincia,
            empresaDepartamento   AS EmpresaDepartamento,
            empresaDistrito       AS EmpresaDistrito,
            empresaUbigeo         AS EmpresaUbigeo
        FROM comprobante 
        WHERE comprobanteID = @ComprobanteId 
          AND tipoComprobante IN ('07', '08')";

        return await _connection.QueryFirstOrDefaultAsync<Note>(sql,
            new { ComprobanteId = comprobanteId }, _transaction);
    }

    public async Task<Note?> GetNoteByNumeroAsync(int empresaId, string tipoDoc, string serie, int correlativo)
    {
        var sql = @"
        SELECT 
            comprobanteID           AS ComprobanteId,
            empresaID               AS EmpresaId,
            clienteID               AS ClienteId,
            tipoComprobante         AS TipoDoc,
            serie                   AS Serie,
            correlativo             AS Correlativo,
            numeroCompleto          AS NumeroCompleto,
            DATE_ADD(fechaEmision, INTERVAL TIME_TO_SEC(horaEmision) SECOND) AS FechaEmision,
            tipoMoneda              AS TipoMoneda,
            comprobanteAfectadoID   AS ComprobanteAfectadoId,
            tipDocAfectado          AS TipDocAfectado,
            numDocAfectado          AS NumDocAfectado,
            tipoNotaCreditoDebito   AS TipoNotaCreditoDebito,
            motivoNota              AS MotivoNota,
            totalOperacionesGravadas AS MtoOperGravadas,
            totalOperacionesExoneradas AS MtoOperExoneradas,
            totalOperacionesInafectas AS MtoOperInafectas,
            totalIGV                AS MtoIGV,
            totalIcbper             AS TotalIcbper,
            valorVenta              AS ValorVenta,
            subTotal                AS SubTotal,
            importeTotal            AS MtoImpVenta,
            estadoSunat             AS EstadoSunat,
            codigoRespuestaSunat    AS CodigoRespuestaSunat,
            mensajeRespuestaSunat   AS MensajeRespuestaSunat,
            xmlGenerado             AS XmlGenerado,
            cdrSunat                AS CdrSunat,
            fechaEnvioSunat         AS FechaEnvioSunat,
            usuarioCreacion         AS UsuarioCreacion,
            fechaCreacion           AS FechaCreacion,
            usuarioModificacion     AS UsuarioModificacion,
            fechaModificacion       AS FechaModificacion,
            clienteTipoDoc          AS ClienteTipoDoc,
            clienteNumDoc           AS ClienteNumDoc,
            clienteRznSocial        AS ClienteRznSocial,
            clienteDireccion        AS ClienteDireccion,
            clienteProvincia        AS ClienteProvincia,
            clienteDepartamento     AS ClienteDepartamento,
            clienteDistrito         AS ClienteDistrito,
            clienteUbigeo           AS ClienteUbigeo,
            empresaRuc              AS EmpresaRuc,
            establecimientoAnexo    AS EstablecimientoAnexo,
            empresaRazonSocial      AS EmpresaRazonSocial,
            empresaNombreComercial  AS EmpresaNombreComercial,
            empresaDireccion        AS EmpresaDireccion,
            empresaProvincia        AS EmpresaProvincia,
            empresaDepartamento     AS EmpresaDepartamento,
            empresaDistrito         AS EmpresaDistrito,
            empresaUbigeo           AS EmpresaUbigeo
        FROM comprobante 
        WHERE empresaID      = @EmpresaId
          AND tipoComprobante = @TipoDoc
          AND serie           = @Serie
          AND correlativo     = @Correlativo
          AND tipoComprobante IN ('07', '08')";

        return await _connection.QueryFirstOrDefaultAsync<Note>(sql,
            new { EmpresaId = empresaId, TipoDoc = tipoDoc, Serie = serie, Correlativo = correlativo },
            _transaction);
    }

    public async Task<bool> ExisteNoteAsync(int empresaId, string tipoDoc, string serie, int correlativo)
    {
        var sql = @"
            SELECT COUNT(1) FROM comprobante 
            WHERE empresaID = @EmpresaId 
              AND tipoComprobante = @TipoDoc
              AND serie = @Serie 
              AND correlativo = @Correlativo";

        var count = await _connection.ExecuteScalarAsync<int>(sql,
            new { EmpresaId = empresaId, TipoDoc = tipoDoc, Serie = serie, Correlativo = correlativo },
            _transaction);

        return count > 0;
    }

    public async Task<int> CreateNoteAsync(Note note)
    {
        var sql = @"
        INSERT INTO comprobante (
            empresaID, empresaRuc, empresaRazonSocial, empresaNombreComercial,
            empresaDireccion, empresaProvincia, empresaDepartamento, empresaDistrito, empresaUbigeo,
            establecimientoAnexo, clienteID, clienteTipoDoc, clienteNumDoc, clienteRznSocial,
            clienteDireccion, clienteProvincia, clienteDepartamento, clienteDistrito, clienteUbigeo,
            tipoComprobante, serie, correlativo, fechaEmision, horaEmision, tipoMoneda,
            totalOperacionesGravadas, totalOperacionesExoneradas, totalOperacionesInafectas, totalIGV, 
            totalIcbper, valorVenta, subTotal, totalDescuentos, totalOtrosCargos, importeTotal,
            comprobanteAfectadoID, tipDocAfectado, numDocAfectado,
            tipoNotaCreditoDebito, motivoNota,
            estadoSunat, usuarioCreacion, fechaCreacion
        ) VALUES (
            @EmpresaId, @EmpresaRuc, @EmpresaRazonSocial, @EmpresaNombreComercial,
            @EmpresaDireccion, @EmpresaProvincia, @EmpresaDepartamento, @EmpresaDistrito, @EmpresaUbigeo,
            @EstablecimientoAnexo, @ClienteId, @ClienteTipoDoc, @ClienteNumDoc, @ClienteRznSocial,
            @ClienteDireccion, @ClienteProvincia, @ClienteDepartamento, @ClienteDistrito, @ClienteUbigeo,
            @TipoDoc, @Serie, @Correlativo, @FechaEmision, @HoraEmision, @TipoMoneda,
            @MtoOperGravadas, @MtoOperExoneradas, @MtoOperInafectas, @MtoIGV, 
            @TotalIcbper, @ValorVenta, @SubTotal, @TotalDescuentos, @TotalOtrosCargos, @MtoImpVenta,
            @ComprobanteAfectadoId, @TipDocAfectado, @NumDocAfectado,
            @TipoNotaCreditoDebito, @MotivoNota,
            @EstadoSunat, @UsuarioCreacion, @FechaCreacion
        );
        SELECT LAST_INSERT_ID();";

        var newId = await _connection.ExecuteScalarAsync<int>(sql, new
        {
            note.EmpresaId,
            note.EmpresaRuc,
            note.EmpresaRazonSocial,
            note.EstablecimientoAnexo,
            note.EmpresaNombreComercial,
            note.EmpresaDireccion,
            note.EmpresaProvincia,
            note.EmpresaDepartamento,
            note.EmpresaDistrito,
            note.EmpresaUbigeo,
            note.ClienteId,
            note.ClienteTipoDoc,
            note.ClienteNumDoc,
            note.ClienteRznSocial,
            note.ClienteDireccion,
            note.ClienteProvincia,
            note.ClienteDepartamento,
            note.ClienteDistrito,
            note.ClienteUbigeo,
            TipoDoc = note.TipoDoc,
            note.Serie,
            note.Correlativo,
            FechaEmision = note.FechaEmision.Date,
            HoraEmision = note.FechaEmision.TimeOfDay,
            note.TipoMoneda,
            note.MtoOperGravadas,
            MtoOperExoneradas = note.MtoOperExoneradas,
            MtoOperInafectas  = note.MtoOperInafectas, 
            note.MtoIGV,
            TotalIcbper = note.TotalIcbper,
            ValorVenta = note.ValorVenta,
            SubTotal = note.SubTotal,
            TotalDescuentos = 0m,
            TotalOtrosCargos = 0m,
            note.MtoImpVenta,
            note.ComprobanteAfectadoId,
            note.TipDocAfectado,
            note.NumDocAfectado,
            note.TipoNotaCreditoDebito,
            note.MotivoNota,
            note.EstadoSunat,
            note.UsuarioCreacion,
            FechaCreacion = DateTime.UtcNow
        }, _transaction);

        // ← Actualizar serie y correlativo actual
        await ActualizarSerieCorrelativoAsync(note);

        return newId;
    }

    private async Task ActualizarSerieCorrelativoAsync(Note note)
    {
        // Determinar si la nota afecta factura (FC/BC) o boleta (BE/BD)
        var esParaBoleta = note.TipDocAfectado == "03";

        string sql = (note.TipoDoc, esParaBoleta) switch
        {
            ("07", false) => @"UPDATE sucursal SET 
                serieNotaCredito               = @Serie,
                correlativoNotaCredito         = correlativoNotaCredito + 1
              WHERE empresaRuc                 = @EmpresaRuc
                AND codEstablecimiento         = @CodEstablecimiento
                AND estado                     = 1",

            ("07", true) => @"UPDATE sucursal SET 
                serieNotaCreditoBoleta         = @Serie,
                correlativoNotaCreditoBoleta   = correlativoNotaCreditoBoleta + 1
              WHERE empresaRuc                 = @EmpresaRuc
                AND codEstablecimiento         = @CodEstablecimiento
                AND estado                     = 1",

            ("08", false) => @"UPDATE sucursal SET 
                serieNotaDebito                = @Serie,
                correlativoNotaDebito          = correlativoNotaDebito + 1
              WHERE empresaRuc                 = @EmpresaRuc
                AND codEstablecimiento         = @CodEstablecimiento
                AND estado                     = 1",

            ("08", true) => @"UPDATE sucursal SET 
                serieNotaDebitoBoleta          = @Serie,
                correlativoNotaDebitoBoleta    = correlativoNotaDebitoBoleta + 1
              WHERE empresaRuc                 = @EmpresaRuc
                AND codEstablecimiento         = @CodEstablecimiento
                AND estado                     = 1",

            _ => throw new InvalidOperationException($"Tipo de nota '{note.TipoDoc}' no soportado.")
        };

        await _connection.ExecuteAsync(sql, new
        {
            note.Serie,
            note.EmpresaRuc,
            CodEstablecimiento = note.EstablecimientoAnexo
        }, _transaction);
    }

    public async Task UpdateNoteAsync(Note note)
    {
        var sql = @"
            UPDATE comprobante SET
                tipoMoneda           = @TipoMoneda,
                totalOperacionesGravadas = @MtoOperGravadas,
                totalIGV             = @MtoIGV,
                importeTotal         = @MtoImpVenta,
                comprobanteAfectadoID = @ComprobanteAfectadoId,
                tipDocAfectado       = @TipDocAfectado,
                numDocAfectado       = @NumDocAfectado,
                tipoNotaCreditoDebito = @TipoNotaCreditoDebito,
                motivoNota           = @MotivoNota,
                usuarioModificacion  = @UsuarioModificacion,
                fechaModificacion    = @FechaModificacion
            WHERE comprobanteID = @ComprobanteId
              AND tipoComprobante IN ('07', '08')";

        await _connection.ExecuteAsync(sql, new
        {
            note.TipoMoneda,
            note.MtoOperGravadas,
            note.MtoIGV,
            note.MtoImpVenta,
            note.ComprobanteAfectadoId,
            note.TipDocAfectado,
            note.NumDocAfectado,
            note.TipoNotaCreditoDebito,
            note.MotivoNota,
            note.UsuarioModificacion,
            FechaModificacion = DateTime.UtcNow,
            note.ComprobanteId
        }, _transaction);
    }

    public async Task UpdateEstadoSunatAsync(int comprobanteId, string estado, string? codigo,
        string? mensaje, string? xml, string? cdr)
    {
        var sql = @"
            UPDATE comprobante SET
                estadoSunat             = @Estado,
                codigoRespuestaSunat    = @Codigo,
                mensajeRespuestaSunat   = @Mensaje,
                xmlGenerado             = COALESCE(@Xml, xmlGenerado),
                cdrSunat                = COALESCE(@Cdr, cdrSunat),
                fechaEnvioSunat         = @FechaEnvio,
                fechaModificacion       = NOW()
            WHERE comprobanteID = @ComprobanteId";

        await _connection.ExecuteAsync(sql, new
        {
            ComprobanteId = comprobanteId,
            Estado = estado,
            Codigo = codigo,
            Mensaje = mensaje,
            Xml = xml,
            Cdr = cdr,
            FechaEnvio = DateTime.UtcNow
        }, _transaction);
    }
}