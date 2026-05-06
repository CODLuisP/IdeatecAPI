using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Dashboard.DTOs;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    private static readonly string[] EstadosExcluidos = { "ANULADO", "RECHAZADO" };

    public DashboardRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<DashboardResponseDto> GetDashboardPorEmpresaAsync(
        string ruc,
        DateTime? fecha,
        int limite)
    {
        return await CalcularDashboard(
            whereBase: "c.empresaRuc = @Ruc",
            parametrosBase: new { Ruc = ruc },
            fecha, limite
        );
    }

    public async Task<DashboardResponseDto> GetDashboardPorSucursalAsync(
        int sucursalId,
        DateTime? fecha,
        int limite)
    {
        var sqlSucursal = @"
            SELECT empresaRuc, codEstablecimiento
            FROM sucursal
            WHERE sucursalID = @SucursalId AND estado = 1
            LIMIT 1;";

        var sucursal = await _connection.QueryFirstOrDefaultAsync<(string Ruc, string CodEstablecimiento)>(
            sqlSucursal,
            new { SucursalId = sucursalId },
            _transaction
        );

        if (sucursal == default)
            throw new KeyNotFoundException($"Sucursal {sucursalId} no encontrada o inactiva.");

        return await CalcularDashboard(
            whereBase: "c.empresaRuc = @Ruc AND c.establecimientoAnexo = @CodEstablecimiento",
            parametrosBase: new { Ruc = sucursal.Ruc, CodEstablecimiento = sucursal.CodEstablecimiento },
            fecha, limite
        );
    }

    private async Task<DashboardResponseDto> CalcularDashboard(
        string whereBase,
        object parametrosBase,
        DateTime? fecha,
        int limite)
    {
        var estadosExcluidos = string.Join("','", EstadosExcluidos);
        var whereEstados = $"c.estadoSunat NOT IN ('{estadosExcluidos}')";

        var hoy = fecha.HasValue ? fecha.Value.Date : DateTime.Today;
        var hace7Dias = hoy.AddDays(-6);

        // ── Parámetros unificados para las 3 queries ──────────────────────────
        var dp = new DynamicParameters(parametrosBase);
        dp.Add("Hoy", hoy);
        dp.Add("Hace7Dias", hace7Dias);
        dp.Add("Limite", limite);

        // ═════════════════════════════════════════════════════════════════════
        // 1 sola llamada QueryMultiple = 1 roundtrip a la BD (antes eran 7)
        // ═════════════════════════════════════════════════════════════════════
        var sql = $@"
            -- KPIs del día: 5 queries anteriores unificadas en 1 con SUM+CASE
            SELECT
                COALESCE(SUM(
                    CASE WHEN c.tipoComprobante IN ('01','03')
                         THEN CASE WHEN c.tipoMoneda = 'USD'
                                   THEN c.importeTotal * c.tipoCambio
                                   ELSE c.importeTotal END
                         ELSE 0 END
                ), 0) AS VentasDelDia,
                SUM(CASE WHEN c.tipoComprobante = '01' THEN 1 ELSE 0 END) AS FacturasEmitidas,
                SUM(CASE WHEN c.tipoComprobante = '03' THEN 1 ELSE 0 END) AS BoletasEmitidas,
                SUM(CASE WHEN c.tipoComprobante = '07' THEN 1 ELSE 0 END) AS NotasCreditoEmitidas,
                SUM(CASE WHEN c.tipoComprobante = '08' THEN 1 ELSE 0 END) AS NotasDebitoEmitidas
            FROM comprobante c
            WHERE {whereBase}
              AND {whereEstados}
              AND c.fechaEmision = @Hoy;

            -- Rendimiento últimos 7 días
            SELECT
                c.fechaEmision AS Fecha,
                COALESCE(SUM(
                    CASE WHEN c.tipoMoneda = 'USD' THEN c.importeTotal * c.tipoCambio
                         ELSE c.importeTotal END
                ), 0) AS TotalVentas
            FROM comprobante c
            WHERE {whereBase}
              AND {whereEstados}
              AND c.tipoComprobante IN ('01','03')
              AND c.fechaEmision >= @Hace7Dias
              AND c.fechaEmision <= @Hoy
            GROUP BY c.fechaEmision
            ORDER BY c.fechaEmision ASC;

            -- Comprobantes recientes
            SELECT
                c.comprobanteID    AS ComprobanteID,
                c.numeroCompleto   AS NumeroCompleto,
                c.tipoComprobante  AS TipoComprobante,
                c.clienteRznSocial AS ClienteRznSocial,
                c.fechaEmision     AS FechaEmision,
                CASE WHEN c.tipoMoneda = 'USD' THEN c.importeTotal * c.tipoCambio
                     ELSE c.importeTotal END AS ImporteTotal,
                c.estadoSunat      AS EstadoSunat
            FROM comprobante c
            WHERE {whereBase}
              AND {whereEstados}
              AND c.fechaEmision <= @Hoy
            ORDER BY c.fechaEmision DESC, c.horaEmision DESC
            LIMIT @Limite;";

        using var multi = await _connection.QueryMultipleAsync(sql, dp, _transaction);

        var kpiDia              = await multi.ReadFirstOrDefaultAsync<KpiDiaDto>() ?? new KpiDiaDto();
        var rendimientoVentas   = (await multi.ReadAsync<RendimientoVentasDto>()).ToList();
        var comprobantesRecientes = (await multi.ReadAsync<ComprobanteRecienteDto>()).ToList();

        return new DashboardResponseDto
        {
            VentasDelDia          = kpiDia.VentasDelDia,
            FacturasEmitidas      = kpiDia.FacturasEmitidas,
            BoletasEmitidas       = kpiDia.BoletasEmitidas,
            NotasCreditoEmitidas  = kpiDia.NotasCreditoEmitidas,
            NotasDebitoEmitidas   = kpiDia.NotasDebitoEmitidas,
            RendimientoVentas     = rendimientoVentas,
            ComprobantesRecientes = comprobantesRecientes
        };
    }
}

// ── DTO interno para los KPIs del día (5 métricas en 1 query) ─────────────────
internal class KpiDiaDto
{
    public decimal VentasDelDia        { get; set; }
    public int     FacturasEmitidas    { get; set; }
    public int     BoletasEmitidas     { get; set; }
    public int     NotasCreditoEmitidas { get; set; }
    public int     NotasDebitoEmitidas  { get; set; }
}