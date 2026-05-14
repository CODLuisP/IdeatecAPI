using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.CuentasPorCobrar.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories.CuentasPorCobrar;

public class CuentasPorCobrarRepository : ICuentasPorCobrarRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public CuentasPorCobrarRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<IEnumerable<ListaCuentasPorCobrarDto>> GetCuentasPorCobrarAsync(
        string empresaRuc,
        string? establecimientoAnexo,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? clienteNumDoc)
    {
        var tieneFechas = fechaInicio.HasValue || fechaFin.HasValue;

        var sql = @"
            SELECT 
                c.comprobanteID         AS ComprobanteId,
                c.tipoComprobante       AS TipoComprobante,
                c.serie                 AS Serie,
                c.correlativo           AS Correlativo,
                c.numeroCompleto        AS NumeroCompleto,
                c.fechaEmision          AS FechaEmision,
                c.fechaVencimiento      AS FechaVencimiento,
                c.tipoMoneda            AS TipoMoneda,
                c.estadoSunat           AS EstadoSunat,
                c.establecimientoAnexo  AS EstablecimientoAnexo,
                c.usuarioCreacion       AS UsuarioCreacion,
                c.clienteNumDoc         AS ClienteNumDoc,
                c.clienteRznSocial      AS ClienteRznSocial,
                c.clienteCorreo         AS ClienteCorreo,
                c.clienteWhatsApp       AS ClienteWhatsApp,
                c.valorVenta            AS ValorVenta,
                c.totalIGV              AS TotalIGV,
                c.importeTotal          AS ImporteTotal,
                c.montoCredito          AS MontoCredito,
                c.tipoPago              AS TipoPago
            FROM comprobante c
            WHERE c.empresaRuc = @EmpresaRuc
            AND c.tipoPago = 'CREDITO'
            AND c.estadoSunat = 'ACEPTADO'
            AND c.tipoComprobante IN ('01', '03')
            AND (@EstablecimientoAnexo IS NULL OR c.establecimientoAnexo = @EstablecimientoAnexo)
            AND (@FechaInicio IS NULL OR c.fechaEmision >= @FechaInicio)
            AND (@FechaFin    IS NULL OR c.fechaEmision <= @FechaFin)
            AND (@ClienteNumDoc IS NULL OR c.clienteNumDoc = @ClienteNumDoc)
            AND EXISTS (
                SELECT 1 FROM cuota cu 
                WHERE cu.comprobanteID = c.comprobanteID 
                AND (cu.estado != 'PAGADO' OR cu.estado IS NULL)
            )
            ORDER BY c.fechaEmision DESC"
            + (tieneFechas ? "" : " LIMIT 50");

        return await _connection.QueryAsync<ListaCuentasPorCobrarDto>(sql, new
        {
            EmpresaRuc = empresaRuc,
            EstablecimientoAnexo = establecimientoAnexo,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            ClienteNumDoc = clienteNumDoc
        }, _transaction);
    }

    public async Task<IEnumerable<CuotaDto>> GetCuotasByComprobanteIdAsync(int comprobanteId)
    {
        var sql = @"
            SELECT 
                cuotaID             AS CuotaId,
                comprobanteID       AS ComprobanteId,
                numeroCuota         AS NumeroCuota,
                monto               AS Monto,
                fechaVencimiento    AS FechaVencimiento,
                montoPagado         AS MontoPagado,
                fechaPago           AS FechaPago,
                estado              AS Estado,
                usuarioRegistroPago AS UsuarioRegistroPago
            FROM cuota
            WHERE comprobanteID = @ComprobanteId
            ORDER BY numeroCuota ASC";

        return await _connection.QueryAsync<CuotaDto>(
            sql, new { ComprobanteId = comprobanteId }, _transaction);
    }

    public async Task<Cuota?> GetCuotaByIdAsync(int cuotaId)
    {
        var sql = @"
            SELECT 
                cuotaID             AS CuotaId,
                comprobanteID       AS ComprobanteId,
                numeroCuota         AS NumeroCuota,
                monto               AS Monto,
                fechaVencimiento    AS FechaVencimiento,
                montoPagado         AS MontoPagado,
                fechaPago           AS FechaPago,
                estado              AS Estado,
                usuarioRegistroPago AS UsuarioRegistroPago
            FROM cuota
            WHERE cuotaID = @CuotaId";

        return await _connection.QueryFirstOrDefaultAsync<Cuota>(
            sql, new { CuotaId = cuotaId }, _transaction);
    }

    public async Task<bool> PagarCuotaAsync(PagarCuotaDto dto, string nuevoEstado, decimal nuevoMontoPagado)
    {
        // 1. Actualizar cuota
        var sqlUpdateCuota = @"
            UPDATE cuota SET
                montoPagado         = @MontoPagado,
                fechaPago           = @FechaPago,
                estado              = @Estado,
                usuarioRegistroPago = @UsuarioRegistroPago
            WHERE cuotaID = @CuotaId";

        await _connection.ExecuteAsync(sqlUpdateCuota, new
        {
            MontoPagado = nuevoMontoPagado,
            dto.FechaPago,
            Estado = nuevoEstado,
            dto.UsuarioRegistroPago,
            dto.CuotaId
        }, _transaction);

        // 2. Insertar en cuotapago
        var sqlInsertCuotaPago = @"
            INSERT INTO cuotapago (
                cuotaID,
                montoPagado,
                fechaPago,
                medioPago,
                entidadFinanciera,
                numeroOperacion,
                observaciones,
                usuarioRegistroPago,
                fechaRegistro
            ) VALUES (
                @CuotaId,
                @MontoPagado,
                @FechaPago,
                @MedioPago,
                @EntidadFinanciera,
                @NumeroOperacion,
                @Observaciones,
                @UsuarioRegistroPago,
                NOW()
            )";

        var result = await _connection.ExecuteAsync(sqlInsertCuotaPago, new
        {
            dto.CuotaId,
            dto.MontoPagado,
            dto.FechaPago,
            dto.MedioPago,
            dto.EntidadFinanciera,
            dto.NumeroOperacion,
            dto.Observaciones,
            dto.UsuarioRegistroPago
        }, _transaction);

        return result > 0;
    }

    public async Task<IEnumerable<CuotaPagoDto>> GetHistorialPagosByCuotaIdAsync(int cuotaId)
    {
        var sql = @"
            SELECT
                cuotaPagoID         AS CuotaPagoId,
                cuotaID             AS CuotaId,
                montoPagado         AS MontoPagado,
                fechaPago           AS FechaPago,
                medioPago           AS MedioPago,
                entidadFinanciera   AS EntidadFinanciera,
                numeroOperacion     AS NumeroOperacion,
                observaciones       AS Observaciones,
                usuarioRegistroPago AS UsuarioRegistroPago,
                fechaRegistro       AS FechaRegistro
            FROM cuotapago
            WHERE cuotaID = @CuotaId
            ORDER BY fechaRegistro ASC";

        return await _connection.QueryAsync<CuotaPagoDto>(
            sql, new { CuotaId = cuotaId }, _transaction);
    }

    public async Task<IEnumerable<ReporteCuentasPorCobrarItemDto>> GetReporteCuentasPorCobrarAsync(ReporteCuentasPorCobrarFiltroDto filtro)
    {
        var fechaFin = filtro.FechaFin ?? filtro.FechaInicio;

        var sql = @"
            SELECT
                c.comprobanteID     AS ComprobanteId,
                c.numeroCompleto    AS NumeroCompleto,
                c.tipoComprobante   AS TipoComprobante,
                c.fechaEmision      AS FechaEmision,
                c.tipoMoneda        AS TipoMoneda,
                c.clienteNumDoc     AS ClienteNumDoc,
                c.clienteRznSocial  AS ClienteRznSocial,
                c.importeTotal      AS ImporteTotal,
                c.montoCredito      AS MontoCredito,
                CASE
                    WHEN COUNT(cu.cuotaID) = SUM(CASE WHEN cu.estado = 'PAGADO' THEN 1 ELSE 0 END)
                        THEN 'PAGADO'
                    ELSE 'PENDIENTE'
                END AS Estado
            FROM comprobante c
            INNER JOIN cuota cu ON cu.comprobanteID = c.comprobanteID
            WHERE c.empresaRuc = @EmpresaRuc
            AND c.tipoPago = 'CREDITO'
            AND c.estadoSunat = 'ACEPTADO'
            AND c.tipoComprobante IN ('01', '03')
            AND (@EstablecimientoAnexo IS NULL OR c.establecimientoAnexo = @EstablecimientoAnexo)
            AND (@FechaInicio IS NULL OR c.fechaEmision >= @FechaInicio)
            AND (@FechaFin    IS NULL OR c.fechaEmision <= @FechaFin)
            AND (@ClienteNumDoc IS NULL OR c.clienteNumDoc = @ClienteNumDoc)
            GROUP BY
                c.comprobanteID, c.numeroCompleto, c.tipoComprobante,
                c.fechaEmision, c.tipoMoneda, c.clienteNumDoc,
                c.clienteRznSocial, c.importeTotal, c.montoCredito
            HAVING
                (@Estado IS NULL) OR
                (@Estado = 'PAGADO' AND COUNT(cu.cuotaID) = SUM(CASE WHEN cu.estado = 'PAGADO' THEN 1 ELSE 0 END)) OR
                (@Estado = 'PENDIENTE' AND COUNT(cu.cuotaID) != SUM(CASE WHEN cu.estado = 'PAGADO' THEN 1 ELSE 0 END))
            ORDER BY c.clienteRznSocial ASC, c.fechaEmision DESC";

        var comprobantes = await _connection.QueryAsync<ReporteCuentasPorCobrarItemDto>(sql, new
        {
            EmpresaRuc           = filtro.EmpresaRuc,
            EstablecimientoAnexo = filtro.EstablecimientoAnexo,
            FechaInicio          = filtro.FechaInicio,
            FechaFin             = fechaFin,
            ClienteNumDoc        = filtro.ClienteNumDoc,
            Estado               = filtro.Estado
        }, _transaction);

        var listaComprobantes = comprobantes.ToList();

        if (!listaComprobantes.Any())
            return listaComprobantes;

        // Traer cuotas de todos los comprobantes en una sola query
        var comprobanteIds = listaComprobantes.Select(x => x.ComprobanteId).ToList();

        var sqlCuotas = @"
            SELECT
                cu.comprobanteID    AS ComprobanteId,
                cu.numeroCuota      AS NumeroCuota,
                cu.monto            AS Monto,
                cu.fechaVencimiento AS FechaVencimiento,
                cu.montoPagado      AS MontoPagado,
                cu.monto - COALESCE(cu.montoPagado, 0) AS Saldo,
                cu.fechaPago        AS FechaPago,
                cu.estado           AS Estado
            FROM cuota cu
            WHERE cu.comprobanteID IN @ComprobanteIds
            ORDER BY cu.numeroCuota ASC";

        var cuotas = await _connection.QueryAsync<(
            int ComprobanteId,
            string? NumeroCuota,
            decimal? Monto,
            DateTime FechaVencimiento,
            decimal? MontoPagado,
            decimal? Saldo,
            DateTime? FechaPago,
            string? Estado)>(
            sqlCuotas, new { ComprobanteIds = comprobanteIds }, _transaction);

        var cuotasPorComprobante = cuotas
            .GroupBy(c => c.ComprobanteId)
            .ToDictionary(g => g.Key, g => g.Select(c => new ReporteCuotaItemDto
            {
                NumeroCuota     = c.NumeroCuota,
                Monto           = c.Monto,
                FechaVencimiento = c.FechaVencimiento,
                MontoPagado     = c.MontoPagado,
                Saldo           = c.Saldo,
                FechaPago       = c.FechaPago,
                Estado          = c.Estado
            }).ToList());

        foreach (var comp in listaComprobantes)
            comp.Cuotas = cuotasPorComprobante.TryGetValue(comp.ComprobanteId, out var c) ? c : new();

        return listaComprobantes;
    }
}