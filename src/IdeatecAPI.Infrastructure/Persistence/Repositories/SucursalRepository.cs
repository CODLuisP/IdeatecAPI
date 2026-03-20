using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class SucursalRepository : DapperRepository<Sucursal>, ISucursalRepository
{
    public SucursalRepository(IDbConnection connection, IDbTransaction? transaction = null) : base(connection, transaction)
    {
    }

    private const string SelectColumns = @"
        SELECT sucursalID                   AS SucursalId,
               empresaRuc                   AS EmpresaRuc,
               codEstablecimiento           AS CodEstablecimiento,
               serieFactura                 AS SerieFactura,
               correlativoFactura           AS CorrelativoFactura,
               serieBoleta                  AS SerieBoleta,
               correlativoBoleta            AS CorrelativoBoleta,
               serieNotaCredito             AS SerieNotaCredito,
               correlativoNotaCredito       AS CorrelativoNotaCredito,
               serieNotaDebito              AS SerieNotaDebito,
               correlativoNotaDebito        AS CorrelativoNotaDebito,
               serieGuiaRemision            AS SerieGuiaRemision,
               correlativoGuiaRemision      AS CorrelativoGuiaRemision,
               serieGuiaTransportista       AS SerieGuiaTransportista,
               correlativoGuiaTransportista AS CorrelativoGuiaTransportista,
               estado                       AS Estado
        FROM sucursal
        WHERE estado = 1";

    public async Task<IEnumerable<Sucursal>> GetAllSucursalAsync()
    {
        return await _connection.QueryAsync<Sucursal>(SelectColumns, transaction: _transaction);
    }

    public async Task<Sucursal> GetByIdSucursalAsync(int sucursalId)
    {
        var sql = $"{SelectColumns} AND sucursalID = @SucursalId";

        return await _connection.QueryFirstOrDefaultAsync<Sucursal>(sql, new { SucursalId = sucursalId }, _transaction)
               ?? throw new KeyNotFoundException($"Sucursal con ID {sucursalId} no encontrada.");
    }

    public async Task<IEnumerable<Sucursal>> GetByRucSucursalAsync(string empresaRuc)
    {
        var sql = $"{SelectColumns} AND empresaRuc = @EmpresaRuc";

        return await _connection.QueryAsync<Sucursal>(sql, new { EmpresaRuc = empresaRuc }, _transaction);
    }

    public async Task<Sucursal> RegistrarSucursalAsync(Sucursal sucursal)
    {
        var sql = @"
            INSERT INTO sucursal (
                empresaRuc, codEstablecimiento,
                serieFactura, correlativoFactura,
                serieBoleta, correlativoBoleta,
                serieNotaCredito, correlativoNotaCredito,
                serieNotaDebito, correlativoNotaDebito,
                serieGuiaRemision, correlativoGuiaRemision,
                serieGuiaTransportista, correlativoGuiaTransportista
            ) VALUES (
                @EmpresaRuc, @CodEstablecimiento,
                @SerieFactura, @CorrelativoFactura,
                @SerieBoleta, @CorrelativoBoleta,
                @SerieNotaCredito, @CorrelativoNotaCredito,
                @SerieNotaDebito, @CorrelativoNotaDebito,
                @SerieGuiaRemision, @CorrelativoGuiaRemision,
                @SerieGuiaTransportista, @CorrelativoGuiaTransportista
            );
            SELECT LAST_INSERT_ID();";

        var newId = await _connection.ExecuteScalarAsync<int>(sql, sucursal, _transaction);
        sucursal.SucursalId = newId;
        return sucursal;
    }

    public async Task<bool> EditarSucursalAsync(Sucursal sucursal)
    {
        var sql = @"
            UPDATE sucursal SET
                serieFactura                 = @SerieFactura,
                correlativoFactura           = @CorrelativoFactura,
                serieBoleta                  = @SerieBoleta,
                correlativoBoleta            = @CorrelativoBoleta,
                serieNotaCredito             = @SerieNotaCredito,
                correlativoNotaCredito       = @CorrelativoNotaCredito,
                serieNotaDebito              = @SerieNotaDebito,
                correlativoNotaDebito        = @CorrelativoNotaDebito,
                serieGuiaRemision            = @SerieGuiaRemision,
                correlativoGuiaRemision      = @CorrelativoGuiaRemision,
                serieGuiaTransportista       = @SerieGuiaTransportista,
                correlativoGuiaTransportista = @CorrelativoGuiaTransportista
            WHERE sucursalID = @SucursalId AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, sucursal, _transaction);
        return filas > 0;
    }

    public async Task<bool> EliminarSucursalAsync(int sucursalId)
    {
        var sql = @"UPDATE sucursal SET estado = 0 WHERE sucursalID = @SucursalId AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, new { SucursalId = sucursalId }, _transaction);
        return filas > 0;
    }
}