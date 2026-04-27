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
            SELECT cuotaID              AS CuotaId,
                comprobanteID        AS ComprobanteId,
                numeroCuota          AS NumeroCuota,
                monto                AS Monto,
                fechaVencimiento     AS FechaVencimiento,
                montoPagado          AS MontoPagado,
                fechaPago            AS FechaPago,
                estado               AS Estado,
                montoDescuento       AS MontoDescuento,
                motivoDescuento      AS MotivoDescuento,
                montoFinal           AS MontoFinal,
                tasaDescuentoDiaria  AS TasaDescuentoDiaria,
                diasAnticipacion     AS DiasAnticipacion,
                porcentajeDescuento  AS PorcentajeDescuento,
                medioPago            AS MedioPago,
                entidadFinanciera    AS EntidadFinanciera,
                numeroOperacion      AS NumeroOperacion,
                observaciones        AS Observaciones,
                usuarioRegistroPago  AS UsuarioRegistroPago
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
                montoDescuento      AS MontoDescuento,
                motivoDescuento     AS MotivoDescuento,
                montoFinal          AS MontoFinal,
                tasaDescuentoDiaria AS TasaDescuentoDiaria,
                diasAnticipacion    AS DiasAnticipacion,
                porcentajeDescuento AS PorcentajeDescuento,
                medioPago           AS MedioPago,
                entidadFinanciera   AS EntidadFinanciera,
                numeroOperacion     AS NumeroOperacion,
                observaciones       AS Observaciones,
                usuarioRegistroPago AS UsuarioRegistroPago
            FROM cuota
            WHERE cuotaID = @CuotaId";

        return await _connection.QueryFirstOrDefaultAsync<Cuota>(
            sql,
            new { CuotaId = cuotaId },
            _transaction
        );
    }

    public async Task<bool> PagarCuotaAsync(PagarCuotaDto dto, string nuevoEstado)
    {
        var sql = @"
            UPDATE cuota SET
                montoPagado         = @MontoPagado,
                fechaPago           = @FechaPago,
                estado              = @Estado,
                montoDescuento      = @MontoDescuento,
                motivoDescuento     = @MotivoDescuento,
                montoFinal          = @MontoFinal,
                tasaDescuentoDiaria = @TasaDescuentoDiaria,
                diasAnticipacion    = @DiasAnticipacion,
                porcentajeDescuento = @PorcentajeDescuento,
                medioPago           = @MedioPago,
                entidadFinanciera   = @EntidadFinanciera,
                numeroOperacion     = @NumeroOperacion,
                observaciones       = @Observaciones,
                usuarioRegistroPago = @UsuarioRegistroPago
            WHERE cuotaID = @CuotaId";

        var result = await _connection.ExecuteAsync(sql, new
        {
            dto.MontoPagado,
            dto.FechaPago,
            Estado = nuevoEstado,
            dto.MontoDescuento,
            dto.MotivoDescuento,
            dto.MontoFinal,
            dto.TasaDescuentoDiaria,
            dto.DiasAnticipacion,
            dto.PorcentajeDescuento,
            dto.MedioPago,
            dto.EntidadFinanciera,
            dto.NumeroOperacion,
            dto.Observaciones,
            dto.UsuarioRegistroPago,
            dto.CuotaId
        }, _transaction);

        return result > 0;
    }
}