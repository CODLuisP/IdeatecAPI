using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class ResumenComprobanteRepository : DapperRepository<ResumenComprobante>, IResumenComprobanteRepository
{
    public ResumenComprobanteRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction) { }

    // ── SQL base cabecera resumen ─────────────────────────────────────────────
    private const string SqlCabeceraCompleta = @"
        r.resumenID                  AS ResumenComprobanteId,
        r.empresaID                  AS EmpresaId,
        r.empresaRuc                 AS EmpresaRuc,
        r.empresaRazonSocial         AS EmpresaRazonSocial,
        r.empresaDireccion           AS EmpresaDireccion,
        r.empresaProvincia           AS EmpresaProvincia,
        r.empresaDepartamento        AS EmpresaDepartamento,
        r.empresaDistrito            AS EmpresaDistrito,
        r.empresaUbigeo              AS EmpresaUbigeo,
        r.establecimientoAnexo       AS EstablecimientoAnexo,
        r.numeroEnvio                AS NumeroEnvio,
        r.fechaEmisionDocumentos     AS FechaEmisionDocumentos,
        r.fechaGeneracion            AS FechaGeneracion,
        r.identificador              AS Identificador,
        r.estadoSunat                AS EstadoSunat,
        r.ticket                     AS Ticket,
        r.codigoRespuesta            AS CodigoRespuesta,
        r.mensajeRespuesta           AS MensajeRespuesta,
        r.xmlGenerado                AS XmlGenerado,
        r.pdfGenerado                AS PdfGenerado,
        r.fechaEnvio                 AS FechaEnvio,
        r.estado                     AS Estado,
        r.usuarioCreacion            AS UsuarioCreacion";

    private const string SqlDetalle = @"
        d.resumendiariodetalleID     AS ResumenComprobanteDetalleId,
        d.lineaId                    AS LineID,
        d.comprobanteId              AS ComprobanteId,
        d.resumenID                  AS ResumenComprobanteId,
        d.tipoComprobanteId          AS TipoComprobante,
        d.serie                      AS Serie,
        d.correlativo                AS Correlativo,
        d.clienteTipoDoc             AS ClienteTipoDoc,
        d.clienteNumDoc              AS ClienteNumDoc,
        d.clienteNombre              AS ClienteNombre,
        d.documentoAfectadoTipo      AS DocumentoAfectadoTipo,
        d.documentoAfectadoNumero    AS DocumentoAfectadoNumero,
        d.codigoCondicion            AS CodigoCondicion,
        d.moneda                     AS Moneda,
        d.montoTotalVenta            AS MontoTotalVenta,
        d.totalGrabado               AS TotalGravado,
        d.totalExonerado             AS TotalExonerado,
        d.totalInafecto              AS TotalInafecto,
        d.totalGratuito              AS TotalGratuito,
        d.totalIGV                   AS TotalIGV,
        d.igvReferencial             AS IGVReferencial";

    // ── Helper multi-map ──────────────────────────────────────────────────────
    private static Func<ResumenComprobante, ResumenComprobanteDetalle, ResumenComprobante>
        BuildMultiMap(Dictionary<int, ResumenComprobante> dict) =>
        (resumen, detalle) =>
        {
            if (!dict.TryGetValue(resumen.ResumenComprobanteId, out var entry))
            {
                entry = resumen;
                entry.DetallesResumen = [];
                dict[entry.ResumenComprobanteId] = entry;
            }
            if (detalle?.ResumenComprobanteDetalleId > 0)
                entry.DetallesResumen.Add(detalle);
            return entry;
        };

    // ── 1. OBTENER TODOS ─────────────────────────────────────────────────────
    public async Task<IEnumerable<ResumenComprobante>> GetAllResumenComprobanteAsync()
    {
        var sql = $@"
            SELECT {SqlCabeceraCompleta}, {SqlDetalle}
            FROM resumendiario r
            LEFT JOIN resumendiariodetalle d ON d.resumenID = r.resumenID";

        var dict = new Dictionary<int, ResumenComprobante>();
        await _connection.QueryAsync<ResumenComprobante, ResumenComprobanteDetalle, ResumenComprobante>(
            sql, BuildMultiMap(dict),
            splitOn: "ResumenComprobanteDetalleId",
            transaction: _transaction);

        return dict.Values;
    }

    // ── 2. OBTENER POR ID ────────────────────────────────────────────────────
    public async Task<ResumenComprobante?> GetResumenComprobanteByIdAsync(int id)
    {
        var sql = $@"
            SELECT {SqlCabeceraCompleta}, {SqlDetalle}
            FROM resumendiario r
            LEFT JOIN resumendiariodetalle d ON d.resumenID = r.resumenID
            WHERE r.resumenID = @Id";

        var dict = new Dictionary<int, ResumenComprobante>();
        await _connection.QueryAsync<ResumenComprobante, ResumenComprobanteDetalle, ResumenComprobante>(
            sql, BuildMultiMap(dict),
            param: new { Id = id },
            splitOn: "ResumenComprobanteDetalleId",
            transaction: _transaction);

        return dict.Values.FirstOrDefault();
    }

    // ── 3. LISTADO CON FILTROS ────────────────────────────────────────────────
    public async Task<IEnumerable<ResumenComprobante>> GetResumenesByFiltroAsync(
        string ruc,
        string? establecimiento,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        int page = 1,
        int limit = 50)
    {
        const string sql = @"
            SELECT
                r.resumenID                 AS ResumenComprobanteId,
                r.empresaID                 AS EmpresaId,
                r.empresaRuc                AS EmpresaRuc,
                r.empresaRazonSocial        AS EmpresaRazonSocial,
                r.establecimientoAnexo      AS EstablecimientoAnexo,
                r.numeroEnvio               AS NumeroEnvio,
                r.fechaEmisionDocumentos    AS FechaEmisionDocumentos,
                r.fechaGeneracion           AS FechaGeneracion,
                r.identificador             AS Identificador,
                r.estadoSunat               AS EstadoSunat,
                r.ticket                    AS Ticket,
                r.codigoRespuesta           AS CodigoRespuesta,
                r.mensajeRespuesta          AS MensajeRespuesta,
                r.fechaEnvio                AS FechaEnvio,
                r.usuarioCreacion           AS UsuarioCreacion
            FROM resumendiario r
            WHERE r.empresaRuc = @Ruc
              AND (@Establecimiento IS NULL OR r.establecimientoAnexo = @Establecimiento)
              AND (@FechaDesde IS NULL OR r.fechaEmisionDocumentos >= @FechaDesde)
              AND (@FechaHasta IS NULL OR r.fechaEmisionDocumentos <= @FechaHasta)
            ORDER BY r.fechaGeneracion DESC
            LIMIT @Limit OFFSET @Offset";

        return await _connection.QueryAsync<ResumenComprobante>(sql, new
        {
            Ruc             = ruc,
            Establecimiento = establecimiento,
            FechaDesde      = fechaDesde?.Date,
            FechaHasta      = fechaHasta?.Date,
            Limit           = limit,
            Offset          = (page - 1) * limit
        }, _transaction);
    }

    // ── 4. DETALLE CON COMPROBANTES ───────────────────────────────────────────
    public async Task<ResumenComprobante?> GetResumenConDetallesAsync(int idResumen)
    {
        var sql = $@"
            SELECT {SqlCabeceraCompleta}, {SqlDetalle}
            FROM resumendiario r
            LEFT JOIN resumendiariodetalle d ON d.resumenID = r.resumenID
            WHERE r.resumenID = @IdResumen";

        var dict = new Dictionary<int, ResumenComprobante>();
        await _connection.QueryAsync<ResumenComprobante, ResumenComprobanteDetalle, ResumenComprobante>(
            sql, BuildMultiMap(dict),
            param: new { IdResumen = idResumen },
            splitOn: "ResumenComprobanteDetalleId",
            transaction: _transaction);

        return dict.Values.FirstOrDefault();
    }

    // ── 5. PRÓXIMO NÚMERO DE ENVÍO ────────────────────────────────────────────
    public async Task<int> GetProximoNumeroEnvioAsync(string ruc, string establecimiento, DateTime fecha)
    {
        const string sql = @"
            SELECT COALESCE(MAX(numeroEnvio), 0) + 1
            FROM resumendiario
            WHERE empresaRuc            = @Ruc
              AND establecimientoAnexo  = @Establecimiento
              AND fechaEmisionDocumentos = @Fecha";

        return await _connection.ExecuteScalarAsync<int>(sql, new
        {
            Ruc             = ruc,
            Establecimiento = establecimiento,
            Fecha           = fecha.Date
        }, _transaction);
    }

    // ── 6. REGISTRAR ─────────────────────────────────────────────────────────
    public async Task<int> RegistrarResumenComprobanteAsync(ResumenComprobante resumen)
    {
        const string sql = @"
            INSERT INTO resumendiario (
                empresaID, empresaRuc, empresaRazonSocial,
                empresaDireccion, empresaProvincia, empresaDepartamento,
                empresaDistrito, empresaUbigeo, establecimientoAnexo,
                numeroEnvio, fechaEmisionDocumentos, fechaGeneracion,
                identificador, estadoSunat, ticket,
                codigoRespuesta, mensajeRespuesta, xmlGenerado,
                pdfGenerado, fechaEnvio, estado, usuarioCreacion
            ) VALUES (
                @EmpresaId, @EmpresaRuc, @EmpresaRazonSocial,
                @EmpresaDireccion, @EmpresaProvincia, @EmpresaDepartamento,
                @EmpresaDistrito, @EmpresaUbigeo, @EstablecimientoAnexo,
                @NumeroEnvio, @FechaEmisionDocumentos, @FechaGeneracion,
                @Identificador, @EstadoSunat, @Ticket,
                @CodigoRespuesta, @MensajeRespuesta, @XmlGenerado,
                @PdfGenerado, @FechaEnvio, @Estado, @UsuarioCreacion
            );
            SELECT LAST_INSERT_ID();";

        int resumenId = await _connection.ExecuteScalarAsync<int>(sql, new
        {
            resumen.EmpresaId,
            resumen.EmpresaRuc,
            resumen.EmpresaRazonSocial,
            resumen.EmpresaDireccion,
            resumen.EmpresaProvincia,
            resumen.EmpresaDepartamento,
            resumen.EmpresaDistrito,
            resumen.EmpresaUbigeo,
            resumen.EstablecimientoAnexo,
            resumen.NumeroEnvio,
            FechaEmisionDocumentos = resumen.FechaEmisionDocumentos.Date,
            FechaGeneracion        = resumen.FechaGeneracion.Date,
            resumen.Identificador,
            resumen.EstadoSunat,
            resumen.Ticket,
            resumen.CodigoRespuesta,
            resumen.MensajeRespuesta,
            resumen.XmlGenerado,
            resumen.PdfGenerado,
            resumen.FechaEnvio,
            resumen.Estado,
            resumen.UsuarioCreacion
        }, _transaction);

        foreach (var detalle in resumen.DetallesResumen)
        {
            detalle.ResumenComprobanteId = resumenId;
            await RegistrarDetalleAsync(detalle);
        }

        return resumenId;
    }

    // ── 7. REGISTRAR DETALLE ─────────────────────────────────────────────────
    private async Task RegistrarDetalleAsync(ResumenComprobanteDetalle d)
    {
        const string sql = @"
            INSERT INTO resumendiariodetalle (
                resumenID, lineaId, comprobanteId, tipoComprobanteId,
                serie, correlativo,
                clienteTipoDoc, clienteNumDoc, clienteNombre,
                documentoAfectadoTipo, documentoAfectadoNumero,
                codigoCondicion, moneda, montoTotalVenta,
                totalGrabado, totalExonerado, totalInafecto,
                totalGratuito, totalIGV, igvReferencial
            ) VALUES (
                @ResumenComprobanteId, @LineID, @ComprobanteId, @TipoComprobante,
                @Serie, @Correlativo,
                @ClienteTipoDoc, @ClienteNumDoc, @ClienteNombre,
                @DocumentoAfectadoTipo, @DocumentoAfectadoNumero,
                @CodigoCondicion, @Moneda, @MontoTotalVenta,
                @TotalGravado, @TotalExonerado, @TotalInafecto,
                @TotalGratuito, @TotalIGV, @IGVReferencial
            );";

        await _connection.ExecuteAsync(sql, d, _transaction);
    }

    // ── 8. EXISTE IDENTIFICADOR ──────────────────────────────────────────────
    public async Task<bool> ExisteIdentificadorAsync(string identificador)
    {
        const string sql = @"
            SELECT COUNT(1) FROM resumendiario WHERE identificador = @Identificador";

        var count = await _connection.ExecuteScalarAsync<int>(
            sql, new { Identificador = identificador }, _transaction);

        return count > 0;
    }

    // ── 9. ACTUALIZAR ESTADO ─────────────────────────────────────────────────
    public async Task UpdateEstadoResumenAsync(int resumenId, string estado, string ticket,
        string codigoRespuesta, string mensajeRespuesta, string xmlGenerado, DateTime fechaEnvio)
    {
        const string sql = @"
            UPDATE resumendiario SET
                estadoSunat      = @Estado,
                ticket           = @Ticket,
                codigoRespuesta  = @CodigoRespuesta,
                mensajeRespuesta = @MensajeRespuesta,
                xmlGenerado      = @XmlGenerado,
                fechaEnvio       = @FechaEnvio
            WHERE resumenID = @ResumenId";

        await _connection.ExecuteAsync(sql, new
        {
            ResumenId        = resumenId,
            Estado           = estado,
            Ticket           = ticket,
            CodigoRespuesta  = codigoRespuesta,
            MensajeRespuesta = mensajeRespuesta,
            XmlGenerado      = xmlGenerado,
            FechaEnvio       = fechaEnvio
        }, _transaction);
    }
}