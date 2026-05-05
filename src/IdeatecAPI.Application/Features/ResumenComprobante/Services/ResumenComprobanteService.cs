using System.IO.Compression;
using System.Text;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Notas.Services;
using IdeatecAPI.Application.Features.ResumenComprobante.DTO;
using Microsoft.Extensions.Configuration;

namespace IdeatecAPI.Application.Features.ResumenComprobante.Services;

public interface IResumenComprobanteService
{
    Task<IEnumerable<ObtenerResumenComprobanteDTO>> GetAllResumenComprobanteAsync();
    Task<ObtenerResumenComprobanteDTO?> GetResumenComprobanteByIdAsync(int id);
    Task<ComprobanteResponse> RegistrarResumenComprobanteAsync(AgregarResumenComprobanteDTO dto);
    Task<ComprobanteResponse> SendToSunatAsync(int resumenId);
}

public class ComprobanteResponse
{
    public bool Exitoso { get; set; }
    public string? Mensaje { get; set; }
    public int? ComprobanteId { get; set; }
    public string? XmlBase64 { get; set; }
    public string? XmlString { get; set; }
    public string? RutaZip { get; set; }
    public string? EstadoSunat { get; set; }
    public string? CodigoRespuesta { get; set; }
    public string? MensajeRespuesta { get; set; }
    public string? CdrBase64 { get; set; }
}

public class ResumenComprobanteService : IResumenComprobanteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IResumenXmlService _xmlService;
    private readonly IXmlSignerService _xmlSigner;
    private readonly ISunatResumenService _sunatResumen;
    private readonly string _rutaXml;

    public ResumenComprobanteService(
        IUnitOfWork unitOfWork,
        IResumenXmlService xmlService,
        IXmlSignerService xmlSigner,
        ISunatResumenService sunatResumen,
        IConfiguration configuration)
    {
        _unitOfWork    = unitOfWork;
        _xmlService    = xmlService;
        _xmlSigner     = xmlSigner;
        _sunatResumen  = sunatResumen;
        _rutaXml       = configuration["Storage:RutaXml"]
                        ?? Path.Combine(Directory.GetCurrentDirectory(), "XmlFiles");
    }

    // ── 1. OBTENER TODOS ─────────────────────────────────────────────────────
    public async Task<IEnumerable<ObtenerResumenComprobanteDTO>> GetAllResumenComprobanteAsync()
    {
        var resumenes = await _unitOfWork.ResumenComprobante.GetAllResumenComprobanteAsync();
        return resumenes.Select(MapToDto);
    }

    // ── 2. OBTENER POR ID ────────────────────────────────────────────────────
    public async Task<ObtenerResumenComprobanteDTO?> GetResumenComprobanteByIdAsync(int id)
    {
        var resumen = await _unitOfWork.ResumenComprobante.GetResumenComprobanteByIdAsync(id);
        if (resumen is null) return null;
        return MapToDto(resumen);
    }

    // ── 3. REGISTRAR ─────────────────────────────────────────────────────────
    public async Task<ComprobanteResponse> RegistrarResumenComprobanteAsync(AgregarResumenComprobanteDTO dto)
    {
        _unitOfWork.BeginTransaction();
        try
        {
            var existe = await _unitOfWork.ResumenComprobante
                .ExisteIdentificadorAsync(dto.Identificador);

            if (existe)
                throw new InvalidOperationException(
                    $"Ya existe un resumen con el identificador '{dto.Identificador}'");

            var resumen = new Domain.Entities.ResumenComprobante
            {
                EmpresaId              = dto.EmpresaId,
                EmpresaRuc             = dto.EmpresaRuc,
                EmpresaRazonSocial     = dto.EmpresaRazonSocial,
                EmpresaDireccion       = dto.EmpresaDireccion,
                EmpresaProvincia       = dto.EmpresaProvincia,
                EmpresaDepartamento    = dto.EmpresaDepartamento,
                EmpresaDistrito        = dto.EmpresaDistrito,
                EmpresaUbigeo          = dto.EmpresaUbigeo,
                EstablecimientoAnexo   = dto.EstablecimientoAnexo,
                NumeroEnvio            = dto.NumeroEnvio,
                FechaEmisionDocumentos = dto.FechaEmisionDocumentos,
                FechaGeneracion        = dto.FechaGeneracion,
                Identificador          = dto.Identificador,
                EstadoSunat            = dto.EstadoSunat,
                Ticket                 = dto.Ticket,
                CodigoRespuesta        = dto.CodigoRespuesta,
                MensajeRespuesta       = dto.MensajeRespuesta,
                XmlGenerado            = dto.XmlGenerado,
                PdfGenerado            = dto.PdfGenerado,
                UsuarioCreacion        = dto.UsuarioCreacion,
                FechaEnvio             = dto.FechaEnvio,

                DetallesResumen = dto.DetallesResumen.Select(d => new Domain.Entities.ResumenComprobanteDetalle
                {
                    LineID                   = d.LineID,
                    ComprobanteId            = d.ComprobanteId,
                    TipoComprobante          = d.TipoComprobante,
                    Serie                    = d.Serie,
                    Correlativo              = d.Correlativo,
                    ClienteTipoDoc           = d.ClienteTipoDoc,
                    ClienteNumDoc            = d.ClienteNumDoc,
                    ClienteNombre            = d.ClienteNombre,
                    DocumentoAfectadoTipo    = d.DocumentoAfectadoTipo,
                    DocumentoAfectadoNumero  = d.DocumentoAfectadoNumero,
                    CodigoCondicion          = d.CodigoCondicion,
                    Moneda                   = d.Moneda,
                    MontoTotalVenta          = d.MontoTotalVenta,
                    TotalGravado             = d.TotalGravado,
                    TotalExonerado           = d.TotalExonerado,
                    TotalInafecto            = d.TotalInafecto,
                    TotalGratuito            = d.TotalGratuito,
                    TotalIGV                 = d.TotalIGV,
                    IGVReferencial           = d.IGVReferencial
                }).ToList()
            };

            int nuevoId = await _unitOfWork.ResumenComprobante.RegistrarResumenComprobanteAsync(resumen);
            _unitOfWork.Commit();

            return new ComprobanteResponse
            {
                Exitoso       = true,
                Mensaje       = "Resumen comprobante registrado correctamente",
                ComprobanteId = nuevoId,
                EstadoSunat   = "PENDIENTE"
            };
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // ── MAPPER MANUAL ────────────────────────────────────────────────────────
    private static ObtenerResumenComprobanteDTO MapToDto(Domain.Entities.ResumenComprobante r)
    {
        return new ObtenerResumenComprobanteDTO
        {
            ResumenComprobanteId   = r.ResumenComprobanteId,
            EmpresaId              = r.EmpresaId,
            EmpresaRuc             = r.EmpresaRuc,
            EmpresaRazonSocial     = r.EmpresaRazonSocial,
            EmpresaDireccion       = r.EmpresaDireccion,
            EmpresaProvincia       = r.EmpresaProvincia,
            EmpresaDepartamento    = r.EmpresaDepartamento,
            EmpresaDistrito        = r.EmpresaDistrito,
            EmpresaUbigeo          = r.EmpresaUbigeo,
            EstablecimientoAnexo   = r.EstablecimientoAnexo,
            NumeroEnvio            = r.NumeroEnvio,
            FechaEmisionDocumentos = r.FechaEmisionDocumentos,
            FechaGeneracion        = r.FechaGeneracion,
            Identificador          = r.Identificador    ?? string.Empty,
            EstadoSunat            = r.EstadoSunat      ?? string.Empty,
            Ticket                 = r.Ticket           ?? string.Empty,
            CodigoRespuesta        = r.CodigoRespuesta  ?? string.Empty,
            MensajeRespuesta       = r.MensajeRespuesta ?? string.Empty,
            XmlGenerado            = r.XmlGenerado      ?? string.Empty,
            PdfGenerado            = r.PdfGenerado,
            UsuarioCreacion        = r.UsuarioCreacion,
            FechaEnvio             = r.FechaEnvio,

            DetallesResumen = r.DetallesResumen?.Select(d => new ObtenerResumenDetalleDTO
            {
                ResumenComprobanteDetalleId = d.ResumenComprobanteDetalleId,
                LineID                      = d.LineID,
                ComprobanteId               = d.ComprobanteId,
                ResumenComprobanteId        = d.ResumenComprobanteId,
                TipoComprobante             = d.TipoComprobante,
                Serie                       = d.Serie,
                Correlativo                 = d.Correlativo,
                ClienteTipoDoc              = d.ClienteTipoDoc,
                ClienteNumDoc               = d.ClienteNumDoc,
                ClienteNombre               = d.ClienteNombre,
                DocumentoAfectadoTipo       = d.DocumentoAfectadoTipo,
                DocumentoAfectadoNumero     = d.DocumentoAfectadoNumero,
                CodigoCondicion             = d.CodigoCondicion,
                Moneda                      = d.Moneda,
                MontoTotalVenta             = d.MontoTotalVenta,
                TotalGravado                = d.TotalGravado,
                TotalExonerado              = d.TotalExonerado,
                TotalInafecto               = d.TotalInafecto,
                TotalGratuito               = d.TotalGratuito,
                TotalIGV                    = d.TotalIGV,
                IGVReferencial              = d.IGVReferencial
            }).ToList() ?? []
        };
    }

    // ── SEND TO SUNAT ─────────────────────────────────────────────────────────
    public async Task<ComprobanteResponse> SendToSunatAsync(int resumenId)
    {
        // 1. Cargar resumen desde BD
        var resumen = await _unitOfWork.ResumenComprobante.GetResumenComprobanteByIdAsync(resumenId)
            ?? throw new KeyNotFoundException($"Resumen {resumenId} no encontrado");

        if (resumen.EstadoSunat == "ACEPTADO")
            throw new InvalidOperationException("El resumen ya fue aceptado por SUNAT");

        // 2. Cargar empresa
        var empresa = await _unitOfWork.Empresas.GetEmpresaByIdAsync(resumen.EmpresaId)
            ?? throw new KeyNotFoundException($"Empresa {resumen.EmpresaId} no encontrada");

        if (string.IsNullOrEmpty(empresa.CertificadoPem))
            throw new InvalidOperationException("La empresa no tiene certificado digital configurado");

        if (string.IsNullOrEmpty(empresa.SolUsuario) || string.IsNullOrEmpty(empresa.SolClave))
            throw new InvalidOperationException("La empresa no tiene credenciales SOL configuradas");

        // 3. Mapear entidad a DTO
        var dto = MapToDto(resumen);

        // 4. Generar XML fresco
        var xmlResultado = _xmlService.GenerarResumenXml(dto);
        if (!xmlResultado.Exitoso)
            throw new InvalidOperationException($"Error generando XML: {xmlResultado.Error}");

        // 5. Firmar XML
        var xmlFirmadoBytes = _xmlSigner.SignXmlToBytes(
            xmlResultado.XmlString!,
            empresa.CertificadoPem!,
            empresa.CertificadoPassword ?? ""
        );

        // 6. Nombre archivo
        var nombreArchivo = $"{empresa.Ruc}-{resumen.Identificador}";

        // 7. Guardar ZIP firmado localmente
        await GuardarArchivosAsync(
            ruc:             empresa.Ruc,
            razonSocial:     resumen.EmpresaRazonSocial!,
            nombreArchivo:   nombreArchivo,
            xmlFirmadoBytes: xmlFirmadoBytes,
            cdrBase64:       null
        );

        // 8. Enviar a SUNAT — incluye envío + consulta ticket internamente
        var sunatResponse = await _sunatResumen.SendResumenAsync(
            xmlFirmadoBytes,
            nombreArchivo,
            empresa.Ruc,
            empresa.SolUsuario!,
            empresa.SolClave!,
            empresa.Environment
        );

        // 9. Si no obtuvo ticket
        if (sunatResponse.CodigoRespuesta == "ERROR_ENVIO")
        {
            await _unitOfWork.ResumenComprobante.UpdateEstadoResumenAsync(
                resumenId, "RECHAZADO", string.Empty,
                sunatResponse.CodigoRespuesta,
                sunatResponse.Descripcion ?? string.Empty,
                string.Empty, DateTime.Now);

            return new ComprobanteResponse
            {
                Exitoso          = false,
                Mensaje          = sunatResponse.Descripcion,
                ComprobanteId    = resumenId,
                EstadoSunat      = "RECHAZADO",
                CodigoRespuesta  = sunatResponse.CodigoRespuesta,
                MensajeRespuesta = sunatResponse.Descripcion
            };
        }

        // 10. Si SUNAT aún procesando tras reintentos
        if (sunatResponse.CodigoRespuesta == "98")
        {
            await _unitOfWork.ResumenComprobante.UpdateEstadoResumenAsync(
                resumenId, "EN_PROCESO",
                sunatResponse.Ticket ?? string.Empty,
                "98", "SUNAT aún procesando",
                string.Empty, DateTime.Now);

            return new ComprobanteResponse
            {
                Exitoso          = false,
                Mensaje          = sunatResponse.Descripcion,
                ComprobanteId    = resumenId,
                EstadoSunat      = "EN_PROCESO",
                CodigoRespuesta  = "98",
                MensajeRespuesta = "Aún en proceso"
            };
        }

        // 11. Guardar CDR si existe
        if (!string.IsNullOrEmpty(sunatResponse.CdrBase64))
        {
            await GuardarArchivosAsync(
                ruc:             empresa.Ruc,
                razonSocial:     resumen.EmpresaRazonSocial!,
                nombreArchivo:   nombreArchivo,
                xmlFirmadoBytes: null,
                cdrBase64:       sunatResponse.CdrBase64
            );
        }

        // 12. Actualizar estado final en BD
        var nuevoEstado = sunatResponse.Success
            ? (sunatResponse.TieneObservaciones ? "ACEPTADO_CON_OBSERVACIONES" : "ACEPTADO")
            : "RECHAZADO";

        await _unitOfWork.ResumenComprobante.UpdateEstadoResumenAsync(
            resumenId, nuevoEstado,
            sunatResponse.Ticket          ?? string.Empty,
            sunatResponse.CodigoRespuesta ?? string.Empty,
            sunatResponse.Descripcion     ?? string.Empty,
            string.Empty, DateTime.Now);

        // 13. Actualizar estado de comprobantes en tabla comprobantes
        if (nuevoEstado == "ACEPTADO" || nuevoEstado == "ACEPTADO_CON_OBSERVACIONES")
        {
            foreach (var detalle in resumen.DetallesResumen.Where(d => d.ComprobanteId > 0))
            {
                var nuevoEstadoComprobante = detalle.CodigoCondicion == "3"
                    ? "ANULADO"
                    : "ACEPTADO";

                await _unitOfWork.Comprobantes.UpdateEstadoSunatAsync(
                    detalle.ComprobanteId,
                    nuevoEstadoComprobante,
                    "0",
                    $"Procesado mediante resumen diario {resumen.Identificador}",
                    null,
                    null
                );
            }
        }

        return new ComprobanteResponse
        {
            Exitoso          = sunatResponse.Success,
            Mensaje          = sunatResponse.Descripcion,
            ComprobanteId    = resumenId,
            EstadoSunat      = nuevoEstado,
            CodigoRespuesta  = sunatResponse.CodigoRespuesta,
            MensajeRespuesta = sunatResponse.Descripcion,
            CdrBase64        = sunatResponse.CdrBase64
        };
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────
    private async Task GuardarArchivosAsync(string ruc, string razonSocial,
        string nombreArchivo, byte[]? xmlFirmadoBytes, string? cdrBase64)
    {
        var empresaCarpeta = LimpiarNombreCarpeta(razonSocial);
        var carpeta        = Path.Combine(_rutaXml, empresaCarpeta, "ResumenComprobantes");
        Directory.CreateDirectory(carpeta);

        if (xmlFirmadoBytes != null)
        {
            using var memStream = new MemoryStream();
            using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zip.CreateEntry($"{nombreArchivo}.xml");
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(xmlFirmadoBytes);
            }
            await File.WriteAllBytesAsync(
                Path.Combine(carpeta, $"{nombreArchivo}.zip"),
                memStream.ToArray());
        }

        if (!string.IsNullOrEmpty(cdrBase64))
        {
            var cdrBytes = Convert.FromBase64String(cdrBase64);
            await File.WriteAllBytesAsync(
                Path.Combine(carpeta, $"R-{nombreArchivo}.zip"),
                cdrBytes);
        }
    }

    private static string LimpiarNombreCarpeta(string nombre) =>
        string.Concat(nombre
            .Replace("/", "").Replace("\\", "").Replace(":", "")
            .Replace("*", "").Replace("?", "").Replace("\"", "")
            .Replace("<", "").Replace(">", "").Replace("|", "")
            .Trim());
}