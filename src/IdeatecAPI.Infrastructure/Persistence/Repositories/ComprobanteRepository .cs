using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories.Comprobantes;

public class ComprobanteRepository : DapperRepository<Comprobante>, IComprobanteRepository
{
    public ComprobanteRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction) { }

    public async Task<int> GenerarComprobanteAsync(Comprobante comprobante)
    {
        var sql = @"
            INSERT INTO comprobante (
                tipoOperacion, tipoComprobante, serie, correlativo,
                fechaEmision, horaEmision, fechaVencimiento, tipoMoneda,
                tipoPago,
                empresaID, empresaRuc, empresaRazonSocial, empresaNombreComercial,
                empresaDireccion, empresaProvincia, empresaDepartamento,
                empresaDistrito, empresaUbigeo, establecimientoAnexo,
                clienteID, clienteTipoDoc, clienteNumDoc, clienteRznSocial,
                clienteDireccion, clienteProvincia, clienteDepartamento,
                clienteDistrito, clienteUbigeo,
                totalOperacionesGravadas, totalOperacionesExoneradas, totalOperacionesInafectas,
                totalIGV, totalDescuentos, totalOtrosCargos,
                totalIcbper, totalImpuestos, valorVenta, subTotal, importeTotal,
                estadoSunat, xmlGenerado, fechaCreacion
            ) VALUES (
                @TipoOperacion, @TipoComprobante, @Serie, @Correlativo,
                @FechaEmision, @HoraEmision, @FechaVencimiento, @TipoMoneda,
                @TipoPago,
                @EmpresaId, @EmpresaRuc, @EmpresaRazonSocial, @EmpresaNombreComercial,
                @EmpresaDireccion, @EmpresaProvincia, @EmpresaDepartamento,
                @EmpresaDistrito, @EmpresaUbigeo, @EmpresaEstablecimientoAnexo,
                @ClienteId, @ClienteTipoDoc, @ClienteNumDoc, @ClienteRazonSocial,
                @ClienteDireccion, @ClienteProvincia, @ClienteDepartamento,
                @ClienteDistrito, @ClienteUbigeo,
                @TotalOperacionesGravadas, @TotalOperacionesExoneradas, @TotalOperacionesInafectas,
                @TotalIGV, @TotalDescuentos, @TotalOtrosCargos,
                @TotalIcbper, @TotalImpuestos, @ValorVenta, @SubTotal, @ImporteTotal,
                @EstadoSunat, @XmlGenerado, @FechaCreacion
            );
            SELECT LAST_INSERT_ID();";

        var parameters = new
        {
            comprobante.TipoOperacion,
            comprobante.TipoComprobante,
            comprobante.Serie,
            comprobante.Correlativo,
            FechaEmision              = comprobante.FechaEmision.Date,
            HoraEmision               = comprobante.HoraEmision.TimeOfDay,
            FechaVencimiento          = comprobante.FechaVencimiento.Date,
            comprobante.TipoMoneda,
            comprobante.TipoPago,
            comprobante.EmpresaId,
            comprobante.EmpresaRuc,
            comprobante.EmpresaRazonSocial,
            comprobante.EmpresaNombreComercial,
            comprobante.EmpresaDireccion,
            comprobante.EmpresaProvincia,
            comprobante.EmpresaDepartamento,
            comprobante.EmpresaDistrito,
            comprobante.EmpresaUbigeo,
            comprobante.EmpresaEstablecimientoAnexo,
            comprobante.ClienteId,
            comprobante.ClienteTipoDoc,
            comprobante.ClienteNumDoc,
            comprobante.ClienteRazonSocial,
            comprobante.ClienteDireccion,
            comprobante.ClienteProvincia,
            comprobante.ClienteDepartamento,
            comprobante.ClienteDistrito,
            comprobante.ClienteUbigeo,
            comprobante.TotalOperacionesGravadas,
            comprobante.TotalOperacionesExoneradas,
            comprobante.TotalOperacionesInafectas,
            comprobante.TotalIGV,
            comprobante.TotalDescuentos,
            comprobante.TotalOtrosCargos,
            comprobante.TotalIcbper,
            comprobante.TotalImpuestos,
            comprobante.ValorVenta,
            comprobante.SubTotal,
            comprobante.ImporteTotal,
            comprobante.EstadoSunat,
            comprobante.XmlGenerado,
            comprobante.FechaCreacion
        };

        int comprobanteId = await _connection.ExecuteScalarAsync<int>(sql, parameters, _transaction);

        foreach (var detalle in comprobante.Detalles)
        {
            detalle.ComprobanteId = comprobanteId;
            await RegistrarDetalleAsync(detalle);
        }

        foreach (var leyenda in comprobante.Leyendas)
        {
            leyenda.ComprobanteId = comprobanteId;
            await RegistrarLeyendaAsync(leyenda);
        }

        foreach (var pago in comprobante.Pagos)
        {
            pago.ComprobanteId = comprobanteId;
            await RegistrarPagoAsync(pago);
        }

        foreach (var cuota in comprobante.Cuotas)
        {
            cuota.ComprobanteId = comprobanteId;
            await RegistrarCuotaAsync(cuota);
        }

        await ActualizarSerieCorrelativoAsync(comprobante);

        return comprobanteId;
    }

    private async Task RegistrarDetalleAsync(ComprobanteDetalle d)
    {
        var sql = @"
            INSERT INTO comprobantedetalle (
                comprobanteId, item, productoId, codigo, descripcion, cantidad,
                unidadMedida, precioUnitario, tipoAfectacionIGV, porcentajeIGV,
                montoIGV, baseIgv, descuentoUnitario, descuentoTotal,
                valorVenta, precioVenta, icbper, factorIcbper
            ) VALUES (
                @ComprobanteId, @Item, @ProductoId, @Codigo, @Descripcion, @Cantidad,
                @UnidadMedida, @PrecioUnitario, @TipoAfectacionIGV, @PorcentajeIGV,
                @MontoIGV, @BaseIgv, @DescuentoUnitario, @DescuentoTotal,
                @ValorVenta, @PrecioVenta, @Icbper, @FactorIcbper
            );";

        await _connection.ExecuteAsync(sql, d, _transaction);
    }

    private async Task RegistrarLeyendaAsync(NoteLegend l)
    {
        var sql = @"
            INSERT INTO notelegend (comprobanteId, code, value)
            VALUES (@ComprobanteId, @Code, @Value);";

        await _connection.ExecuteAsync(sql, l, _transaction);
    }

    private async Task RegistrarPagoAsync(Pago p)
    {
        var sql = @"
            INSERT INTO pago (
                comprobanteId, medioPago, monto, fechaPago,
                numeroOperacion, entidadFinanciera, observaciones
            ) VALUES (
                @ComprobanteId, @MedioPago, @Monto, @FechaPago,
                @NumeroOperacion, @EntidadFinanciera, @Observaciones
            );";

        await _connection.ExecuteAsync(sql, p, _transaction);
    }

    private async Task RegistrarCuotaAsync(Cuota c)
    {
        var sql = @"
            INSERT INTO cuota (
                comprobanteId, numeroCuota, monto, fechaVencimiento,
                montoPagado, fechaPago, estado
            ) VALUES (
                @ComprobanteId, @NumeroCuota, @Monto, @FechaVencimiento,
                @MontoPagado, @FechaPago, @Estado
            );";
        await _connection.ExecuteAsync(sql, c, _transaction);
    }

    private async Task ActualizarSerieCorrelativoAsync(Comprobante comprobante)
    {
        var sql = @"
            UPDATE serie
            SET serie             = @Serie,
                correlativoActual = @Correlativo,
                fechaActualizacion = @FechaCreacion
            WHERE empresaID = @EmpresaId 
            AND tipoComprobante = @TipoComprobante";

        var parameters = new
        {
            comprobante.Serie,
            comprobante.Correlativo,
            comprobante.FechaCreacion,
            comprobante.EmpresaId,
            comprobante.TipoComprobante
        };

        await _connection.ExecuteAsync(sql, parameters, _transaction);
    }
}