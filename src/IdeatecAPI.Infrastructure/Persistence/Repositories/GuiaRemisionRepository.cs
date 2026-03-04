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
                        llegadaUbigeo           AS LlegadaUbigeo,
                        llegadaDireccion        AS LlegadaDireccion,
                        partidaUbigeo           AS PartidaUbigeo,
                        partidaDireccion        AS PartidaDireccion,
                        transportistaTipoDoc    AS TransportistaTipoDoc,
                        transportistaNumDoc     AS TransportistaNumDoc,
                        transportistaRznSocial  AS TransportistaRznSocial,
                        transportistaPlaca      AS TransportistaPlaca,
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
                        llegadaUbigeo           AS LlegadaUbigeo,
                        llegadaDireccion        AS LlegadaDireccion,
                        partidaUbigeo           AS PartidaUbigeo,
                        partidaDireccion        AS PartidaDireccion,
                        transportistaTipoDoc    AS TransportistaTipoDoc,
                        transportistaNumDoc     AS TransportistaNumDoc,
                        transportistaRznSocial  AS TransportistaRznSocial,
                        transportistaPlaca      AS TransportistaPlaca,
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

    public async Task<int> CreateAsync(GuiaRemision guia)
    {
        // ← Calcular numeroCompleto antes del INSERT
        guia.NumeroCompleto = $"{guia.Serie}-{guia.Correlativo:D8}";

        var sql = @"INSERT INTO guiaRemision (
                        empresaId, version, tipoDoc, serie, correlativo, numeroCompleto, fechaEmision,
                        empresaRuc, empresaRazonSocial, empresaNombreComercial,
                        empresaDireccion, empresaProvincia, empresaDepartamento,
                        empresaDistrito, empresaUbigeo,
                        destinatarioTipoDoc, destinatarioNumDoc, destinatarioRznSocial,
                        terceroTipoDoc, terceroNumDoc, terceroRznSocial,
                        observacion, docBajaTipoDoc, docBajaNroDoc, relDocTipoDoc, relDocNroDoc,
                        codTraslado, desTraslado, modTraslado, fecTraslado,
                        codPuerto, indTransbordo, pesoTotal, undPesoTotal, numContenedor,
                        llegadaUbigeo, llegadaDireccion, partidaUbigeo, partidaDireccion,
                        transportistaTipoDoc, transportistaNumDoc, transportistaRznSocial,
                        transportistaPlaca, choferTipoDoc, choferDoc, choferNombres, choferApellidos, 
                        choferLicencia, estadoSunat, fechaCreacion
                    ) VALUES (
                        @EmpresaId, @Version, @TipoDoc, @Serie, @Correlativo, @NumeroCompleto, @FechaEmision,
                        @EmpresaRuc, @EmpresaRazonSocial, @EmpresaNombreComercial,
                        @EmpresaDireccion, @EmpresaProvincia, @EmpresaDepartamento,
                        @EmpresaDistrito, @EmpresaUbigeo,
                        @DestinatarioTipoDoc, @DestinatarioNumDoc, @DestinatarioRznSocial,
                        @TerceroTipoDoc, @TerceroNumDoc, @TerceroRznSocial,
                        @Observacion, @DocBajaTipoDoc, @DocBajaNroDoc, @RelDocTipoDoc, @RelDocNroDoc,
                        @CodTraslado, @DesTraslado, @ModTraslado, @FecTraslado,
                        @CodPuerto, @IndTransbordo, @PesoTotal, @UndPesoTotal, @NumContenedor,
                        @LlegadaUbigeo, @LlegadaDireccion, @PartidaUbigeo, @PartidaDireccion,
                        @TransportistaTipoDoc, @TransportistaNumDoc, @TransportistaRznSocial,
                        @TransportistaPlaca, @ChoferTipoDoc, @ChoferDoc, @ChoferNombres, @ChoferApellidos, 
                        @ChoferLicencia, @EstadoSunat, @FechaCreacion
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
        var sql = @"
        UPDATE serie
        SET serie              = @Serie,
            correlativoActual  = @Correlativo,
            fechaActualizacion = NOW()
        WHERE empresaID        = @EmpresaId
          AND tipoComprobante  = @TipoDoc
          AND serie            = @Serie";

        await _connection.ExecuteAsync(sql, new
        {
            guia.Serie,
            guia.Correlativo,
            guia.EmpresaId,
            guia.TipoDoc
        }, _transaction);
    }
}