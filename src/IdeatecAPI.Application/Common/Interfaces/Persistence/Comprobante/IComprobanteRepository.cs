using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    Task<IEnumerable<Comprobante>> GetByRucAndFechasAsync(string ruc, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null);
    Task<IEnumerable<Comprobante>> GetByDocClienteAndFechasAsync(string rucEmpresa, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta);
    Task<IEnumerable<Comprobante>> GetBySucursalAndFechasAsync(string empresaRuc, string codEstablecimiento, DateTime? fechaDesde, DateTime? fechaHasta, int? limit = null);
    Task<IEnumerable<Comprobante>> GetByDocUsuarioAndFechasAsync(string rucEmpresa, int usuarioCreacion, DateTime? fechaDesde, DateTime? fechaHasta);
    Task<IEnumerable<Comprobante>> GetByClienteAndSucursalAsync(string empresaRuc, string codEstablecimiento, string clienteNumDoc, DateTime? fechaDesde, DateTime? fechaHasta);
    Task UpdateCorreoWhatsappAsync(int comprobanteId, string? correo, bool? enviadoPorCorreo, string? whatsApp, bool? enviadoPorWhatsApp);
    Task<int> GetCantidadByClienteNumDocAsync(string clienteNumDoc);
    Task<IEnumerable<Pago>> GetPagosByIdAsync(int comprobanteId);
    Task<IEnumerable<Cuota>> GetCuotasByIdAsync(int comprobanteId);
    Task<IEnumerable<NoteLegend>> GetLeyendasByIdAsync(int comprobanteId);
    Task<IEnumerable<GuiaComprobante>> GetGuiasByIdAsync(int comprobanteId);
    Task<IEnumerable<Detraccion>> GetDetraccionesByIdAsync(int comprobanteId);
    Task UpdateEstadoSunatAsync(int comprobanteId, string estado, string? codigo, string? mensaje, string? xmlFirmado, string? cdrBase64);
    Task<int?> GetSucursalIdByRucAndAnexoAsync(string empresaRuc, string codEstablecimiento);
}
