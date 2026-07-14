using IdeatecAPI.Application.Features.Reportes.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IReportesRepository
{
    Task<ReporteResponseDto> GetReportesPorEmpresaAsync(
        string ruc,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int limite,
        int? usuarioId);

    Task<ReporteResponseDto> GetReportesPorSucursalAsync(
        int sucursalId,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int limite,
        int? usuarioId);

    // Endpoint separado para Excel sin límite
    Task<List<ClienteExportDto>> GetClientesExportPorEmpresaAsync(
        string ruc,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int? usuarioId);

    Task<List<ClienteExportDto>> GetClientesExportPorSucursalAsync(
        int sucursalId,
        string periodo,
        DateTime? desde,
        DateTime? hasta,
        int? usuarioId);
    
    Task<IEnumerable<Comprobante>> GetListadoParaReportesAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null,
        string filtroNV = "excluir");

    Task<IEnumerable<ProductoTopDTO>> GetProductosTopAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null,
        string orderBy = "monto");
    Task<IEnumerable<MedioPagoTopDTO>> GetMediosPagoTopAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null);
    
    Task<IEnumerable<Comprobante>> GetListadoControlCajaAsync(
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null,
        int? limit = null,
        string filtroNV = "excluir");

    /// Devuelve (ComprobanteId, MedioPago, Monto) para un conjunto de comprobantes.
    Task<IEnumerable<(int ComprobanteId, string? MedioPago, decimal Monto)>>
        GetPagosByComprobanteIdsAsync(IEnumerable<int> comprobanteIds);
}