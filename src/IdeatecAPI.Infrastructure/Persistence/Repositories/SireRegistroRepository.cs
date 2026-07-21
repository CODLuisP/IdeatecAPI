using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class SireRegistroRepository : DapperRepository<SireRegistro>, ISireRegistroRepository
{
    public SireRegistroRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    public async Task<SireRegistro?> GetByRucAndPeriodoAsync(string rucEmpresa, string perTributario)
    {
        var sql = "SELECT *, sireRegistroID AS Id FROM sire_registro WHERE rucEmpresa = @RucEmpresa AND perTributario = @PerTributario";
        return await _connection.QueryFirstOrDefaultAsync<SireRegistro>(
            sql, new { RucEmpresa = rucEmpresa, PerTributario = perTributario }, _transaction);
    }

    public async Task<IEnumerable<SireRegistro>> GetHistorialByRucAsync(string rucEmpresa)
    {
        var sql = "SELECT *, sireRegistroID AS Id FROM sire_registro WHERE rucEmpresa = @RucEmpresa ORDER BY creadoEn DESC";
        return await _connection.QueryAsync<SireRegistro>(sql, new { RucEmpresa = rucEmpresa }, _transaction);
    }

    public async Task<int> CreateSireRegistroAsync(SireRegistro registro)
    {
        var sql = @"INSERT INTO sire_registro
            (rucEmpresa, perTributario, numTicket, estado, respuestaSunat, mensaje, fechaConsulta, fechaCierre, creadoEn)
            VALUES
            (@RucEmpresa, @PerTributario, @NumTicket, @Estado, @RespuestaSunat, @Mensaje, @FechaConsulta, @FechaCierre, NOW())";

        await _connection.ExecuteAsync(sql, registro, _transaction);
        return await _connection.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID();", param: null, transaction: _transaction);
    }

    public async Task UpdateSireRegistroAsync(SireRegistro registro)
    {
        var sql = @"UPDATE sire_registro SET
            numTicket = @NumTicket,
            estado = @Estado,
            respuestaSunat = @RespuestaSunat,
            mensaje = @Mensaje,
            fechaConsulta = @FechaConsulta,
            fechaCierre = @FechaCierre,
            actualizadoEn = NOW()
            WHERE sireRegistroID = @Id";

        await _connection.ExecuteAsync(sql, registro, _transaction);
    }
}
