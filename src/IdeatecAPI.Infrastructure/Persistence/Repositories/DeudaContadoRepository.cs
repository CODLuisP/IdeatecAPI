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
}