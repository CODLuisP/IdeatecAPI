using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.CuentasPorCobrar.DTOs;

namespace IdeatecAPI.Application.Features.CuentasPorCobrar.Services;

public interface ICuentasPorCobrarService
{
    Task<IEnumerable<ListaCuentasPorCobrarDto>> GetCuentasPorCobrarAsync(
        string empresaRuc,
        string? establecimientoAnexo,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? clienteNumDoc);

    Task<IEnumerable<CuotaDto>> GetCuotasByComprobanteIdAsync(int comprobanteId);

    Task<bool> PagarCuotaAsync(PagarCuotaDto dto);
}

public class CuentasPorCobrarService : ICuentasPorCobrarService
{
    private readonly IUnitOfWork _unitOfWork;

    public CuentasPorCobrarService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ListaCuentasPorCobrarDto>> GetCuentasPorCobrarAsync(
        string empresaRuc,
        string? establecimientoAnexo,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? clienteNumDoc)
    {
        return await _unitOfWork.CuentasPorCobrar.GetCuentasPorCobrarAsync(
            empresaRuc,
            establecimientoAnexo,
            fechaInicio,
            fechaFin,
            clienteNumDoc);
    }

    public async Task<IEnumerable<CuotaDto>> GetCuotasByComprobanteIdAsync(int comprobanteId)
    {
        if (comprobanteId <= 0)
            throw new ArgumentException("ComprobanteId inválido");

        return await _unitOfWork.CuentasPorCobrar.GetCuotasByComprobanteIdAsync(comprobanteId);
    }

    public async Task<bool> PagarCuotaAsync(PagarCuotaDto dto)
    {
        if (dto.CuotaId <= 0)
            throw new ArgumentException("CuotaId inválido");

        if (dto.MontoPagado <= 0)
            throw new ArgumentException("El monto pagado debe ser mayor a 0");

        _unitOfWork.BeginTransaction();

        try
        {
            var cuota = await _unitOfWork.CuentasPorCobrar.GetCuotaByIdAsync(dto.CuotaId);

            if (cuota == null)
                throw new ArgumentException("Cuota no encontrada");

            if (cuota.Estado == "PAGADO")
                throw new InvalidOperationException("La cuota ya está pagada");

            var montoFinal = dto.MontoFinal ?? cuota.Monto;

            var montoPagadoAnterior = decimal.TryParse(cuota.MontoPagado, out var mp) ? mp : 0;
            var nuevoMontoPagado = montoPagadoAnterior + dto.MontoPagado;

            string nuevoEstado;
            if (nuevoMontoPagado >= montoFinal)
                nuevoEstado = "PAGADO";
            else if (nuevoMontoPagado > 0)
                nuevoEstado = "PARCIAL";
            else
                nuevoEstado = "PENDIENTE";

            dto.MontoPagado = nuevoMontoPagado;
            dto.MontoFinal = montoFinal;

            var result = await _unitOfWork.CuentasPorCobrar.PagarCuotaAsync(dto, nuevoEstado);

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