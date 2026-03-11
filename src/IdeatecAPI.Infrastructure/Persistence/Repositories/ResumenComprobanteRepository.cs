using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class ResumenComprobanteRepository : DapperRepository<ResumenComprobante>, IResumenComprobanteRepository
{
    public ResumenComprobanteRepository(IDbConnection connection, IDbTransaction? transaction = null) 
        : base(connection, transaction)
    {
    }

    // ── 1. OBTENER TODOS CON SUS DETALLES ────────────────────────────────────
    public async Task<IEnumerable<ResumenComprobante>> GetAllResumenComprobanteAsync()
    {
        const string sql = @"
            SELECT
                r.resumenID                     AS ResumenComprobanteId,
                r.empresaID                     AS EmpresaId,
                r.empresaRuc                    AS EmpresaRuc,
                r.empresaRazonSocial            AS EmpresaRazonSocial,
                r.numeroEnvio                   AS NumeroEnvio,
                r.fechaEmisionDocumentos        AS FechaEmisionDocumentos,
                r.fechaGeneracion               AS FechaGeneracion,
                r.identificador                 AS Identificador,
                r.estadoSunat                   AS EstadoSunat,
                r.ticket                        AS Ticket,
                r.codigoRespuesta               AS CodigoRespuesta,
                r.mensajeRespuesta              AS MensajeRespuesta,
                r.xmlGenerado                   AS XmlGenerado,
                r.fechaEnvio                    AS FechaEnvio,
                r.estado                        AS Estado,
                d.resumendiariodetalleID        AS ResumenComprobanteDetalleId,
                d.lineaId                       AS LineID,
                d.comprobanteId                 AS ComprobanteId,
                d.resumenID                     AS ResumenComprobanteId,
                d.tipoComprobanteId             AS TipoComprobante,
                d.serie                         AS Serie,
                d.correlativo                   AS Correlativo,
                d.codigoCondicion               AS CodigoCondicion,
                d.moneda                        AS Moneda,
                d.montoTotalVenta               AS MontoTotalVenta,
                d.totalGrabado                  AS TotalGravado,
                d.totalExonerado                AS TotalExonerado,
                d.totalInafecto                 AS TotalInafecto,
                d.totalGratuito                 AS TotalGratuito,
                d.totalIGV                      AS TotalIGV,
                d.igvReferencial                AS IGVReferencial
            FROM resumendiario r
            LEFT JOIN resumendiariodetalle d ON d.resumenID = r.resumenID";

        var resumenDict = new Dictionary<int, ResumenComprobante>();

        await _connection.QueryAsync<ResumenComprobante, ResumenComprobanteDetalle, ResumenComprobante>(
            sql,
            (resumen, detalle) =>
            {
                if (!resumenDict.TryGetValue(resumen.ResumenComprobanteId, out var resumenEntry))
                {
                    resumenEntry = resumen;
                    resumenEntry.DetallesResumen = [];
                    resumenDict.Add(resumenEntry.ResumenComprobanteId, resumenEntry);
                }

                if (detalle is not null)
                    resumenEntry.DetallesResumen.Add(detalle);

                return resumenEntry;
            },
            splitOn: "ResumenComprobanteDetalleId",
            transaction: _transaction
        );

        return resumenDict.Values;
    }

    // ── 2. OBTENER POR ID CON SUS DETALLES ───────────────────────────────────
    public async Task<ResumenComprobante?> GetResumenComprobanteByIdAsync(int id)
    {
        const string sql = @"
            SELECT
                r.resumenID                     AS ResumenComprobanteId,
                r.empresaID                     AS EmpresaId,
                r.empresaRuc                    AS EmpresaRuc,
                r.empresaRazonSocial            AS EmpresaRazonSocial,
                r.numeroEnvio                   AS NumeroEnvio,
                r.fechaEmisionDocumentos        AS FechaEmisionDocumentos,
                r.fechaGeneracion               AS FechaGeneracion,
                r.identificador                 AS Identificador,
                r.estadoSunat                   AS EstadoSunat,
                r.ticket                        AS Ticket,
                r.codigoRespuesta               AS CodigoRespuesta,
                r.mensajeRespuesta              AS MensajeRespuesta,
                r.xmlGenerado                   AS XmlGenerado,
                r.fechaEnvio                    AS FechaEnvio,
                r.estado                        AS Estado,
                d.resumendiariodetalleID        AS ResumenComprobanteDetalleId,
                d.lineaId                       AS LineID,
                d.comprobanteId                 AS ComprobanteId,
                d.resumenID                     AS ResumenComprobanteId,
                d.tipoComprobanteId             AS TipoComprobante,
                d.serie                         AS Serie,
                d.correlativo                   AS Correlativo,
                d.codigoCondicion               AS CodigoCondicion,
                d.moneda                        AS Moneda,
                d.montoTotalVenta               AS MontoTotalVenta,
                d.totalGrabado                  AS TotalGravado,
                d.totalExonerado                AS TotalExonerado,
                d.totalInafecto                 AS TotalInafecto,
                d.totalGratuito                 AS TotalGratuito,
                d.totalIGV                      AS TotalIGV,
                d.igvReferencial                AS IGVReferencial
            FROM resumendiario r
            LEFT JOIN resumendiariodetalle d ON d.resumenID = r.resumenID
            WHERE r.resumenID = @Id";

        ResumenComprobante? resumenEntry = null;

        await _connection.QueryAsync<ResumenComprobante, ResumenComprobanteDetalle, ResumenComprobante>(
            sql,
            (resumen, detalle) =>
            {
                resumenEntry ??= resumen;
                resumenEntry.DetallesResumen ??= [];

                if (detalle is not null)
                    resumenEntry.DetallesResumen.Add(detalle);

                return resumenEntry;
            },
            param: new { Id = id },
            splitOn: "ResumenComprobanteDetalleId",
            transaction: _transaction
        );

        return resumenEntry;
    }

    public async Task<int> RegistrarResumenComprobanteAsync(ResumenComprobante resumen)
    {
        const string sql = @"
            INSERT INTO resumendiario (
                empresaID, empresaRuc, empresaRazonSocial,
                numeroEnvio, fechaEmisionDocumentos, fechaGeneracion,
                identificador, estadoSunat, ticket,
                codigoRespuesta, mensajeRespuesta, xmlGenerado,
                fechaEnvio, estado
            ) VALUES (
                @EmpresaId, @EmpresaRuc, @EmpresaRazonSocial,
                @NumeroEnvio, @FechaEmisionDocumentos, @FechaGeneracion,
                @Identificador, @EstadoSunat, @Ticket,
                @CodigoRespuesta, @MensajeRespuesta, @XmlGenerado,
                @FechaEnvio, @Estado
            );
            SELECT LAST_INSERT_ID();";

        var parameters = new
        {
            resumen.EmpresaId,
            resumen.EmpresaRuc,
            resumen.EmpresaRazonSocial,
            resumen.NumeroEnvio,
            FechaEmisionDocumentos = resumen.FechaEmisionDocumentos.Date,
            FechaGeneracion        = resumen.FechaGeneracion.Date,
            resumen.Identificador,
            resumen.EstadoSunat,
            resumen.Ticket,
            resumen.CodigoRespuesta,
            resumen.MensajeRespuesta,
            resumen.XmlGenerado,
            resumen.FechaEnvio,
            resumen.Estado
        };

        int resumenId = await _connection.ExecuteScalarAsync<int>(sql, parameters, _transaction);

        foreach (var detalle in resumen.DetallesResumen)
        {
            detalle.ResumenComprobanteId = resumenId;
            await RegistrarDetalleAsync(detalle);
        }

        return resumenId;
    }

    private async Task RegistrarDetalleAsync(ResumenComprobanteDetalle d)
    {
        const string sql = @"
            INSERT INTO resumendiariodetalle (
                resumenID, lineaId, comprobanteId, tipoComprobanteId,
                serie, correlativo, codigoCondicion,
                moneda, montoTotalVenta, totalGrabado, totalExonerado,
                totalInafecto, totalGratuito, totalIGV, igvReferencial
            ) VALUES (
                @ResumenComprobanteId, @LineID, @ComprobanteId, @TipoComprobante,
                @Serie, @Correlativo, @CodigoCondicion,
                @Moneda, @MontoTotalVenta, @TotalGravado, @TotalExonerado,
                @TotalInafecto, @TotalGratuito, @TotalIGV, @IGVReferencial
            );";

        await _connection.ExecuteAsync(sql, d, _transaction);
    }

    public async Task<bool> ExisteIdentificadorAsync(string identificador)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM resumendiario 
            WHERE identificador = @Identificador";

        var count = await _connection.ExecuteScalarAsync<int>(
            sql, 
            new { Identificador = identificador }, 
            _transaction
        );

        return count > 0;
    }

    // ResumenComprobanteRepository
    public async Task UpdateEstadoSunatAsync(int resumenId, string estado, string ticket,
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