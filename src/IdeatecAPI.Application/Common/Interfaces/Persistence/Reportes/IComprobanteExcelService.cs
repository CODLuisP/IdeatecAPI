using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Application.Features.Comprobante.Services;

public interface IComprobanteExcelService
{
    Task<byte[]> ExportarListadoReportesAsync(
        string titulo,
        IEnumerable<ListarComprobanteDTO> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null);

    Task<byte[]> ExportarProductosTopAsync(
        string titulo,
        IEnumerable<ProductoTopDTO> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null);
    
    Task<byte[]> ExportarMediosPagoTopAsync(
        string titulo,
        IEnumerable<MedioPagoTopDTO> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null);
}