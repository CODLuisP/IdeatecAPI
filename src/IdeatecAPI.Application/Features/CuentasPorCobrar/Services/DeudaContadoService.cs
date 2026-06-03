using ClosedXML.Excel;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.DeudaContado.DTOs;

namespace IdeatecAPI.Application.Features.DeudaContado.Services;

public interface IDeudaContadoService
{
    Task<IEnumerable<ListaDeudaContadoDto>> GetDeudaContadoAsync(
        string empresaRuc,
        string? establecimientoAnexo,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? clienteNumDoc,
        string? serie = null,
        int? correlativo = null,
        string estadoPago = "PENDIENTE");

    Task<IEnumerable<PagoDeudaContadoDto>> GetHistorialPagosByPagoIdAsync(int pagoId);

    Task<bool> RegistrarPagoAsync(RegistrarPagoDeudaContadoDto dto);

    Task<byte[]> GenerarExcelAsync(ReporteDeudaContadoFiltroDto filtro);
    Task<bool> EditarPagoAsync(EditarPagoDeudaContadoDto dto);
    Task<bool> EliminarPagoAsync(int deudaPagoId, int pagoId);
}

public class DeudaContadoService : IDeudaContadoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeudaContadoExcelService _excelService;

    public DeudaContadoService(IUnitOfWork unitOfWork, IDeudaContadoExcelService excelService)
    {
        _unitOfWork   = unitOfWork;
        _excelService = excelService;
    }

    public async Task<IEnumerable<ListaDeudaContadoDto>> GetDeudaContadoAsync(
        string empresaRuc,
        string? establecimientoAnexo,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? clienteNumDoc,
        string? serie = null,
        int? correlativo = null,
        string estadoPago = "PENDIENTE")
    {
        var estadoNormalizado = estadoPago.ToUpper() switch
        {
            "PAGADO" => "PAGADO",
            "TODOS"  => "TODOS",
            _        => "PENDIENTE"
        };

        return await _unitOfWork.DeudaContado.GetDeudaContadoAsync(
            empresaRuc,
            establecimientoAnexo,
            fechaInicio,
            fechaFin,
            clienteNumDoc,
            serie,
            correlativo,
            estadoNormalizado);
    }

    public async Task<IEnumerable<PagoDeudaContadoDto>> GetHistorialPagosByPagoIdAsync(int pagoId)
    {
        if (pagoId <= 0)
            throw new ArgumentException("PagoId inválido");

        return await _unitOfWork.DeudaContado.GetHistorialPagosByPagoIdAsync(pagoId);
    }

    public async Task<bool> RegistrarPagoAsync(RegistrarPagoDeudaContadoDto dto)
    {
        if (dto.PagoId <= 0)
            throw new ArgumentException("PagoId inválido");

        if (dto.MontoPagado <= 0)
            throw new ArgumentException("El monto pagado debe ser mayor a 0");

        _unitOfWork.BeginTransaction();

        try
        {
            var pago = await _unitOfWork.DeudaContado.GetPagoByIdAsync(dto.PagoId);

            if (pago == null)
                throw new ArgumentException("Pago no encontrado");

            if (pago.Monto == null)
                throw new InvalidOperationException("El pago no tiene monto registrado");

            // Validar que la moneda del pago coincida con la del comprobante
            var comprobanteId = pago.ComprobanteId ?? 0;
            var comprobante = await _unitOfWork.Comprobantes.GetByIdAsync(comprobanteId);
            if (comprobante != null
                && !string.IsNullOrEmpty(comprobante.TipoMoneda)
                && comprobante.TipoMoneda != dto.TipoMoneda)
                throw new InvalidOperationException(
                    $"La moneda del pago ({dto.TipoMoneda}) no coincide con la del comprobante ({comprobante.TipoMoneda})");

            // Calcular saldo real considerando notas de crédito/débito parciales
            var notasNC = await _unitOfWork.Comprobantes.GetNotasByComprobanteAfectadoIdAsync(comprobanteId, "07");
            var notasND = await _unitOfWork.Comprobantes.GetNotasByComprobanteAfectadoIdAsync(comprobanteId, "08");

            var motivosDescuentoParcial = new[] { "04", "05", "07", "08", "09" };
            var estadosValidos = new[] { "ACEPTADO", "ACEPTADO_CON_OBSERVACIONES" };

            var descuentoNC = notasNC
                .Where(n => motivosDescuentoParcial.Contains(n.TipoNotaCreditoDebito)
                         && estadosValidos.Contains(n.EstadoSunat))
                .Sum(n => n.ImporteTotal ?? 0);

            var incrementoND = notasND
                .Where(n => estadosValidos.Contains(n.EstadoSunat))
                .Sum(n => n.ImporteTotal ?? 0);

            var saldoReal = (pago.Monto ?? 0) - descuentoNC + incrementoND;

            var historial = await _unitOfWork.DeudaContado.GetHistorialPagosByPagoIdAsync(dto.PagoId);
            var montoPagadoAnterior = historial.Sum(h => h.MontoPagado);

            if (montoPagadoAnterior + dto.MontoPagado > saldoReal)
                throw new InvalidOperationException(
                    $"El monto a pagar excede el saldo pendiente. Saldo: {saldoReal - montoPagadoAnterior:N2} {dto.TipoMoneda}");

            var result = await _unitOfWork.DeudaContado.RegistrarPagoAsync(dto);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<byte[]> GenerarExcelAsync(ReporteDeudaContadoFiltroDto filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro.EmpresaRuc))
            throw new ArgumentException("El RUC de la empresa es obligatorio");

        // Si solo viene fechaInicio, fechaFin = fechaInicio
        if (filtro.FechaInicio.HasValue && !filtro.FechaFin.HasValue)
            filtro.FechaFin = filtro.FechaInicio;

        var items = await _unitOfWork.DeudaContado.GetReporteDeudaContadoAsync(filtro);
        return _excelService.GenerarReporteDeudaContado(items, filtro);
    }

    public async Task<bool> EditarPagoAsync(EditarPagoDeudaContadoDto dto)
    {
        if (dto.DeudaPagoId <= 0)
            throw new ArgumentException("DeudaPagoId inválido");
    
        if (dto.PagoId <= 0)
            throw new ArgumentException("PagoId inválido");
    
        if (dto.MontoPagado <= 0)
            throw new ArgumentException("El monto pagado debe ser mayor a 0");
    
        _unitOfWork.BeginTransaction();
    
        try
        {
            // Verificar que el pago padre exista
            var pago = await _unitOfWork.DeudaContado.GetPagoByIdAsync(dto.PagoId);
            if (pago == null)
                throw new ArgumentException("Pago no encontrado");
    
            if (pago.Monto == null)
                throw new InvalidOperationException("El pago no tiene monto registrado");
    
            // Sumar todos los pagos EXCEPTO el que se está editando
            var historial = await _unitOfWork.DeudaContado.GetHistorialPagosByPagoIdAsync(dto.PagoId);
            var montoPagadoOtros = historial
                .Where(h => h.DeudaPagoID != dto.DeudaPagoId)
                .Sum(h => h.MontoPagado);
    
            if (montoPagadoOtros + dto.MontoPagado > pago.Monto)
                throw new InvalidOperationException(
                    $"El monto editado excede el saldo disponible. Máximo permitido: {pago.Monto - montoPagadoOtros:F2}");
    
            var result = await _unitOfWork.DeudaContado.EditarPagoAsync(dto);
    
            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }
    
    public async Task<bool> EliminarPagoAsync(int deudaPagoId, int pagoId)
    {
        if (deudaPagoId <= 0)
            throw new ArgumentException("DeudaPagoId inválido");
    
        if (pagoId <= 0)
            throw new ArgumentException("PagoId inválido");
    
        _unitOfWork.BeginTransaction();
    
        try
        {
            var result = await _unitOfWork.DeudaContado.EliminarPagoAsync(deudaPagoId, pagoId);
    
            if (!result)
                throw new ArgumentException("No se encontró el pago a eliminar");
    
            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }
}