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

    Task<IEnumerable<CuotaPagoDto>> GetHistorialPagosByCuotaIdAsync(int cuotaId);

    Task<byte[]> GenerarExcelAsync(ReporteCuentasPorCobrarFiltroDto filtro);
}

public class CuentasPorCobrarService : ICuentasPorCobrarService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICuentasPorCobrarExcelService _excelService;

    public CuentasPorCobrarService(IUnitOfWork unitOfWork, ICuentasPorCobrarExcelService excelService)
    {
        _unitOfWork   = unitOfWork;
        _excelService = excelService;
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

            var montoPagadoAnterior = cuota.MontoPagado ?? 0;
            var nuevoMontoPagado    = montoPagadoAnterior + dto.MontoPagado;

            if (nuevoMontoPagado > cuota.Monto)
                throw new InvalidOperationException(
                    $"El monto a pagar excede el saldo pendiente. Saldo: {cuota.Monto - montoPagadoAnterior}");

            string nuevoEstado;
            if (nuevoMontoPagado >= cuota.Monto)
                nuevoEstado = "PAGADO";
            else if (nuevoMontoPagado > 0)
                nuevoEstado = "PARCIAL";
            else
                nuevoEstado = "PENDIENTE";

            var result = await _unitOfWork.CuentasPorCobrar.PagarCuotaAsync(dto, nuevoEstado, nuevoMontoPagado);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<CuotaPagoDto>> GetHistorialPagosByCuotaIdAsync(int cuotaId)
    {
        if (cuotaId <= 0)
            throw new ArgumentException("CuotaId inválido");

        return await _unitOfWork.CuentasPorCobrar.GetHistorialPagosByCuotaIdAsync(cuotaId);
    }

    public async Task<byte[]> GenerarExcelAsync(ReporteCuentasPorCobrarFiltroDto filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro.EmpresaRuc))
            throw new ArgumentException("El RUC de la empresa es obligatorio");

        if (filtro.FechaInicio.HasValue && !filtro.FechaFin.HasValue)
            filtro.FechaFin = filtro.FechaInicio;

        var items = await _unitOfWork.CuentasPorCobrar.GetReporteCuentasPorCobrarAsync(filtro);
        return _excelService.GenerarReporteCuentasPorCobrar(items, filtro);
    }
}