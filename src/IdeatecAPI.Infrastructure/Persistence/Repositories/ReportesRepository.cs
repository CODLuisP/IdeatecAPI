// IdeatecAPI.Infrastructure.Persistence.Repositories/ReportesRepository.cs

using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Reportes.DTOs;
using IdeatecAPI.Domain.Entities;

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
                ), 0) AS TotalIGV
            FROM comprobante c
            WHERE {whereBase}
            AND c.estadoSunat NOT IN ('RECHAZADO')
            AND c.tipoComprobante IN ('01','03')
            AND c.fechaEmision >= @Desde
            AND c.fechaEmision <= @Hasta
            {whereUsuario};";
        
        var sqlTotalDocs = $@"
            SELECT COUNT(*) 
            FROM comprobante c
            WHERE {whereBase}
            AND c.estadoSunat NOT IN ('RECHAZADO')
            AND c.fechaEmision >= @Desde
            AND c.fechaEmision <= @Hasta
            {whereUsuario};";

        var totalDocs = await _connection.QueryFirstOrDefaultAsync<int>(
            sqlTotalDocs, dpActual, _transaction);
        
        var totalDocsAnterior = await _connection.QueryFirstOrDefaultAsync<int>(
            sqlTotalDocs, dpAnterior, _transaction);

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
              AND c.estadoSunat NOT IN ('RECHAZADO')
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
              AND c.estadoSunat NOT IN ('RECHAZADO')
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
                TotalDocumentos        = totalDocs,
                TotalVentasAnterior    = kpiAnterior.TotalVentas,
                TotalIGVAnterior       = kpiAnterior.TotalIGV,
                TotalDocumentosAnterior = totalDocsAnterior,
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
        var hoy = desde?.Date ?? DateTime.Today;
        
        int diasDesdeElLunes = ((int)hoy.DayOfWeek + 6) % 7;
        var lunesActual = hoy.AddDays(-diasDesdeElLunes);

        return periodo.ToLower() switch
        {
            "hoy"           => (hoy, hoy),
            "semana"        => (lunesActual, hasta?.Date ?? hoy),
            "mes"           => (new DateTime(hoy.Year, hoy.Month, 1), hasta?.Date ?? hoy),
            "año"           => (new DateTime(hoy.Year, 1, 1), hasta?.Date ?? hoy),
            "personalizado" => (desde?.Date ?? hoy, hasta?.Date ?? hoy),
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
        public async Task<IEnumerable<Comprobante>> GetListadoParaReportesAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null)
        {
        var sql = BaseSelectReportes + @"
            WHERE c.empresaRuc = @Ruc
            AND c.estadoSunat NOT IN ('PENDIENTE')
            AND (@CodEstablecimiento IS NULL OR c.establecimientoAnexo = @CodEstablecimiento)
            AND (@UsuarioCreacion IS NULL OR c.usuarioCreacion = @UsuarioCreacion)
            AND (@ClienteNumDoc IS NULL OR c.clienteNumDoc = @ClienteNumDoc)
            AND (
                -- Facturas y boletas: filtrar por su propia fecha
                (c.tipoComprobante NOT IN ('07','08')
                    AND (@FechaDesde IS NULL OR c.fechaEmision >= @FechaDesde)
                    AND (@FechaHasta IS NULL OR c.fechaEmision <= @FechaHasta))
                OR
                -- Notas: emitidas en el rango
                (c.tipoComprobante IN ('07','08')
                    AND (@FechaDesde IS NULL OR c.fechaEmision >= @FechaDesde)
                    AND (@FechaHasta IS NULL OR c.fechaEmision <= @FechaHasta))
                OR
                -- Notas: cuyo doc afectado está en el rango (aunque la nota sea de otro día)
                (c.tipoComprobante IN ('07','08')
                    AND c.comprobanteAfectadoID IS NOT NULL
                    AND EXISTS (
                        SELECT 1 FROM comprobante ca
                        WHERE ca.comprobanteID = c.comprobanteAfectadoID
                        AND (@FechaDesde IS NULL OR ca.fechaEmision >= @FechaDesde)
                        AND (@FechaHasta IS NULL OR ca.fechaEmision <= @FechaHasta)
                    ))
            )
            ORDER BY c.fechaEmision DESC"
            + (limit.HasValue ? " LIMIT @Limit" : "");

        return await _connection.QueryAsync<Comprobante>(sql, new
        {
            Ruc = ruc,
            CodEstablecimiento = codEstablecimiento,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            UsuarioCreacion = usuarioCreacion,
            ClienteNumDoc = clienteNumDoc,
            Limit = limit
        }, _transaction);
    }

    public async Task<IEnumerable<ProductoTopDTO>> GetProductosTopAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null,
        string orderBy = "monto")
    {
        var orden = orderBy.ToLower() switch
        {
            "cantidad" => "TotalCantidad DESC",
            "veces"    => "VecesVendido DESC",
            _          => "TotalMonto DESC"
        };

        var sql = @"
            SELECT 
                cd.codigo                           AS Codigo,
                cd.descripcion                      AS Descripcion,
                SUM(cd.cantidad)                    AS TotalCantidad,
                SUM(cd.totalVentaItem)              AS TotalMonto,
                SUM(cd.montoIGV)                    AS TotalIGV,
                COUNT(DISTINCT cd.comprobanteId)    AS VecesVendido,
                AVG(cd.precioUnitario)              AS PrecioPromedio
            FROM comprobantedetalle cd
            INNER JOIN comprobante c ON c.comprobanteID = cd.comprobanteId
            WHERE c.empresaRuc = @Ruc
            AND c.estadoSunat IN ('ACEPTADO', 'ACEPTADO_CON_OBSERVACIONES', 'PENDIENTE')
            AND (@CodEstablecimiento IS NULL OR c.establecimientoAnexo = @CodEstablecimiento)
            AND (@FechaDesde IS NULL OR c.fechaEmision >= @FechaDesde)
            AND (@FechaHasta IS NULL OR c.fechaEmision <= @FechaHasta)
            AND (@UsuarioCreacion IS NULL OR c.usuarioCreacion = @UsuarioCreacion)
            AND (@ClienteNumDoc IS NULL OR c.clienteNumDoc = @ClienteNumDoc)
            GROUP BY cd.codigo, cd.descripcion
            ORDER BY " + orden
            + (limit.HasValue ? " LIMIT @Limit" : "");

        return await _connection.QueryAsync<ProductoTopDTO>(sql, new
        {
            Ruc = ruc,
            CodEstablecimiento = codEstablecimiento,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            UsuarioCreacion = usuarioCreacion,
            ClienteNumDoc = clienteNumDoc,
            Limit = limit
        }, _transaction);
    }

    public async Task<IEnumerable<MedioPagoTopDTO>> GetMediosPagoTopAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null)
    {
        var sql = @"
            SELECT MedioPago, SUM(VecesUsado) AS VecesUsado, SUM(MontoTotal) AS MontoTotal, AVG(PromedioMonto) AS PromedioMonto
            FROM (
                SELECT 
                    medioPago               AS MedioPago,
                    COUNT(*)                AS VecesUsado,
                    SUM(p.monto)            AS MontoTotal,
                    AVG(p.monto)            AS PromedioMonto
                FROM pago p
                INNER JOIN comprobante c ON c.comprobanteID = p.comprobanteID
                WHERE c.empresaRuc = @Ruc
                AND c.estadoSunat IN ('ACEPTADO', 'ACEPTADO_CON_OBSERVACIONES')
                AND p.medioPago IS NOT NULL AND p.medioPago != ''
                AND (@CodEstablecimiento IS NULL OR c.establecimientoAnexo = @CodEstablecimiento)
                AND (@FechaDesde IS NULL OR c.fechaEmision >= @FechaDesde)
                AND (@FechaHasta IS NULL OR c.fechaEmision <= @FechaHasta)
                AND (@UsuarioCreacion IS NULL OR c.usuarioCreacion = @UsuarioCreacion)
                AND (@ClienteNumDoc IS NULL OR c.clienteNumDoc = @ClienteNumDoc)

                UNION ALL

                SELECT
                    c2.tipoPago             AS MedioPago,
                    COUNT(*)                AS VecesUsado,
                    SUM(cu.monto)           AS MontoTotal,
                    AVG(cu.monto)           AS PromedioMonto
                FROM cuota cu
                INNER JOIN comprobante c2 ON c2.comprobanteID = cu.comprobanteID
                WHERE cu.estado = 'PAGADO'
                AND c2.empresaRuc = @Ruc
                AND c2.estadoSunat IN ('ACEPTADO', 'ACEPTADO_CON_OBSERVACIONES')
                AND c2.tipoPago IS NOT NULL AND c2.tipoPago != ''
                AND (@CodEstablecimiento IS NULL OR c2.establecimientoAnexo = @CodEstablecimiento)
                AND (@FechaDesde IS NULL OR c2.fechaEmision >= @FechaDesde)
                AND (@FechaHasta IS NULL OR c2.fechaEmision <= @FechaHasta)
                AND (@UsuarioCreacion IS NULL OR c2.usuarioCreacion = @UsuarioCreacion)
                AND (@ClienteNumDoc IS NULL OR c2.clienteNumDoc = @ClienteNumDoc)
            ) AS union_medios
            GROUP BY MedioPago
            ORDER BY VecesUsado DESC"
            + (limit.HasValue ? " LIMIT @Limit" : "");

        // El SQL ya agrupa y ordena, retornamos directo
        return await _connection.QueryAsync<MedioPagoTopDTO>(sql, new
        {
            Ruc = ruc,
            CodEstablecimiento = codEstablecimiento,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            UsuarioCreacion = usuarioCreacion,
            ClienteNumDoc = clienteNumDoc,
            Limit = limit
        }, _transaction);
    }

    public async Task<IEnumerable<Comprobante>> GetListadoControlCajaAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null)
    {
        // Nota: se incluyen los RECHAZADOS a propósito (visibilidad de correlativo).
        // Sus montos se ponen en cero en la capa de servicio/presentación, no aquí.
        var sql = BaseSelectReportes + @"
            WHERE c.empresaRuc = @Ruc
            AND (@CodEstablecimiento IS NULL OR c.establecimientoAnexo = @CodEstablecimiento)
            AND (@FechaDesde IS NULL OR c.fechaEmision >= @FechaDesde)
            AND (@FechaHasta IS NULL OR c.fechaEmision <= @FechaHasta)
            AND (@UsuarioCreacion IS NULL OR c.usuarioCreacion = @UsuarioCreacion)
            AND (@ClienteNumDoc IS NULL OR c.clienteNumDoc = @ClienteNumDoc)
            ORDER BY c.fechaEmision DESC"
            + (limit.HasValue ? " LIMIT @Limit" : "");

        return await _connection.QueryAsync<Comprobante>(sql, new
        {
            Ruc = ruc,
            CodEstablecimiento = codEstablecimiento,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            UsuarioCreacion = usuarioCreacion,
            ClienteNumDoc = clienteNumDoc,
            Limit = limit
        }, _transaction);
    }

    public async Task<IEnumerable<(int ComprobanteId, string? MedioPago, decimal Monto)>>
        GetPagosByComprobanteIdsAsync(IEnumerable<int> comprobanteIds)
    {
        var ids = comprobanteIds.ToList();
        if (!ids.Any())
            return Enumerable.Empty<(int, string?, decimal)>();

        var sql = @"
            SELECT comprobanteID AS ComprobanteId,
                   medioPago     AS MedioPago,
                   monto         AS Monto
            FROM pago
            WHERE comprobanteID IN @Ids";

        var rows = await _connection.QueryAsync<(int ComprobanteId, string? MedioPago, decimal Monto)>(
            sql, new { Ids = ids }, _transaction);

        return rows;
    }

    private const string BaseSelectReportes = @"
        SELECT 
            c.comprobanteID           AS ComprobanteId,
            c.tipoOperacion           AS TipoOperacion,
            c.tipoComprobante         AS TipoComprobante,
            c.serie                   AS Serie,
            c.correlativo             AS Correlativo,
            c.numeroCompleto          AS NumeroCompleto,
            c.tipoCambio              AS TipoCambio,
            c.fechaEmision            AS FechaEmision,
            TIMESTAMP(c.fechaEmision, c.horaEmision) AS HoraEmision,
            c.fechaVencimiento        AS FechaVencimiento,
            c.tipoMoneda              AS TipoMoneda,
            c.tipoPago                AS TipoPago,
            c.empresaRuc              AS EmpresaRuc,
            c.empresaRazonSocial      AS EmpresaRazonSocial,
            c.establecimientoAnexo    AS EmpresaEstablecimientoAnexo,
            c.empresaDireccion        AS EmpresaDireccion,
            c.clienteTipoDoc          AS ClienteTipoDoc,
            c.clienteNumDoc           AS ClienteNumDoc,
            c.clienteRznSocial        AS ClienteRazonSocial,
            c.clienteDireccion        AS ClienteDireccion,
            c.clienteProvincia        AS ClienteProvincia,
            c.clienteDepartamento     AS ClienteDepartamento,
            c.clienteDistrito         AS ClienteDistrito,
            c.codigoTipoDescGlobal    AS CodigoTipoDescGlobal,
            c.descuentoGlobal         AS DescuentoGlobal,
            c.totalOperacionesGravadas   AS TotalOperacionesGravadas,
            c.totalOperacionesExoneradas AS TotalOperacionesExoneradas,
            c.totalOperacionesInafectas  AS TotalOperacionesInafectas,
            c.totalOperacionesGratuitas  AS TotalOperacionesGratuitas,
            c.totalIgvGratuitas       AS TotalIgvGratuitas,
            c.totalIGV                AS TotalIGV,
            c.totalImpuestos          AS TotalImpuestos,
            c.totalDescuentos         AS TotalDescuentos,
            c.totalOtrosCargos        AS TotalOtrosCargos,
            c.totalIcbper             AS TotalIcbper,
            c.valorVenta              AS ValorVenta,
            c.subTotal                AS SubTotal,
            c.importeTotal            AS ImporteTotal,
            c.montoCredito            AS MontoCredito,
            c.tipDocAfectado          AS TipDocAfectado,
            c.numDocAfectado          AS NumDocAfectado,
            c.tipoNotaCreditoDebito   AS TipoNotaCreditoDebito,
            c.motivoNota              AS MotivoNota,
            c.comprobanteAfectadoID   AS ComprobanteAfectadoId,
            c.observaciones           AS Observaciones,
            c.estadoSunat             AS EstadoSunat,
            c.pdfGenerado             AS PdfGenerado,
            c.codigoRespuestaSunat    AS CodigoRespuestaSunat,
            c.mensajeRespuestaSunat   AS MensajeRespuestaSunat,
            c.fechaEnvioSunat         AS FechaEnvioSunat,
            c.xmlGenerado             AS XmlGenerado,
            c.usuarioCreacion         AS UsuarioCreacion,
            c.fechaCreacion           AS FechaCreacion
        FROM comprobante c
        ";
    }
// ── DTO interno para mapeo raw ────────────────────────────────────────────────
internal class KpiRawDto
{
    public decimal TotalVentas { get; set; }
    public decimal TotalIGV { get; set; }
}