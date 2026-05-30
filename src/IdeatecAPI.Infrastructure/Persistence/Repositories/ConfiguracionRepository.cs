using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class ConfiguracionRepository : DapperRepository<Configuracion>, IConfiguracionRepository
{
    public ConfiguracionRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    public async Task<Configuracion?> GetByRucAsync(int ruc)
    {
        var sql = "SELECT * FROM configuracion WHERE ruc = @Ruc";
        return await _connection.QueryFirstOrDefaultAsync<Configuracion>(sql, new { Ruc = ruc }, _transaction);
    }

    public async Task<bool> RegistrarConfiguracionAsync(Configuracion configuracion)
    {
        var sql = @"
            INSERT INTO configuracion
                (ruc, isimprime, tamañoimpresion, igv, isconsumo, guiaremision,
                 iscredito, itemsdefecto, isboletaorfactura, isenvioresumen,
                 isvale, deudascobrar, trabajadores, cargacomprobantes)
            VALUES
                (@Ruc, @IsImprime, @TamañoImpresion, @Igv, @IsConsumo, @GuiaRemision,
                 @IsCredito, @ItemsDefecto, @IsBoletaOrFactura, @IsEnvioResumen,
                 @IsVale, @DeudasCobrar, @Trabajadores, @CargaComprobantes);";

        var result = await _connection.ExecuteAsync(sql, configuracion, _transaction);
        return result > 0;
    }

    public async Task<bool> EditarConfiguracionAsync(int ruc, Configuracion configuracion)
    {
        var sql = @"
            UPDATE configuracion
            SET
                isimprime         = @IsImprime,
                tamañoimpresion   = @TamañoImpresion,
                igv               = @Igv,
                isconsumo         = @IsConsumo,
                guiaremision      = @GuiaRemision,
                iscredito         = @IsCredito,
                itemsdefecto      = @ItemsDefecto,
                isboletaorfactura = @IsBoletaOrFactura,
                isenvioresumen    = @IsEnvioResumen,
                isvale            = @IsVale,
                deudascobrar      = @DeudasCobrar,
                trabajadores      = @Trabajadores,
                cargacomprobantes = @CargaComprobantes
            WHERE ruc = @Ruc;";

        var result = await _connection.ExecuteAsync(sql, new
        {
            configuracion.IsImprime,
            configuracion.TamañoImpresion,
            configuracion.Igv,
            configuracion.IsConsumo,
            configuracion.GuiaRemision,
            configuracion.IsCredito,
            configuracion.ItemsDefecto,
            configuracion.IsBoletaOrFactura,
            configuracion.IsEnvioResumen,
            configuracion.IsVale,
            configuracion.DeudasCobrar,
            configuracion.Trabajadores,
            configuracion.CargaComprobantes,
            Ruc = ruc
        }, _transaction);

        return result > 0;
    }
}
