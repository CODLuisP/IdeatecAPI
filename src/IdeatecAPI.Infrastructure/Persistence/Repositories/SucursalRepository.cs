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
        SELECT sucursalID                        AS SucursalId,
               empresaRuc                        AS EmpresaRuc,
               codEstablecimiento                AS CodEstablecimiento,
               nombre                            AS Nombre,
               direccion                         AS Direccion,
               serieFactura                      AS SerieFactura,
               correlativoFactura                AS CorrelativoFactura,
               serieBoleta                       AS SerieBoleta,
               correlativoBoleta                 AS CorrelativoBoleta,
               serieNotaCreditoFactura           AS SerieNotaCreditoFactura,
               correlativoNotaCreditoFactura     AS CorrelativoNotaCreditoFactura,
               serieNotaCreditoBoleta            AS SerieNotaCreditoBoleta,
               correlativoNotaCreditoBoleta      AS CorrelativoNotaCreditoBoleta,
               serieNotaDebitoFactura            AS SerieNotaDebitoFactura,
               correlativoNotaDebitoFactura      AS CorrelativoNotaDebitoFactura,
               serieNotaDebitoBoleta             AS SerieNotaDebitoBoleta,
               correlativoNotaDebitoBoleta       AS CorrelativoNotaDebitoBoleta,
               serieGuiaRemision                 AS SerieGuiaRemision,
               correlativoGuiaRemision           AS CorrelativoGuiaRemision,
               serieGuiaTransportista            AS SerieGuiaTransportista,
               correlativoGuiaTransportista      AS CorrelativoGuiaTransportista,
               estado                            AS Estado
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

    public async Task<IEnumerable<Sucursal>> GetByRucSucursalAsync(string empresaRuc, string? sucursalID = null)
    {
        var sql = $"{SelectColumns} AND empresaRuc = @EmpresaRuc";

        if (!string.IsNullOrEmpty(sucursalID))
            sql += " AND sucursalID = @SucursalID";

        return await _connection.QueryAsync<Sucursal>(
            sql, new { EmpresaRuc = empresaRuc, SucursalID = sucursalID }, _transaction);
    }

    public async Task<Sucursal> RegistrarSucursalAsync(Sucursal sucursal)
    {
        var sql = @"
            INSERT INTO sucursal (
                empresaRuc, codEstablecimiento, nombre, direccion,
                serieFactura, correlativoFactura,
                serieBoleta, correlativoBoleta,
                serieNotaCreditoFactura, correlativoNotaCreditoFactura,
                serieNotaCreditoBoleta, correlativoNotaCreditoBoleta,
                serieNotaDebitoFactura, correlativoNotaDebitoFactura,
                serieNotaDebitoBoleta, correlativoNotaDebitoBoleta,
                serieGuiaRemision, correlativoGuiaRemision,
                serieGuiaTransportista, correlativoGuiaTransportista
            ) VALUES (
                @EmpresaRuc, @CodEstablecimiento, @Nombre, @Direccion,
                @SerieFactura, @CorrelativoFactura,
                @SerieBoleta, @CorrelativoBoleta,
                @SerieNotaCreditoFactura, @CorrelativoNotaCreditoFactura,
                @SerieNotaCreditoBoleta, @CorrelativoNotaCreditoBoleta,
                @SerieNotaDebitoFactura, @CorrelativoNotaDebitoFactura,
                @SerieNotaDebitoBoleta, @CorrelativoNotaDebitoBoleta,
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
                nombre                            = @Nombre,
                direccion                         = @Direccion,
                serieFactura                      = @SerieFactura,
                correlativoFactura                = @CorrelativoFactura,
                serieBoleta                       = @SerieBoleta,
                correlativoBoleta                 = @CorrelativoBoleta,
                serieNotaCreditoFactura           = @SerieNotaCreditoFactura,
                correlativoNotaCreditoFactura     = @CorrelativoNotaCreditoFactura,
                serieNotaCreditoBoleta            = @SerieNotaCreditoBoleta,
                correlativoNotaCreditoBoleta      = @CorrelativoNotaCreditoBoleta,
                serieNotaDebitoFactura            = @SerieNotaDebitoFactura,
                correlativoNotaDebitoFactura      = @CorrelativoNotaDebitoFactura,
                serieNotaDebitoBoleta             = @SerieNotaDebitoBoleta,
                correlativoNotaDebitoBoleta       = @CorrelativoNotaDebitoBoleta,
                serieGuiaRemision                 = @SerieGuiaRemision,
                correlativoGuiaRemision           = @CorrelativoGuiaRemision,
                serieGuiaTransportista            = @SerieGuiaTransportista,
                correlativoGuiaTransportista      = @CorrelativoGuiaTransportista
            WHERE sucursalID = @SucursalId AND estado = 1";

        var filas = await _connection.ExecuteAsync(sql, sucursal, _transaction);
        return filas > 0;
    }

    public async Task<bool> EliminarSucursalAsync(int sucursalId)
    {
        var sql = @"DELETE FROM sucursal WHERE sucursalID = @SucursalId";

        var filas = await _connection.ExecuteAsync(sql, new { SucursalId = sucursalId }, _transaction);
        return filas > 0;
    }

    public async Task<bool> EditarInfoAsync(int sucursalId, string? nombre, string? direccion)
    {
        var sql = @"
        UPDATE sucursal SET
            nombre    = COALESCE(@Nombre, nombre),
            direccion = COALESCE(@Direccion, direccion)
        WHERE sucursalID = @SucursalId";

        var filas = await _connection.ExecuteAsync(sql,
            new { Nombre = nombre, Direccion = direccion, SucursalId = sucursalId }, _transaction);
        return filas > 0;
    }
}