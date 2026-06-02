using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence.Reportes;

public interface IControlCajaTicketHtmlService
{
    Task<string> GenerarHtmlAsync(
        string titulo,
        IEnumerable<ControlCajaTicketItemDto> datos,
        string ruc,
        string? codEstablecimiento,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        string nombreResponsable,
        string? nombreUsuario = null);
}
