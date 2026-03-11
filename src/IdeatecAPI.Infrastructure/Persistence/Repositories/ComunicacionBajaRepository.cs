using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class ComunicacionBajaRepository : IComunicacionBajaRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public ComunicacionBajaRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection  = connection;
        _transaction = transaction;
    }

    public async Task<IEnumerable<ComunicacionBaja>> GetAllAsync(int empresaId)
    {
        var sql = @"SELECT 
                        bajaId          AS BajaId,
                        empresaId       AS EmpresaId,
                        correlativo     AS Correlativo,
                        fecGeneracion   AS FecGeneracion,
                        fecComunicacion AS FecComunicacion,
                        empresaRuc              AS EmpresaRuc,
                        empresaRazonSocial      AS EmpresaRazonSocial,
                        empresaNombreComercial  AS EmpresaNombreComercial,
                        empresaDireccion        AS EmpresaDireccion,
                        empresaProvincia        AS EmpresaProvincia,
                        empresaDepartamento     AS EmpresaDepartamento,
                        empresaDistrito         AS EmpresaDistrito,
                        empresaUbigeo           AS EmpresaUbigeo,
                        estadoSunat             AS EstadoSunat,
                        codigoRespuestaSunat    AS CodigoRespuestaSunat,
                        mensajeRespuestaSunat   AS MensajeRespuestaSunat,
                        ticketSunat             AS TicketSunat,
                        xmlEnviado              AS XmlEnviado,
                        cdrBase64               AS CdrBase64,
                        fechaCreacion           AS FechaCreacion,
                        fechaEnvioSunat         AS FechaEnvioSunat
                    FROM comunicacionBaja
                    WHERE empresaId = @EmpresaId
                    ORDER BY fechaCreacion DESC";
        return await _connection.QueryAsync<ComunicacionBaja>(sql, new { EmpresaId = empresaId }, _transaction);
    }

    public async Task<ComunicacionBaja?> GetByIdAsync(int bajaId)
    {
        var sql = @"SELECT 
                        bajaId          AS BajaId,
                        empresaId       AS EmpresaId,
                        correlativo     AS Correlativo,
                        fecGeneracion   AS FecGeneracion,
                        fecComunicacion AS FecComunicacion,
                        empresaRuc              AS EmpresaRuc,
                        empresaRazonSocial      AS EmpresaRazonSocial,
                        empresaNombreComercial  AS EmpresaNombreComercial,
                        empresaDireccion        AS EmpresaDireccion,
                        empresaProvincia        AS EmpresaProvincia,
                        empresaDepartamento     AS EmpresaDepartamento,
                        empresaDistrito         AS EmpresaDistrito,
                        empresaUbigeo           AS EmpresaUbigeo,
                        estadoSunat             AS EstadoSunat,
                        codigoRespuestaSunat    AS CodigoRespuestaSunat,
                        mensajeRespuestaSunat   AS MensajeRespuestaSunat,
                        ticketSunat             AS TicketSunat,
                        xmlEnviado              AS XmlEnviado,
                        cdrBase64               AS CdrBase64,
                        fechaCreacion           AS FechaCreacion,
                        fechaEnvioSunat         AS FechaEnvioSunat
                    FROM comunicacionBaja
                    WHERE bajaId = @BajaId";
        return await _connection.QueryFirstOrDefaultAsync<ComunicacionBaja>(sql, new { BajaId = bajaId }, _transaction);
    }

    public async Task<int> CreateAsync(ComunicacionBaja baja)
    {
        var sql = @"INSERT INTO comunicacionBaja (
                        empresaId, correlativo, fecGeneracion, fecComunicacion,
                        empresaRuc, empresaRazonSocial, empresaNombreComercial,
                        empresaDireccion, empresaProvincia, empresaDepartamento,
                        empresaDistrito, empresaUbigeo,
                        estadoSunat, fechaCreacion
                    ) VALUES (
                        @EmpresaId, @Correlativo, @FecGeneracion, @FecComunicacion,
                        @EmpresaRuc, @EmpresaRazonSocial, @EmpresaNombreComercial,
                        @EmpresaDireccion, @EmpresaProvincia, @EmpresaDepartamento,
                        @EmpresaDistrito, @EmpresaUbigeo,
                        @EstadoSunat, @FechaCreacion
                    );
                    SELECT LAST_INSERT_ID();";
        return await _connection.ExecuteScalarAsync<int>(sql, baja, _transaction);
    }

    public async Task UpdateEstadoAsync(int bajaId, string estado, string? codigo, string? mensaje, string? ticket, string? xml, string? cdr)
    {
        var sql = @"UPDATE comunicacionBaja SET
                        estadoSunat           = @Estado,
                        codigoRespuestaSunat  = @Codigo,
                        mensajeRespuestaSunat = @Mensaje,
                        ticketSunat           = @Ticket,
                        xmlEnviado            = @Xml,
                        cdrBase64             = @Cdr,
                        fechaEnvioSunat       = @FechaEnvio
                    WHERE bajaId = @BajaId";
        await _connection.ExecuteAsync(sql, new
        {
            BajaId     = bajaId,
            Estado     = estado,
            Codigo     = codigo,
            Mensaje    = mensaje,
            Ticket     = ticket,
            Xml        = xml,
            Cdr        = cdr,
            FechaEnvio = DateTime.UtcNow
        }, _transaction);
    }
}