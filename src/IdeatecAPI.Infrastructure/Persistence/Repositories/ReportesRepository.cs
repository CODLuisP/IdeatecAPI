// IdeatecAPI.Infrastructure.Persistence.Repositories/ReportesRepository.cs

using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class ReportesRepository : IReportesRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    private static readonly string[] EstadosExcluidos = { "ANULADO", "RECHAZADO" };

    public ReportesRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PÚBLICOS
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ReporteResponseDto> GetReportesPorEmpresaAsync(
        string ruc, string periodo, DateTime? desde, DateTime? hasta, int limite, int? usuarioId)
    {
        return await CalcularReporte(
            whereBase: "c.empresaRuc = @Ruc",
            parametrosBase: new { Ruc = ruc },
            periodo, desde, hasta, limite, usuarioId);
    }

    public async Task<ReporteResponseDto> GetReportesPorSucursalAsync(
        int sucursalId, string periodo, DateTime? desde, DateTime? hasta, int limite, int? usuarioId)
    {
        var (ruc, cod) = await ObtenerDatosSucursal(sucursalId);
        return await CalcularReporte(
            whereBase: "c.empresaRuc = @Ruc AND c.establecimientoAnexo = @CodEstablecimiento",
            parametrosBase: new { Ruc = ruc, CodEstablecimiento = cod },
            periodo, desde, hasta, limite, usuarioId);
    }

    public async Task<List<ClienteExportDto>> GetClientesExportPorEmpresaAsync(
        string ruc, string periodo, DateTime? desde, DateTime? hasta, int? usuarioId)
    {
        var (fechaDesde, fechaHasta) = ObtenerRango(periodo, desde, hasta);
        return await ConsultarClientesExport(
            whereBase: "c.empresaRuc = @Ruc",
            parametrosBase: new { Ruc = ruc },
            fechaDesde, fechaHasta, usuarioId);
    }

    public async Task<List<ClienteExportDto>> GetClientesExportPorSucursalAsync(
        int sucursalId, string periodo, DateTime? desde, DateTime? hasta, int? usuarioId)
    {
        var (ruc, cod) = await ObtenerDatosSucursal(sucursalId);
        var (fechaDesde, fechaHasta) = ObtenerRango(periodo, desde, hasta);
        return await ConsultarClientesExport(
            whereBase: "c.empresaRuc = @Ruc AND c.establecimientoAnexo = @CodEstablecimiento",
            parametrosBase: new { Ruc = ruc, CodEstablecimiento = cod },
            fechaDesde, fechaHasta, usuarioId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CORE PRIVADO
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<ReporteResponseDto> CalcularReporte(
        string whereBase,
        object parametrosBase,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int limite,
        int? usuarioId)
    {
        var estadosExcluidos = $"'{string.Join("','", EstadosExcluidos)}'";
        var whereEstados = $"c.estadoSunat NOT IN ({estadosExcluidos})";

        var (fechaDesde, fechaHasta) = ObtenerRango(periodo, desde, hasta);
        var (fechaDesdeAnterior, fechaHastaAnterior) = ObtenerRangoAnterior(periodo, fechaDesde, fechaHasta);

        // ── WHERE dinámico ────────────────────────────────────────────────────
        var whereUsuario = usuarioId.HasValue ? "AND c.usuarioCreacion = @UsuarioId" : "";

        // ── Parámetros período actual ─────────────────────────────────────────
        var dpActual = new DynamicParameters(parametrosBase);
        dpActual.Add("Desde", fechaDesde);
        dpActual.Add("Hasta", fechaHasta);
        if (usuarioId.HasValue) dpActual.Add("UsuarioId", usuarioId.Value);

        // ── Parámetros período anterior ───────────────────────────────────────
        var dpAnterior = new DynamicParameters(parametrosBase);
        dpAnterior.Add("Desde", fechaDesdeAnterior);
        dpAnterior.Add("Hasta", fechaHastaAnterior);
        if (usuarioId.HasValue) dpAnterior.Add("UsuarioId", usuarioId.Value);

        // ── Parámetros top clientes ───────────────────────────────────────────
        var dpClientes = new DynamicParameters(parametrosBase);
        dpClientes.Add("Desde", fechaDesde);
        dpClientes.Add("Hasta", fechaHasta);
        dpClientes.Add("Limite", limite);
        if (usuarioId.HasValue) dpClientes.Add("UsuarioId", usuarioId.Value);

        // ═════════════════════════════════════════════════════════════════════
        // 1. KPI — período actual
        // ═════════════════════════════════════════════════════════════════════
        var sqlKpiActual = $@"
            SELECT
                COALESCE(SUM(
                    CASE WHEN c.tipoMoneda = 'USD' THEN c.importeTotal * c.tipoCambio
                         ELSE c.importeTotal END
                ), 0) AS TotalVentas,
                COALESCE(SUM(
                    CASE WHEN c.tipoMoneda = 'USD' THEN c.totalIGV * c.tipoCambio
                         ELSE c.totalIGV END
                ), 0) AS TotalIGV,
                COUNT(*) AS TotalDocumentos
            FROM comprobante c
            WHERE {whereBase}
              AND {whereEstados}
              AND c.tipoComprobante IN ('01','03')
              AND c.fechaEmision >= @Desde
              AND c.fechaEmision <= @Hasta
              {whereUsuario};";

        var kpiActual = await _connection.QueryFirstOrDefaultAsync<KpiRawDto>(
            sqlKpiActual, dpActual, _transaction) ?? new KpiRawDto();

        // ═════════════════════════════════════════════════════════════════════
        // 2. KPI — período anterior (para tendencia)
        // ═════════════════════════════════════════════════════════════════════
        var kpiAnterior = await _connection.QueryFirstOrDefaultAsync<KpiRawDto>(
            sqlKpiActual
                .Replace("@Desde", "@Desde")
                .Replace("@Hasta", "@Hasta"),
            dpAnterior, _transaction) ?? new KpiRawDto();

        // ═════════════════════════════════════════════════════════════════════
        // 3. Distribución de documentos
        // ═════════════════════════════════════════════════════════════════════
        var sqlDistribucion = $@"
            SELECT
                SUM(CASE WHEN c.tipoComprobante = '01' THEN 1 ELSE 0 END) AS Facturas,
                SUM(CASE WHEN c.tipoComprobante = '03' THEN 1 ELSE 0 END) AS Boletas,
                SUM(CASE WHEN c.tipoComprobante = '07' THEN 1 ELSE 0 END) AS NotasCredito,
                SUM(CASE WHEN c.tipoComprobante = '08' THEN 1 ELSE 0 END) AS NotasDebito
            FROM comprobante c
            WHERE {whereBase}
              AND {whereEstados}
              AND c.fechaEmision >= @Desde
              AND c.fechaEmision <= @Hasta
              {whereUsuario};";

        var distribucion = await _connection.QueryFirstOrDefaultAsync<DistribucionDocumentosDto>(
            sqlDistribucion, dpActual, _transaction) ?? new DistribucionDocumentosDto();

        // ═════════════════════════════════════════════════════════════════════
        // 4. Gráfico agrupado según período
        // ═════════════════════════════════════════════════════════════════════
        var grafico = await ConsultarGrafico(
            whereBase, whereEstados, whereUsuario,
            parametrosBase, periodo, fechaDesde, fechaHasta, usuarioId);

        // ═════════════════════════════════════════════════════════════════════
        // 5. Top clientes
        // ═════════════════════════════════════════════════════════════════════
        var sqlClientes = $@"
            SELECT
                c.clienteRznSocial  AS ClienteRznSocial,
                c.clienteNumDoc     AS ClienteNumDoc,
                COUNT(*)            AS NumDocs,
                COALESCE(SUM(
                    CASE WHEN c.tipoMoneda = 'USD' THEN c.valorVenta * c.tipoCambio
                         ELSE c.valorVenta END
                ), 0) AS Subtotal,
                COALESCE(SUM(
                    CASE WHEN c.tipoMoneda = 'USD' THEN c.totalIGV * c.tipoCambio
                         ELSE c.totalIGV END
                ), 0) AS Igv,
                COALESCE(SUM(
                    CASE WHEN c.tipoMoneda = 'USD' THEN c.importeTotal * c.tipoCambio
                         ELSE c.importeTotal END
                ), 0) AS Total
            FROM comprobante c
            WHERE {whereBase}
              AND {whereEstados}
              AND c.tipoComprobante IN ('01','03')
              AND c.fechaEmision >= @Desde
              AND c.fechaEmision <= @Hasta
              {whereUsuario}
            GROUP BY c.clienteNumDoc, c.clienteRznSocial
            ORDER BY Total DESC
            LIMIT @Limite;";

        var topClientes = (await _connection.QueryAsync<ClienteResumenDto>(
            sqlClientes, dpClientes, _transaction)).ToList();

        // ── Totales de clientes ───────────────────────────────────────────────
        var totales = new TotalesClientesDto
        {
            TotalDocs     = topClientes.Sum(c => c.NumDocs),
            TotalSubtotal = topClientes.Sum(c => c.Subtotal),
            TotalIgv      = topClientes.Sum(c => c.Igv),
            TotalGeneral  = topClientes.Sum(c => c.Total),
        };

        return new ReporteResponseDto
        {
            Kpi = new KpiDto
            {
                TotalVentas            = kpiActual.TotalVentas,
                TotalIGV               = kpiActual.TotalIGV,
                TotalDocumentos        = kpiActual.TotalDocumentos,
                TotalVentasAnterior    = kpiAnterior.TotalVentas,
                TotalIGVAnterior       = kpiAnterior.TotalIGV,
                TotalDocumentosAnterior = kpiAnterior.TotalDocumentos,
            },
            Grafico      = grafico,
            Distribucion = distribucion,
            TopClientes  = topClientes,
            TotalesClientes = totales,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GRÁFICO — agrupamiento dinámico
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<List<GraficoBarraDto>> ConsultarGrafico(
        string whereBase,
        string whereEstados,
        string whereUsuario,
        object parametrosBase,
        string periodo,
        DateTime fechaDesde,
        DateTime fechaHasta,
        int? usuarioId)
    {
        var esAnio = periodo.ToLower() == "año";

        var groupExpr = esAnio ? "DATE_FORMAT(c.fechaEmision, '%Y-%m')" : "c.fechaEmision";
        var labelExpr = esAnio ? "DATE_FORMAT(c.fechaEmision, '%Y-%m')" : "DAY(c.fechaEmision)";

        var dp = new DynamicParameters(parametrosBase);
        dp.Add("Desde", fechaDesde);
        dp.Add("Hasta", fechaHasta);
        if (usuarioId.HasValue) dp.Add("UsuarioId", usuarioId.Value);

        var sql = $@"
            SELECT
                {labelExpr} AS Etiqueta,
                COALESCE(SUM(
                    CASE WHEN c.tipoMoneda = 'USD' THEN c.importeTotal * c.tipoCambio
                        ELSE c.importeTotal END
                ), 0) AS Ventas,
                COALESCE(SUM(
                    CASE WHEN c.tipoMoneda = 'USD' THEN c.totalIGV * c.tipoCambio
                        ELSE c.totalIGV END
                ), 0) AS Igv
            FROM comprobante c
            WHERE {whereBase}
            AND {whereEstados}
            AND c.tipoComprobante IN ('01','03')
            AND c.fechaEmision >= @Desde
            AND c.fechaEmision <= @Hasta
            {whereUsuario}
            GROUP BY {groupExpr}
            ORDER BY {groupExpr} ASC;";

        var raw = (await _connection.QueryAsync<GraficoBarraDto>(sql, dp, _transaction)).ToList();

        return periodo.ToLower() switch
        {
            "semana"        => CompletarDiasSemana(raw, fechaDesde, fechaHasta),
            "mes"           => CompletarDiasMes(raw, fechaDesde, fechaHasta),
            "personalizado" => CompletarDiasMes(raw, fechaDesde, fechaHasta),
            "año"           => CompletarMesesAnio(raw, fechaDesde, fechaHasta),
            _               => raw, // hoy — solo 1 punto
        };
    }

    // ── Semana: Lun-Dom con nombres ───────────────────────────────────────────────
    private static List<GraficoBarraDto> CompletarDiasSemana(
        List<GraficoBarraDto> raw, DateTime desde, DateTime hasta)
    {
        var diasSemana = new[] { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
        var resultado  = new List<GraficoBarraDto>();

        for (var d = desde; d <= hasta; d = d.AddDays(1))
        {
            var etiqueta = diasSemana[(int)d.DayOfWeek == 0 ? 6 : (int)d.DayOfWeek - 1];
            var existe   = raw.FirstOrDefault(r => r.Etiqueta == d.Day.ToString());
            resultado.Add(new GraficoBarraDto
            {
                Etiqueta = etiqueta,
                Ventas   = existe?.Ventas ?? 0,
                Igv      = existe?.Igv    ?? 0,
            });
        }
        return resultado;
    }

    // ── Mes / Personalizado: número de día con 0 donde no hay ventas ──────────────
    private static List<GraficoBarraDto> CompletarDiasMes(
        List<GraficoBarraDto> raw, DateTime desde, DateTime hasta)
    {
        var resultado = new List<GraficoBarraDto>();

        for (var d = desde; d <= hasta; d = d.AddDays(1))
        {
            var etiqueta = d.Day.ToString();
            var existe   = raw.FirstOrDefault(r => r.Etiqueta == etiqueta);
            resultado.Add(new GraficoBarraDto
            {
                Etiqueta = etiqueta,
                Ventas   = existe?.Ventas ?? 0,
                Igv      = existe?.Igv    ?? 0,
            });
        }
        return resultado;
    }

    // ── Año: Ene→mes actual con nombres cortos y 0 donde no hay ventas ────────────
    private static List<GraficoBarraDto> CompletarMesesAnio(
        List<GraficoBarraDto> raw, DateTime desde, DateTime hasta)
    {
        var nombresMes = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun",
                                "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
        var resultado  = new List<GraficoBarraDto>();

        for (var m = new DateTime(desde.Year, desde.Month, 1);
            m <= new DateTime(hasta.Year, hasta.Month, 1);
            m = m.AddMonths(1))
        {
            // La clave del raw es 'YYYY-MM' (ej: "2026-04")
            var clave    = m.ToString("yyyy-MM");
            var etiqueta = nombresMes[m.Month - 1];
            var existe   = raw.FirstOrDefault(r => r.Etiqueta == clave);
            resultado.Add(new GraficoBarraDto
            {
                Etiqueta = etiqueta,
                Ventas   = existe?.Ventas ?? 0,
                Igv      = existe?.Igv    ?? 0,
            });
        }
        return resultado;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // EXPORT EXCEL — sin límite
    // ─────────────────────────────────────────────────────────────────────────
    private async Task<List<ClienteExportDto>> ConsultarClientesExport(
        string whereBase,
        object parametrosBase,
        DateTime fechaDesde,
        DateTime fechaHasta,
        int? usuarioId)
    {
        var estadosExcluidos = $"'{string.Join("','", EstadosExcluidos)}'";
        var whereEstados = $"c.estadoSunat NOT IN ({estadosExcluidos})";
        var whereUsuario = usuarioId.HasValue ? "AND c.usuarioCreacion = @UsuarioId" : "";

        var dp = new DynamicParameters(parametrosBase);
        dp.Add("Desde", fechaDesde);
        dp.Add("Hasta", fechaHasta);
        if (usuarioId.HasValue) dp.Add("UsuarioId", usuarioId.Value);

        var sql = $@"
            SELECT
                c.clienteRznSocial  AS ClienteRznSocial,
                c.clienteNumDoc     AS ClienteNumDoc,
                COUNT(*)            AS NumDocs,
                COALESCE(SUM(
                    CASE WHEN c.tipoMoneda = 'USD' THEN c.valorVenta * c.tipoCambio
                         ELSE c.valorVenta END
                ), 0) AS Subtotal,
                COALESCE(SUM(
                    CASE WHEN c.tipoMoneda = 'USD' THEN c.totalIGV * c.tipoCambio
                         ELSE c.totalIGV END
                ), 0) AS Igv,
                COALESCE(SUM(
                    CASE WHEN c.tipoMoneda = 'USD' THEN c.importeTotal * c.tipoCambio
                         ELSE c.importeTotal END
                ), 0) AS Total
            FROM comprobante c
            WHERE {whereBase}
              AND {whereEstados}
              AND c.tipoComprobante IN ('01','03')
              AND c.fechaEmision >= @Desde
              AND c.fechaEmision <= @Hasta
              {whereUsuario}
            GROUP BY c.clienteNumDoc, c.clienteRznSocial
            ORDER BY Total DESC;";

        return (await _connection.QueryAsync<ClienteExportDto>(sql, dp, _transaction)).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<(string Ruc, string Cod)> ObtenerDatosSucursal(int sucursalId)
    {
        var sql = @"
            SELECT empresaRuc, codEstablecimiento
            FROM sucursal
            WHERE sucursalID = @SucursalId AND estado = 1
            LIMIT 1;";

        var result = await _connection.QueryFirstOrDefaultAsync<(string, string)>(
            sql, new { SucursalId = sucursalId }, _transaction);

        if (result == default)
            throw new KeyNotFoundException($"Sucursal {sucursalId} no encontrada o inactiva.");

        return result;
    }

    private static (DateTime Desde, DateTime Hasta) ObtenerRango(
        string periodo, DateTime? desde, DateTime? hasta)
    {
        var hoy = DateTime.Today;
        
        // ── Calcular lunes de la semana actual ──
        int diasDesdeElLunes = ((int)hoy.DayOfWeek + 6) % 7; // Lun=0, Mar=1... Dom=6
        var lunesActual = hoy.AddDays(-diasDesdeElLunes);

        return periodo.ToLower() switch
        {
            "hoy"           => (hoy, hoy),
            "semana"        => (lunesActual, hoy),           // Lunes → hoy
            "mes"           => (new DateTime(hoy.Year, hoy.Month, 1), hoy),
            "año"           => (new DateTime(hoy.Year, 1, 1), hoy),
            "personalizado" => (desde ?? hoy, hasta ?? hoy),
            _               => (hoy, hoy),
        };
    }

    private static (DateTime Desde, DateTime Hasta) ObtenerRangoAnterior(
        string periodo, DateTime desdeActual, DateTime hastaActual)
    {
        // Duración exacta del período actual en días
        var duracion = (hastaActual - desdeActual).Days + 1;

        return periodo.ToLower() switch
        {
            // Todos restan exactamente la misma cantidad de días
            "hoy"           => (desdeActual.AddDays(-1), hastaActual.AddDays(-1)),
            "semana"        => (desdeActual.AddDays(-7), hastaActual.AddDays(-7)),
            "mes"           => (desdeActual.AddDays(-duracion), hastaActual.AddDays(-duracion)),
            "año"           => (desdeActual.AddDays(-duracion), hastaActual.AddDays(-duracion)),
            _               => (desdeActual.AddDays(-duracion), hastaActual.AddDays(-duracion)),
        };
    }
}

// ── DTO interno para mapeo raw ────────────────────────────────────────────────
internal class KpiRawDto
{
    public decimal TotalVentas { get; set; }
    public decimal TotalIGV { get; set; }
    public int TotalDocumentos { get; set; }
}