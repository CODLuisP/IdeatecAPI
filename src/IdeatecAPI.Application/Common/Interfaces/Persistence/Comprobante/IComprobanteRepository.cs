using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IComprobanteRepository : IRepository<Comprobante>
{
    Task<int> GenerarComprobanteAsync(Comprobante dto);
    Task<Comprobante?> GetComprobanteByIdAsync(int comprobanteId);
    Task<IEnumerable<Comprobante>> GetComprobanteByEstadoAsync(string estado);
    Task<Comprobante?> GetByRucSerieNumeroAsync(string ruc, string serie, int numero);
    Task<Comprobante?> GetByComprobanteUnicoAsync(string ruc, string serie, int numero);

    //Metodos internos para
    new Task<Comprobante?> GetByIdAsync(int comprobanteId);
    Task<IEnumerable<Comprobante>> GetByEstadoAsync(string estado);
    Task<IEnumerable<ComprobanteDetalle>> GetDetallesByIdAsync(int comprobanteId);
    Task<IEnumerable<Comprobante>> GetByRucAndFechasAsync(string ruc, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null);
    Task<IEnumerable<Comprobante>> GetByDocClienteAndFechasAsync(string rucEmpresa, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null);
    Task<IEnumerable<Comprobante>> GetBySucursalAndFechasAsync(string empresaRuc, string codEstablecimiento, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null, int? usuarioId = null);
    /// <summary>Cantidad de líneas de detalle por comprobante, para listados que necesitan mostrar "N artículos" sin traer el detalle completo.</summary>
    Task<IReadOnlyDictionary<int, int>> GetCantidadItemsPorComprobantesAsync(IEnumerable<int> comprobanteIds);
    Task<IEnumerable<Comprobante>> GetByDocUsuarioAndFechasAsync(string rucEmpresa, int usuarioCreacion, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null);
    Task<IEnumerable<Comprobante>> GetByClienteAndSucursalAsync(string empresaRuc, string codEstablecimiento, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null);
    Task UpdateCorreoWhatsappAsync(int comprobanteId, string? correo, bool? enviadoPorCorreo, string? whatsApp, bool? enviadoPorWhatsApp);
    Task<int> GetCantidadByClienteNumDocAsync(string clienteNumDoc);
    Task<IEnumerable<Pago>> GetPagosByIdAsync(int comprobanteId);
    Task<IEnumerable<Cuota>> GetCuotasByIdAsync(int comprobanteId);
    Task<IEnumerable<NoteLegend>> GetLeyendasByIdAsync(int comprobanteId);
    Task<IEnumerable<GuiaComprobante>> GetGuiasByIdAsync(int comprobanteId);
    Task<IEnumerable<Detraccion>> GetDetraccionesByIdAsync(int comprobanteId);
    Task<(
        IEnumerable<ComprobanteDetalle> Detalles,
        IEnumerable<Pago> Pagos,
        IEnumerable<Cuota> Cuotas,
        IEnumerable<NoteLegend> Leyendas,
        IEnumerable<GuiaComprobante> Guias,
        IEnumerable<Detraccion> Detracciones
    )> GetDatosCompletosByComprobanteIdAsync(int comprobanteId);
    Task UpdateEstadoSunatAsync(int comprobanteId, string estado, string? codigo, string? mensaje, string? xmlFirmado, string? cdrBase64, string? hash = null, string? mensajeAdicional = null);
    Task AnularComprobanteAsync(int comprobanteId, string? motivo, int? usuarioId);
    Task<int?> GetSucursalIdByRucAndAnexoAsync(string empresaRuc, string codEstablecimiento);
    Task UpdateXmlGeneradoAsync(int comprobanteId, string rutaZip);
    Task UpdateXmlRespuestaSunatAsync(int comprobanteId, string rutaCdr);
    Task<bool> UpdateOrdenServicioSpotAsync(
        string ruc, string serie, int correlativo, string? ordenServicio, bool? spot,
        string? spotLeyenda = null, string? spotBienServicio = null, string? spotMedioPago = null,
        string? spotCuentaBanco = null, decimal? spotPorcentaje = null);
    Task InsertValesAsync(int comprobanteId, IEnumerable<int> valeIds);
    Task<IEnumerable<int>> GetValesByComprobanteIdAsync(int comprobanteId);
    Task<IEnumerable<Vale>> GetValesFullByComprobanteIdAsync(int comprobanteId);
    Task<int> ObtenerYIncrementarCorrelativoAsync(int sucursalId, string tipoComprobante, string serie);
    // Version de un solo viaje: localiza la sucursal por RUC+anexo, comprueba que la empresa
    // este activa, reserva el siguiente correlativo y devuelve serie y numero asignado.
    Task<AsignacionSerieDTO?> AsignarSerieYCorrelativoAsync(string empresaRuc, string codEstablecimiento, string tipoComprobante);
    Task<IEnumerable<Comprobante>> GetNotasByComprobanteAfectadoIdAsync(int comprobanteAfectadoId, string tipoComprobante);
    Task<IEnumerable<Comprobante>> GetNotasVentaBySucursalAsync(string empresaRuc, string codEstablecimiento, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null, int? offset = null);
    /// <summary>Mapa item (número de línea) → detalleID, para atar cada movimiento de kardex a su línea de venta exacta justo después de insertar el comprobante.</summary>
    Task<IReadOnlyDictionary<int, int>> GetItemDetalleIdMapAsync(int comprobanteId);
}
