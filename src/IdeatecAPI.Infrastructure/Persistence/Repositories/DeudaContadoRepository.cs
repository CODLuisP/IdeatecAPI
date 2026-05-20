using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.DeudaContado.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories.DeudaContado;

public class DeudaContadoRepository : IDeudaContadoRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public DeudaContadoRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<IEnumerable<ListaDeudaContadoDto>> GetDeudaContadoAsync(
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
                c.tipoPago              AS TipoPago,
                p.pagoID                AS PagoId,
                p.monto                 AS MontoTotal,
                COALESCE(SUM(pd.montoPagado), 0) AS MontoPagado,
                CASE
                    WHEN COALESCE(SUM(pd.montoPagado), 0) = 0 THEN 'PENDIENTE'
                    WHEN COALESCE(SUM(pd.montoPagado), 0) >= p.monto THEN 'PAGADO'
                    ELSE 'PARCIAL'
                END AS Estado
            FROM comprobante c
            INNER JOIN pago p ON p.comprobanteID = c.comprobanteID
            LEFT JOIN pagodeudacontado pd ON pd.pagoID = p.pagoID
            WHERE c.empresaRuc = @EmpresaRuc
            AND c.tipoPago = 'CONTADO'
            AND c.estadoSunat = 'ACEPTADO'
            AND c.tipoComprobante IN ('01', '03')
            AND (@EstablecimientoAnexo IS NULL OR c.establecimientoAnexo = @EstablecimientoAnexo)
            AND (@FechaInicio IS NULL OR c.fechaEmision >= @FechaInicio)
            AND (@FechaFin    IS NULL OR c.fechaEmision <= @FechaFin)
            AND (@ClienteNumDoc IS NULL OR c.clienteNumDoc = @ClienteNumDoc)
            GROUP BY
                c.comprobanteID, c.tipoComprobante, c.serie, c.correlativo,
                c.numeroCompleto, c.fechaEmision, c.fechaVencimiento, c.tipoMoneda,
                c.estadoSunat, c.establecimientoAnexo, c.usuarioCreacion,
                c.clienteNumDoc, c.clienteRznSocial, c.clienteCorreo, c.clienteWhatsApp,
                c.valorVenta, c.totalIGV, c.importeTotal, c.tipoPago,
                p.pagoID, p.monto
            HAVING CASE
                WHEN COALESCE(SUM(pd.montoPagado), 0) >= p.monto THEN 'PAGADO'
                ELSE 'NO_PAGADO'
            END != 'PAGADO'
            ORDER BY c.fechaEmision DESC"
            + (tieneFechas ? "" : " LIMIT 50");

        return await _connection.QueryAsync<ListaDeudaContadoDto>(sql, new
        {
            EmpresaRuc = empresaRuc,
            EstablecimientoAnexo = establecimientoAnexo,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            ClienteNumDoc = clienteNumDoc
        }, _transaction);
    }

    public async Task<Pago?> GetPagoByIdAsync(int pagoId)
    {
        var sql = @"
            SELECT
                pagoID            AS PagoId,
                comprobanteID     AS ComprobanteId,
                medioPago         AS MedioPago,
                monto             AS Monto,
                fechaPago         AS FechaPago,
                numeroOperacion   AS NumeroOperacion,
                entidadFinanciera AS EntidadFinanciera,
                observaciones     AS Observaciones
            FROM pago
            WHERE pagoID = @PagoId";

        return await _connection.QueryFirstOrDefaultAsync<Pago>(
            sql, new { PagoId = pagoId }, _transaction);
    }

    public async Task<IEnumerable<PagoDeudaContadoDto>> GetHistorialPagosByPagoIdAsync(int pagoId)
    {
        var sql = @"
            SELECT
                deudaPagoID         AS DeudaPagoID,
                pagoID              AS PagoID,
                montoPagado         AS MontoPagado,
                fechaPago           AS FechaPago,
                medioPago           AS MedioPago,
                entidadFinanciera   AS EntidadFinanciera,
                numeroOperacion     AS NumeroOperacion,
                observaciones       AS Observaciones,
                usuarioRegistroPago AS UsuarioRegistroPago,
                fechaRegistro       AS FechaRegistro
            FROM pagodeudacontado
            WHERE pagoID = @PagoId
            ORDER BY fechaRegistro ASC";

        return await _connection.QueryAsync<PagoDeudaContadoDto>(
            sql, new { PagoId = pagoId }, _transaction);
    }

    public async Task<bool> RegistrarPagoAsync(RegistrarPagoDeudaContadoDto dto)
    {
        var sql = @"
            INSERT INTO pagodeudacontado (
                pagoID,
                montoPagado,
                fechaPago,
                medioPago,
                entidadFinanciera,
                numeroOperacion,
                observaciones,
                usuarioRegistroPago,
                fechaRegistro
            ) VALUES (
                @PagoId,
                @MontoPagado,
                @FechaPago,
                @MedioPago,
                @EntidadFinanciera,
                @NumeroOperacion,
                @Observaciones,
                @UsuarioRegistroPago,
                NOW()
            )";

        var result = await _connection.ExecuteAsync(sql, new
        {
            dto.PagoId,
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

    public async Task<IEnumerable<ReporteDeudaContadoItemDto>> GetReporteDeudaContadoAsync(ReporteDeudaContadoFiltroDto filtro)
    {
        // Si solo viene fechaInicio, fechaFin = fechaInicio
        var fechaFin = filtro.FechaFin ?? filtro.FechaInicio;

        var sql = @"
            SELECT
                c.comprobanteID         AS ComprobanteId,
                c.numeroCompleto        AS NumeroCompleto,
                c.tipoComprobante       AS TipoComprobante,
                c.fechaEmision          AS FechaEmision,
                c.tipoMoneda            AS TipoMoneda,
                c.clienteNumDoc         AS ClienteNumDoc,
                c.clienteRznSocial      AS ClienteRznSocial,
                p.monto                 AS MontoTotal,
                COALESCE(SUM(pd.montoPagado), 0)                    AS MontoPagado,
                p.monto - COALESCE(SUM(pd.montoPagado), 0)          AS Saldo,
                CASE
                    WHEN COALESCE(SUM(pd.montoPagado), 0) = 0          THEN 'PENDIENTE'
                    WHEN COALESCE(SUM(pd.montoPagado), 0) >= p.monto   THEN 'PAGADO'
                    ELSE 'PARCIAL'
                END AS Estado
            FROM comprobante c
            INNER JOIN pago p ON p.comprobanteID = c.comprobanteID
            LEFT JOIN pagodeudacontado pd ON pd.pagoID = p.pagoID
            WHERE c.empresaRuc = @EmpresaRuc
            AND c.tipoPago = 'CONTADO'
            AND c.estadoSunat = 'ACEPTADO'
            AND c.tipoComprobante IN ('01', '03')
            AND (@EstablecimientoAnexo IS NULL OR c.establecimientoAnexo = @EstablecimientoAnexo)
            AND (@FechaInicio IS NULL OR c.fechaEmision >= @FechaInicio)
            AND (@FechaFin    IS NULL OR c.fechaEmision <= @FechaFin)
            AND (@ClienteNumDoc IS NULL OR c.clienteNumDoc = @ClienteNumDoc)
            GROUP BY
                c.comprobanteID, c.numeroCompleto, c.tipoComprobante,
                c.fechaEmision, c.tipoMoneda, c.clienteNumDoc,
                c.clienteRznSocial, p.pagoID, p.monto
            ORDER BY c.clienteRznSocial ASC, c.fechaEmision DESC";

        var comprobantes = await _connection.QueryAsync<ReporteDeudaContadoItemDto>(sql, new
        {
            EmpresaRuc          = filtro.EmpresaRuc,
            EstablecimientoAnexo = filtro.EstablecimientoAnexo,
            FechaInicio         = filtro.FechaInicio,
            FechaFin            = fechaFin,
            ClienteNumDoc       = filtro.ClienteNumDoc
        }, _transaction);

        var listaComprobantes = comprobantes.ToList();

        if (!listaComprobantes.Any())
            return listaComprobantes;

        // Traer pagos de todos los comprobantes en una sola query
        var pagoIds = listaComprobantes.Select(x => x.ComprobanteId).ToList();

        var sqlPagos = @"
            SELECT
                p.comprobanteID     AS ComprobanteId,
                pd.montoPagado      AS MontoPagado,
                pd.fechaPago        AS FechaPago,
                pd.medioPago        AS MedioPago,
                pd.entidadFinanciera AS EntidadFinanciera,
                pd.numeroOperacion  AS NumeroOperacion,
                pd.observaciones    AS Observaciones
            FROM pagodeudacontado pd
            INNER JOIN pago p ON p.pagoID = pd.pagoID
            WHERE p.comprobanteID IN @ComprobanteIds
            ORDER BY pd.fechaRegistro ASC";

        var pagos = await _connection.QueryAsync<(int ComprobanteId, decimal MontoPagado, DateTime FechaPago, string? MedioPago, string? EntidadFinanciera, string? NumeroOperacion, string? Observaciones)>(
            sqlPagos, new { ComprobanteIds = pagoIds }, _transaction);

        // Agrupar pagos por comprobanteId
        var pagosPorComprobante = pagos
            .GroupBy(p => p.ComprobanteId)
            .ToDictionary(g => g.Key, g => g.Select(p => new ReporteDeudaPagoItemDto
            {
                FechaPago         = p.FechaPago,
                MontoPagado       = p.MontoPagado,
                MedioPago         = p.MedioPago,
                EntidadFinanciera = p.EntidadFinanciera,
                NumeroOperacion   = p.NumeroOperacion,
                Observaciones     = p.Observaciones
            }).ToList());

        // Asignar pagos a cada comprobante
        foreach (var comp in listaComprobantes)
        {
            comp.Pagos = pagosPorComprobante.TryGetValue(comp.ComprobanteId, out var p) ? p : new();
        }

        return listaComprobantes;
    }

    public async Task<bool> EditarPagoAsync(EditarPagoDeudaContadoDto dto)
    {
        var sql = @"
            UPDATE pagodeudacontado SET
                montoPagado       = @MontoPagado,
                fechaPago         = @FechaPago,
                medioPago         = @MedioPago,
                entidadFinanciera = @EntidadFinanciera,
                numeroOperacion   = @NumeroOperacion,
                observaciones     = @Observaciones,
                usuarioRegistroPago = @UsuarioRegistroPago
            WHERE deudaPagoID = @DeudaPagoId
            AND pagoID      = @PagoId";

        var rows = await _connection.ExecuteAsync(sql, new
        {
            dto.MontoPagado,
            dto.FechaPago,
            dto.MedioPago,
            dto.EntidadFinanciera,
            dto.NumeroOperacion,
            dto.Observaciones,
            dto.UsuarioRegistroPago,
            dto.DeudaPagoId,
            dto.PagoId
        }, _transaction);

        return rows > 0;
    }

    public async Task<bool> EliminarPagoAsync(int deudaPagoId, int pagoId)
    {
        var sql = @"
            DELETE FROM pagodeudacontado
            WHERE deudaPagoID = @DeudaPagoId
            AND pagoID      = @PagoId";

        var rows = await _connection.ExecuteAsync(sql, new
        {
            DeudaPagoId = deudaPagoId,
            PagoId      = pagoId
        }, _transaction);

        return rows > 0;
    }
}