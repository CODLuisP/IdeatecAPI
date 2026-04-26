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
        DateTime? desde,
        DateTime? hasta,
        int limite)
    {
        return await CalcularDashboard(
            whereBase: "c.empresaRuc = @Ruc",
            parametrosBase: new { Ruc = ruc },
            desde, hasta, limite
        );
    }

    public async Task<DashboardResponseDto> GetDashboardPorSucursalAsync(
        int sucursalId,
        DateTime? desde,
        DateTime? hasta,
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
            desde, hasta, limite
        );
    }

    private async Task<DashboardResponseDto> CalcularDashboard(
        string whereBase,
        object parametrosBase,
        DateTime? desde,
        DateTime? hasta,
        int limite)
    {
        var estadosExcluidos = string.Join("','", EstadosExcluidos);
        var whereEstados = $"c.estadoSunat NOT IN ('{estadosExcluidos}')";

        var hoy = DateTime.Today;
        var hace7Dias = hoy.AddDays(-6);

        // ── Parámetros para ventas del día ──
        var dpHoy = new DynamicParameters(parametrosBase);
        dpHoy.Add("Hoy", hoy);

        // ── Parámetros para conteos con rango ──
        var dpRango = new DynamicParameters(parametrosBase);
        dpRango.Add("Desde", desde.HasValue ? desde.Value.Date : (DateTime?)null);
        dpRango.Add("Hasta", hasta.HasValue ? hasta.Value.Date.AddDays(1).AddTicks(-1) : (DateTime?)null);

        // ── Parámetros para rendimiento (siempre últimos 7 días) ──
        var dpRendimiento = new DynamicParameters(parametrosBase);
        dpRendimiento.Add("Hace7Dias", hace7Dias);
        dpRendimiento.Add("Hoy", hoy);

        // ── Parámetros para recientes ──
        var dpRecientes = new DynamicParameters(parametrosBase);
        dpRecientes.Add("Limite", limite);

        // Filtro de rango de fechas para conteos
        var whereRango = whereEstados;
        if (desde.HasValue)
            whereRango += " AND c.fechaEmision >= @Desde";
        if (hasta.HasValue)
            whereRango += " AND c.fechaEmision <= @Hasta";

        // ── Ventas del día (siempre hoy) ──
        var sqlVentasDelDia = $@"
            SELECT COALESCE(SUM(
                CASE WHEN c.tipoMoneda = 'USD' THEN c.importeTotal * c.tipoCambio
                     ELSE c.importeTotal END
            ), 0)
            FROM comprobante c
            WHERE {whereBase}
              AND {whereEstados}
              AND c.tipoComprobante IN ('01','03')
              AND c.fechaEmision = @Hoy;";

        var ventasDelDia = await _connection.ExecuteScalarAsync<decimal>(
            sqlVentasDelDia, dpHoy, _transaction);

        // ── Facturas emitidas ──
        var sqlFacturas = $@"
            SELECT COUNT(*) FROM comprobante c
            WHERE {whereBase} AND {whereRango}
              AND c.tipoComprobante = '01';";

        var facturasEmitidas = await _connection.ExecuteScalarAsync<int>(
            sqlFacturas, dpRango, _transaction);

        // ── Boletas emitidas ──
        var sqlBoletas = $@"
            SELECT COUNT(*) FROM comprobante c
            WHERE {whereBase} AND {whereRango}
              AND c.tipoComprobante = '03';";

        var boletasEmitidas = await _connection.ExecuteScalarAsync<int>(
            sqlBoletas, dpRango, _transaction);

        // ── Notas de crédito ──
        var sqlNC = $@"
            SELECT COUNT(*) FROM comprobante c
            WHERE {whereBase} AND {whereRango}
              AND c.tipoComprobante = '07';";

        var notasCreditoEmitidas = await _connection.ExecuteScalarAsync<int>(
            sqlNC, dpRango, _transaction);

        // ── Notas de débito ──
        var sqlND = $@"
            SELECT COUNT(*) FROM comprobante c
            WHERE {whereBase} AND {whereRango}
              AND c.tipoComprobante = '08';";

        var notasDebitoEmitidas = await _connection.ExecuteScalarAsync<int>(
            sqlND, dpRango, _transaction);

        // ── Rendimiento siempre últimos 7 días ──
        var sqlRendimiento = $@"
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
            ORDER BY c.fechaEmision ASC;";

        var rendimientoVentas = (await _connection.QueryAsync<RendimientoVentasDto>(
            sqlRendimiento, dpRendimiento, _transaction)).ToList();

        // ── Comprobantes recientes ──
        var sqlRecientes = $@"
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
            WHERE {whereBase} AND {whereEstados}
            ORDER BY c.fechaEmision DESC, c.horaEmision DESC
            LIMIT @Limite;";

        var comprobantesRecientes = (await _connection.QueryAsync<ComprobanteRecienteDto>(
            sqlRecientes, dpRecientes, _transaction)).ToList();

        return new DashboardResponseDto
        {
            VentasDelDia         = ventasDelDia,
            FacturasEmitidas     = facturasEmitidas,
            BoletasEmitidas      = boletasEmitidas,
            NotasCreditoEmitidas = notasCreditoEmitidas,
            NotasDebitoEmitidas  = notasDebitoEmitidas,
            RendimientoVentas    = rendimientoVentas,
            ComprobantesRecientes = comprobantesRecientes
        };
    }
}