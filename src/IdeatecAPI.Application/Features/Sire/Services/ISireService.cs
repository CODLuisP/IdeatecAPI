using IdeatecAPI.Application.Features.Sire.DTOs;

namespace IdeatecAPI.Application.Features.Sire.Services;

public interface ISireService
{
    Task<SirePeriodosResponse> ConsultarPeriodosAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret);

    Task<SireDescargarPropuestaResponse> DescargarPropuestaAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario);

    Task<SireAceptarPropuestaResponse> AceptarPropuestaAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario);

    Task<SireRegistrarPreliminarResponse> RegistrarPreliminarAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario);

    Task<SireEliminarComprobanteResponse> EliminarComprobanteAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario, bool enPreliminar, List<SireComprobanteEliminarDto> comprobantes);

    Task<SireImportarComprobantesResponse> ImportarComprobantesAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario, string razonSocialEmisor, bool enPreliminar, List<SireComprobanteNuevoDto> comprobantes);

    Task<SireEditarTipoCambioResponse> EditarTipoCambioAsync(
        string ruc, string solUsuario, string solClave, string clienteId, string clientSecret,
        string perTributario, SireEditarTipoCambioDto datos);
}
