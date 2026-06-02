using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence.Reportes;

public interface IReportesPdfService
{
    Task<byte[]> ExportarListadoPdfAsync(
        string titulo,
        IEnumerable<ListarComprobanteDTO> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null);

    Task<byte[]> ExportarControlCajaPdfAsync(
        string titulo,
        IEnumerable<ListarComprobanteDTO> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null);

    Task<byte[]> ExportarProductosTopPdfAsync(
        string titulo,
        IEnumerable<ProductoTopDTO> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null);

    Task<byte[]> ExportarMediosPagoPdfAsync(
        string titulo,
        IEnumerable<MedioPagoTopDTO> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null);

    Task<byte[]> ExportarControlCajaTicketPdfAsync(
        string titulo,
        IEnumerable<ControlCajaTicketItemDto> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string nombreResponsable = "",
        string? empresaNombre = null,
        string? empresaDireccion = null,
        string? logoBase64 = null,
        string? nombreUsuario = null);
}
