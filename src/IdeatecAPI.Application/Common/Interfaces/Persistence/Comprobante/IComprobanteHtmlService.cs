using IdeatecAPI.Application.Features.Comprobante.Services;

namespace IdeatecAPI.Application.Features.Comprobante.Services;

public interface IComprobanteHtmlService
{
    /// <summary>
    /// Genera el HTML de un ticket térmico (58 mm u 80 mm) listo para window.print().
    /// </summary>
    Task<string> GenerarHtmlTicketAsync(int comprobanteId, TamanoPdf tamano);
}
