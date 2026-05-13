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
        string? clienteNumDoc);

    Task<IEnumerable<PagoDeudaContadoDto>> GetHistorialPagosByPagoIdAsync(int pagoId);

    Task<bool> RegistrarPagoAsync(RegistrarPagoDeudaContadoDto dto);
}

public class DeudaContadoService : IDeudaContadoService
{
    private readonly IUnitOfWork _unitOfWork;

    public DeudaContadoService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ListaDeudaContadoDto>> GetDeudaContadoAsync(
        string empresaRuc,
        string? establecimientoAnexo,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? clienteNumDoc)
    {
        return await _unitOfWork.DeudaContado.GetDeudaContadoAsync(
            empresaRuc,
            establecimientoAnexo,
            fechaInicio,
            fechaFin,
            clienteNumDoc);
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

            var historial = await _unitOfWork.DeudaContado.GetHistorialPagosByPagoIdAsync(dto.PagoId);
            var montoPagadoAnterior = historial.Sum(h => h.MontoPagado);

            if (montoPagadoAnterior + dto.MontoPagado > pago.Monto)
                throw new InvalidOperationException(
                    $"El monto a pagar excede el saldo pendiente. Saldo: {pago.Monto - montoPagadoAnterior}");

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
}