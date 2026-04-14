using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class GuiaRemisionRepository : IGuiaRemisionRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public GuiaRemisionRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<IEnumerable<GuiaRemision>> GetAllAsync(int empresaId)
    {
        var sql = @"SELECT
                        guiaId                  AS GuiaId,
                        empresaId               AS EmpresaId,
                        version                 AS Version,
                        tipoDoc                 AS TipoDoc,
                        serie                   AS Serie,
                        correlativo             AS Correlativo,
                        numeroCompleto          AS NumeroCompleto,
                        fechaEmision            AS FechaEmision,
                        empresaRuc              AS EmpresaRuc,
                        empresaRazonSocial      AS EmpresaRazonSocial,
                        empresaNombreComercial  AS EmpresaNombreComercial,
                        empresaDireccion        AS EmpresaDireccion,
                        empresaProvincia        AS EmpresaProvincia,
                        empresaDepartamento     AS EmpresaDepartamento,
                        empresaDistrito         AS EmpresaDistrito,
                        empresaUbigeo           AS EmpresaUbigeo,
                        destinatarioTipoDoc     AS DestinatarioTipoDoc,
                        destinatarioNumDoc      AS DestinatarioNumDoc,
                        destinatarioRznSocial   AS DestinatarioRznSocial,
                        terceroTipoDoc          AS TerceroTipoDoc,
                        terceroNumDoc           AS TerceroNumDoc,
                        terceroRznSocial        AS TerceroRznSocial,
                        observacion             AS Observacion,
                        docBajaTipoDoc          AS DocBajaTipoDoc,
                        docBajaNroDoc           AS DocBajaNroDoc,
                        relDocTipoDoc           AS RelDocTipoDoc,
                        relDocNroDoc            AS RelDocNroDoc,
                        codTraslado             AS CodTraslado,
                        desTraslado             AS DesTraslado,
                        modTraslado             AS ModTraslado,
                        fecTraslado             AS FecTraslado,
                        codPuerto               AS CodPuerto,
                        indTransbordo           AS IndTransbordo,
                        pesoTotal               AS PesoTotal,
                        undPesoTotal            AS UndPesoTotal,
                        numContenedor           AS NumContenedor,
                        matPeligrosoClase       AS MatPeligrosoClase,
                        matPeligrosoNroONU      AS MatPeligrosoNroONU,
                        llegadaUbigeo           AS LlegadaUbigeo,
                        llegadaDireccion        AS LlegadaDireccion,
                        llegadaDepartamento     AS LlegadaDepartamento,
                        llegadaProvincia        AS LlegadaProvincia,
                        llegadaDistrito         AS LlegadaDistrito, 
                        partidaUbigeo           AS PartidaUbigeo,
                        partidaDireccion        AS PartidaDireccion,
                        partidaDepartamento     AS PartidaDepartamento,
                        partidaProvincia        AS PartidaProvincia,
                        partidaDistrito         AS PartidaDistrito,  
                        transportistaTipoDoc    AS TransportistaTipoDoc,
                        transportistaNumDoc     AS TransportistaNumDoc,
                        transportistaRznSocial  AS TransportistaRznSocial,
                        transportistaRegistroMTC AS TransportistaRegistroMTC,
                        indVehiculoM1L          AS IndVehiculoM1L,
                        transportistaPlaca      AS TransportistaPlaca,
                        autorizacionVehiculoEntidad AS AutorizacionVehiculoEntidad,
                        autorizacionVehiculoNumero  AS AutorizacionVehiculoNumero,
                        placaSecundaria1          AS PlacaSecundaria1,
                        placaSecundaria2          AS PlacaSecundaria2,
                        placaSecundaria3          AS PlacaSecundaria3,
                        choferSecundarioTipoDoc   AS ChoferSecundarioTipoDoc,
                        choferSecundarioDoc       AS ChoferSecundarioDoc,
                        choferSecundarioNombres   AS ChoferSecundarioNombres,
                        choferSecundarioApellidos AS ChoferSecundarioApellidos,
                        choferSecundarioLicencia  AS ChoferSecundarioLicencia,
                        choferSecundario2TipoDoc   AS ChoferSecundario2TipoDoc,
                        choferSecundario2Doc       AS ChoferSecundario2Doc,
                        choferSecundario2Nombres   AS ChoferSecundario2Nombres,
                        choferSecundario2Apellidos AS ChoferSecundario2Apellidos,
                        choferSecundario2Licencia  AS ChoferSecundario2Licencia,
                        choferTipoDoc           AS ChoferTipoDoc,
                        choferDoc               AS ChoferDoc,
                        choferNombres           AS ChoferNombres,
                        choferApellidos         AS ChoferApellidos,
                        choferLicencia          AS ChoferLicencia,
                        estadoSunat             AS EstadoSunat,
                        codigoRespuestaSunat    AS CodigoRespuestaSunat,
                        mensajeRespuestaSunat   AS MensajeRespuestaSunat,
                        ticketSunat             AS TicketSunat,
                        cdrBase64               AS CdrBase64,
                        fechaEnvioSunat         AS FechaEnvioSunat,
                        fechaCreacion           AS FechaCreacion,
                        fechaModificacion       AS FechaModificacion
                    FROM guiaRemision
                    WHERE empresaId = @EmpresaId
                    ORDER BY fechaCreacion DESC";

        return await _connection.QueryAsync<GuiaRemision>(sql, new { EmpresaId = empresaId }, _transaction);
    }

    public async Task<GuiaRemision?> GetByIdAsync(int guiaId)
    {
        var sql = @"SELECT
                        guiaId                  AS GuiaId,
                        empresaId               AS EmpresaId,
                        version                 AS Version,
                        tipoDoc                 AS TipoDoc,
                        serie                   AS Serie,
                        correlativo             AS Correlativo,
                        numeroCompleto          AS NumeroCompleto,
                        fechaEmision            AS FechaEmision,
                        empresaRuc              AS EmpresaRuc,
                        empresaRazonSocial      AS EmpresaRazonSocial,
                        empresaNombreComercial  AS EmpresaNombreComercial,
                        empresaDireccion        AS EmpresaDireccion,
                        empresaProvincia        AS EmpresaProvincia,
                        empresaDepartamento     AS EmpresaDepartamento,
                        empresaDistrito         AS EmpresaDistrito,
                        empresaUbigeo           AS EmpresaUbigeo,
                        destinatarioTipoDoc     AS DestinatarioTipoDoc,
                        destinatarioNumDoc      AS DestinatarioNumDoc,
                        destinatarioRznSocial   AS DestinatarioRznSocial,
                        terceroTipoDoc          AS TerceroTipoDoc,
                        terceroNumDoc           AS TerceroNumDoc,
                        terceroRznSocial        AS TerceroRznSocial,
                        observacion             AS Observacion,
                        docBajaTipoDoc          AS DocBajaTipoDoc,
                        docBajaNroDoc           AS DocBajaNroDoc,
                        relDocTipoDoc           AS RelDocTipoDoc,
                        relDocNroDoc            AS RelDocNroDoc,
                        codTraslado             AS CodTraslado,
                        desTraslado             AS DesTraslado,
                        modTraslado             AS ModTraslado,
                        fecTraslado             AS FecTraslado,
                        codPuerto               AS CodPuerto,
                        indTransbordo           AS IndTransbordo,
                        pesoTotal               AS PesoTotal,
                        undPesoTotal            AS UndPesoTotal,
                        numContenedor           AS NumContenedor,
                        matPeligrosoClase       AS MatPeligrosoClase,
                        matPeligrosoNroONU      AS MatPeligrosoNroONU,
                        llegadaUbigeo           AS LlegadaUbigeo,
                        llegadaDireccion        AS LlegadaDireccion,
                        llegadaDepartamento     AS LlegadaDepartamento,
                        llegadaProvincia        AS LlegadaProvincia,
                        llegadaDistrito         AS LlegadaDistrito, 
                        partidaUbigeo           AS PartidaUbigeo,
                        partidaDireccion        AS PartidaDireccion,
                        partidaDepartamento     AS PartidaDepartamento,
                        partidaProvincia        AS PartidaProvincia,
                        partidaDistrito         AS PartidaDistrito,  
                        transportistaTipoDoc    AS TransportistaTipoDoc,
                        transportistaNumDoc     AS TransportistaNumDoc,
                        transportistaRznSocial  AS TransportistaRznSocial,
                        transportistaRegistroMTC AS TransportistaRegistroMTC,
                        indVehiculoM1L          AS IndVehiculoM1L,
                        transportistaPlaca      AS TransportistaPlaca,
                        autorizacionVehiculoEntidad AS AutorizacionVehiculoEntidad,
                        autorizacionVehiculoNumero  AS AutorizacionVehiculoNumero,
                        placaSecundaria1          AS PlacaSecundaria1,
                        placaSecundaria2          AS PlacaSecundaria2,
                        placaSecundaria3          AS PlacaSecundaria3,
                        choferSecundarioTipoDoc   AS ChoferSecundarioTipoDoc,
                        choferSecundarioDoc       AS ChoferSecundarioDoc,
                        choferSecundarioNombres   AS ChoferSecundarioNombres,
                        choferSecundarioApellidos AS ChoferSecundarioApellidos,
                        choferSecundarioLicencia  AS ChoferSecundarioLicencia,
                        choferSecundario2TipoDoc   AS ChoferSecundario2TipoDoc,
                        choferSecundario2Doc       AS ChoferSecundario2Doc,
                        choferSecundario2Nombres   AS ChoferSecundario2Nombres,
                        choferSecundario2Apellidos AS ChoferSecundario2Apellidos,
                        choferSecundario2Licencia  AS ChoferSecundario2Licencia,
                        choferTipoDoc           AS ChoferTipoDoc,
                        choferDoc               AS ChoferDoc,
                        choferNombres           AS ChoferNombres,
                        choferApellidos         AS ChoferApellidos,
                        choferLicencia          AS ChoferLicencia,
                        estadoSunat             AS EstadoSunat,
                        codigoRespuestaSunat    AS CodigoRespuestaSunat,
                        mensajeRespuestaSunat   AS MensajeRespuestaSunat,
                        ticketSunat             AS TicketSunat,
                        cdrBase64               AS CdrBase64,
                        fechaEnvioSunat         AS FechaEnvioSunat,
                        fechaCreacion           AS FechaCreacion,
                        fechaModificacion       AS FechaModificacion
                    FROM guiaRemision
                    WHERE guiaId = @GuiaId";

        return await _connection.QueryFirstOrDefaultAsync<GuiaRemision>(sql, new { GuiaId = guiaId }, _transaction);
    }

    public async Task<GuiaRemision?> GetBySerieCorrelativoAsync(string empresaRuc, string serie, int correlativo)
    {
        var sql = @"SELECT
                    guiaId          AS GuiaId,
                    empresaId       AS EmpresaId,
                    tipoDoc         AS TipoDoc,
                    serie           AS Serie,
                    correlativo     AS Correlativo,
                    numeroCompleto  AS NumeroCompleto,
                    empresaRuc      AS EmpresaRuc,
                    estadoSunat     AS EstadoSunat
                FROM guiaRemision
                WHERE empresaRuc  = @EmpresaRuc
                  AND serie       = @Serie
                  AND correlativo = @Correlativo
                  AND estadoSunat = 'ACEPTADO'
                LIMIT 1";

        return await _connection.QueryFirstOrDefaultAsync<GuiaRemision>(sql, new
        {
            EmpresaRuc = empresaRuc,
            Serie = serie,
            Correlativo = correlativo
        }, _transaction);
    }

    public async Task<int> CreateAsync(GuiaRemision guia)
    {
        // ← Calcular numeroCompleto antes del INSERT
        guia.NumeroCompleto = $"{guia.Serie}-{guia.Correlativo:D8}";

        var sql = @"INSERT INTO guiaRemision (
                        empresaId, sucursalId, version, tipoDoc, serie, correlativo, numeroCompleto, fechaEmision,
                        empresaRuc, empresaRazonSocial, empresaNombreComercial,
                        empresaDireccion, empresaProvincia, empresaDepartamento,
                        empresaDistrito, empresaUbigeo,
                        destinatarioTipoDoc, destinatarioNumDoc, destinatarioRznSocial,
                        terceroTipoDoc, terceroNumDoc, terceroRznSocial,
                        observacion, docBajaTipoDoc, docBajaNroDoc, relDocTipoDoc, relDocNroDoc,
                        codTraslado, desTraslado, modTraslado, fecTraslado,
                        codPuerto, indTransbordo, pesoTotal, undPesoTotal, numContenedor, matPeligrosoClase, matPeligrosoNroONU,
                        llegadaUbigeo, llegadaDireccion, partidaUbigeo, partidaDireccion,
                        transportistaTipoDoc, transportistaNumDoc, transportistaRznSocial, transportistaRegistroMTC,
                        transportistaPlaca, placaSecundaria1, placaSecundaria2, placaSecundaria3,
                        choferSecundarioTipoDoc, choferSecundarioDoc, choferSecundarioNombres,
                        choferSecundarioApellidos, choferSecundarioLicencia, choferSecundario2TipoDoc, 
                        choferSecundario2Doc, choferSecundario2Nombres, choferSecundario2Apellidos, 
                        choferSecundario2Licencia, choferTipoDoc, choferDoc, 
                        choferNombres, choferApellidos, choferLicencia, estadoSunat, fechaCreacion, indVehiculoM1L, autorizacionVehiculoEntidad, 
                        autorizacionVehiculoNumero, partidaDepartamento, partidaProvincia, partidaDistrito,
                        llegadaDepartamento, llegadaProvincia, llegadaDistrito
                    ) VALUES (
                        @EmpresaId, @SucursalId, @Version, @TipoDoc, @Serie, @Correlativo, @NumeroCompleto, @FechaEmision,
                        @EmpresaRuc, @EmpresaRazonSocial, @EmpresaNombreComercial,
                        @EmpresaDireccion, @EmpresaProvincia, @EmpresaDepartamento,
                        @EmpresaDistrito, @EmpresaUbigeo,
                        @DestinatarioTipoDoc, @DestinatarioNumDoc, @DestinatarioRznSocial,
                        @TerceroTipoDoc, @TerceroNumDoc, @TerceroRznSocial,
                        @Observacion, @DocBajaTipoDoc, @DocBajaNroDoc, @RelDocTipoDoc, @RelDocNroDoc,
                        @CodTraslado, @DesTraslado, @ModTraslado, @FecTraslado,
                        @CodPuerto, @IndTransbordo, @PesoTotal, @UndPesoTotal, @NumContenedor, @MatPeligrosoClase, @MatPeligrosoNroONU,
                        @LlegadaUbigeo, @LlegadaDireccion, @PartidaUbigeo, @PartidaDireccion,
                        @TransportistaTipoDoc, @TransportistaNumDoc, @TransportistaRznSocial, @TransportistaRegistroMTC,
                        @TransportistaPlaca, @PlacaSecundaria1, @PlacaSecundaria2, @PlacaSecundaria3,
                        @ChoferSecundarioTipoDoc, @ChoferSecundarioDoc, @ChoferSecundarioNombres,
                        @ChoferSecundarioApellidos, @ChoferSecundarioLicencia, @ChoferSecundario2TipoDoc, @ChoferSecundario2Doc, 
                        @ChoferSecundario2Nombres, @ChoferSecundario2Apellidos, @ChoferSecundario2Licencia, @ChoferTipoDoc, @ChoferDoc, 
                        @ChoferNombres, @ChoferApellidos, @ChoferLicencia, @EstadoSunat, @FechaCreacion, @IndVehiculoM1L, @AutorizacionVehiculoEntidad, 
                        @AutorizacionVehiculoNumero, @PartidaDepartamento, @PartidaProvincia, @PartidaDistrito,
                        @LlegadaDepartamento, @LlegadaProvincia, @LlegadaDistrito
                    );
                    SELECT LAST_INSERT_ID();";

        var newId = await _connection.ExecuteScalarAsync<int>(sql, guia, _transaction);

        // ← Actualizar serie y correlativo
        await ActualizarSerieCorrelativoAsync(guia);

        return newId;
    }

    public async Task UpdateEstadoAsync(int guiaId, string estado, string? codigo, string? mensaje, string? ticket, string? cdr, DateTime? fechaEnvio)
    {
        var sql = @"UPDATE guiaRemision SET
                        estadoSunat           = @Estado,
                        codigoRespuestaSunat  = @Codigo,
                        mensajeRespuestaSunat = @Mensaje,
                        ticketSunat           = @Ticket,
                        cdrBase64             = @Cdr,
                        fechaEnvioSunat       = @FechaEnvio,
                        fechaModificacion     = NOW()
                    WHERE guiaId = @GuiaId";

        await _connection.ExecuteAsync(sql, new
        {
            GuiaId = guiaId,
            Estado = estado,
            Codigo = codigo,
            Mensaje = mensaje,
            Ticket = ticket,
            Cdr = cdr,
            FechaEnvio = fechaEnvio
        }, _transaction);
    }

    public async Task<bool> ExisteAsync(int empresaId, string tipoDoc, string serie, int correlativo)
    {
        var sql = @"SELECT COUNT(1) FROM guiaRemision 
                WHERE empresaId      = @EmpresaId
                  AND tipoDoc        = @TipoDoc
                  AND serie          = @Serie
                  AND correlativo    = @Correlativo
                  AND estadoSunat   != 'ANULADO'";

        var count = await _connection.ExecuteScalarAsync<int>(sql, new
        {
            EmpresaId = empresaId,
            TipoDoc = tipoDoc,
            Serie = serie,
            Correlativo = correlativo
        }, _transaction);

        return count > 0;
    }

    private async Task ActualizarSerieCorrelativoAsync(GuiaRemision guia)
    {
        string sql = guia.TipoDoc switch
        {
            "09" => @"UPDATE sucursal SET 
                    serieGuiaRemision       = @Serie,
                    correlativoGuiaRemision = correlativoGuiaRemision + 1
                  WHERE sucursalId          = @SucursalId
                  AND estado                = 1",

            "31" => @"UPDATE sucursal SET 
                    serieGuiaTransportista        = @Serie,
                    correlativoGuiaTransportista  = correlativoGuiaTransportista + 1
                  WHERE sucursalId                = @SucursalId
                  AND estado                      = 1",

            _ => throw new InvalidOperationException($"Tipo de guía '{guia.TipoDoc}' no soportado.")
        };

        await _connection.ExecuteAsync(sql, new
        {
            guia.Serie,
            guia.SucursalId   // ← reemplaza EmpresaRuc
        }, _transaction);
    }
}