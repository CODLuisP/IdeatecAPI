using IdeatecAPI.Application.Features.DeudaContado.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IDeudaContadoRepository
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

    Task<Pago?> GetPagoByIdAsync(int pagoId);

    Task<IEnumerable<PagoDeudaContadoDto>> GetHistorialPagosByPagoIdAsync(int pagoId);

    Task<bool> RegistrarPagoAsync(RegistrarPagoDeudaContadoDto dto);

    Task<IEnumerable<ReporteDeudaContadoItemDto>> GetReporteDeudaContadoAsync(ReporteDeudaContadoFiltroDto filtro);
    Task<bool> EditarPagoAsync(EditarPagoDeudaContadoDto dto);
    Task<bool> EliminarPagoAsync(int deudaPagoId, int pagoId);

}