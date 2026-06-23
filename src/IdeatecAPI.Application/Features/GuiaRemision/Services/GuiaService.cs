using System.IO.Compression;
using IdeatecAPI.Application.Common.Interfaces;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.GuiaRemision.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using GuiaEntity = IdeatecAPI.Domain.Entities.GuiaRemision;
using GuiaDetalleEntity = IdeatecAPI.Domain.Entities.GuiaRemisionDetalle;
using IdeatecAPI.Application.Features.Notas.Services;

namespace IdeatecAPI.Application.Features.GuiaRemision.Services;

public interface IGuiaService
{
    Task<IEnumerable<GuiaDto>> GetAllAsync(int empresaId);
    Task<IEnumerable<GuiaListadoDto>> GetAllByRucAsync(string empresaRuc, string tipoDoc, int? sucursalId);
    Task<IEnumerable<GuiaListadoDto>> GetAllByRucFechasAsync(string empresaRuc, string tipoDoc, int? sucursalId, DateOnly? fechaDesde, DateOnly? fechaHasta);
    Task<GuiaDto?> GetByIdAsync(int guiaId);
    Task<GuiaDto?> GetBySerieCorrelativoAsync(string ruc, string serie, int correlativo);
    Task<GuiaDto> CreateAsync(CreateGuiaDto dto);
    Task<GuiaDto> SendToSunatAsync(int guiaId);
    Task DeleteAsync(int guiaId);
}

public class GuiaService : IGuiaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IXmlGuiaBuilderService _xmlBuilder;
    private readonly IXmlSignerService _xmlSigner;
    private readonly ISunatGuiaService _sunatGuia;
    private readonly IStorageService _storageService;
    private readonly IWebSocketNotifier _wsNotifier;
    private readonly ILogger<GuiaService> _logger;

    private static readonly TimeZoneInfo TzLima =
        TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

    public GuiaService(
        IUnitOfWork unitOfWork,
        IXmlGuiaBuilderService xmlBuilder,
        IXmlSignerService xmlSigner,
        ISunatGuiaService sunatGuia,
        IWebSocketNotifier wsNotifier,
        IStorageService storageService,
        ILogger<GuiaService> logger,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _xmlBuilder = xmlBuilder;
        _xmlSigner = xmlSigner;
        _sunatGuia = sunatGuia;
        _storageService = storageService;
        _wsNotifier = wsNotifier;
        _logger = logger;
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    public async Task<IEnumerable<GuiaDto>> GetAllAsync(int empresaId)
    {
        var guias = await _unitOfWork.Guias.GetAllAsync(empresaId);
        return await MapListAsync(guias);
    }

    public async Task<IEnumerable<GuiaListadoDto>> GetAllByRucAsync(
        string empresaRuc, string tipoDoc, int? sucursalId)
    {
        var guias = await _unitOfWork.Guias.GetAllByRucAsync(empresaRuc, tipoDoc, sucursalId);
        return guias.Select(MapToListadoDto);
    }

    public async Task<IEnumerable<GuiaListadoDto>> GetAllByRucFechasAsync(
        string empresaRuc, string tipoDoc, int? sucursalId,
        DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        var guias = await _unitOfWork.Guias.GetAllByRucFechasAsync(
            empresaRuc, tipoDoc, sucursalId, fechaDesde, fechaHasta);
        return guias.Select(MapToListadoDto);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    public async Task<GuiaDto?> GetByIdAsync(int guiaId)
    {
        var guia = await _unitOfWork.Guias.GetByIdAsync(guiaId);
        if (guia is null) return null;

        var dto = MapToDto(guia);
        dto.Details = (await _unitOfWork.GuiaDetalles.GetByGuiaIdAsync(guiaId))
            .Select(MapDetalleToDto).ToList();

        return dto;
    }

    public async Task<GuiaDto?> GetBySerieCorrelativoAsync(string ruc, string serie, int correlativo)
    {
        var guia = await _unitOfWork.Guias.GetBySerieCorrelativoAsync(ruc, serie, correlativo);
        if (guia is null) return null;

        var dto = MapToDto(guia);
        dto.Details = (await _unitOfWork.GuiaDetalles.GetByGuiaIdAsync(guia.GuiaId))
            .Select(MapDetalleToDto).ToList();

        return dto;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    public async Task<GuiaDto> CreateAsync(CreateGuiaDto dto)
    {
        if (dto.Details == null || dto.Details.Count == 0)
            throw new InvalidOperationException("La guía debe tener al menos un detalle");

        if (string.IsNullOrEmpty(dto.Envio.CodTraslado))
            throw new InvalidOperationException("El código de traslado es requerido");

        if (string.IsNullOrEmpty(dto.Envio.ModTraslado))
            throw new InvalidOperationException("El modo de traslado es requerido");

        if (!int.TryParse(dto.Correlativo, out var correlativoInt))
            throw new InvalidOperationException("El correlativo debe ser un número entero");

        var empresa = await _unitOfWork.Empresas.GetEmpresaByRucAsync(dto.Company.Ruc)
            ?? throw new KeyNotFoundException($"Empresa con RUC {dto.Company.Ruc} no encontrada");

        if (await _unitOfWork.Guias.ExisteAsync(empresa.Id, dto.TipoDoc, dto.Serie, correlativoInt))
            throw new InvalidOperationException($"Ya existe la guía {dto.Serie}-{dto.Correlativo}");

        _unitOfWork.BeginTransaction();
        try
        {
            var guia = new GuiaEntity
            {
                EmpresaId = empresa.Id,
                SucursalId = dto.SucursalId,
                Version = dto.Version,
                TipoDoc = dto.TipoDoc,
                Serie = dto.Serie,
                Correlativo = correlativoInt,
                FechaEmision = dto.FechaEmision,
                EmpresaRuc = dto.Company.Ruc,
                EmpresaRazonSocial = dto.Company.RazonSocial,
                EmpresaNombreComercial = dto.Company.NombreComercial,
                EmpresaDireccion = dto.Company.Address?.Direccion,
                EmpresaProvincia = dto.Company.Address?.Provincia,
                EmpresaDepartamento = dto.Company.Address?.Departamento,
                EmpresaDistrito = dto.Company.Address?.Distrito,
                EmpresaUbigeo = dto.Company.Address?.Ubigueo,
                DestinatarioTipoDoc = dto.Destinatario.TipoDoc,
                DestinatarioNumDoc = dto.Destinatario.NumDoc,
                DestinatarioRznSocial = dto.Destinatario.RznSocial,
                TerceroTipoDoc = dto.Tercero?.TipoDoc,
                TerceroNumDoc = dto.Tercero?.NumDoc,
                TerceroRznSocial = dto.Tercero?.RznSocial,
                Observacion = dto.Observacion,
                DocBajaTipoDoc = dto.DocBaja?.TipoDoc,
                DocBajaNroDoc = dto.DocBaja?.NroDoc,
                RelDocTipoDoc = dto.RelDoc?.TipoDoc,
                RelDocNroDoc = dto.RelDoc?.NroDoc,
                CodTraslado = dto.Envio.CodTraslado,
                DesTraslado = dto.Envio.DesTraslado,
                ModTraslado = dto.Envio.ModTraslado,
                FecTraslado = dto.Envio.FecTraslado,
                CodPuerto = dto.Envio.CodPuerto,
                IndTransbordo = dto.Envio.IndTransbordo,
                MatPeligrosoClase = dto.Envio.MatPeligrosoClase,
                MatPeligrosoNroONU = dto.Envio.MatPeligrosoNroONU,
                PesoTotal = dto.Envio.PesoTotal,
                UndPesoTotal = dto.Envio.UndPesoTotal,
                NumContenedor = dto.Envio.NumContenedor,
                LlegadaUbigeo = dto.Envio.Llegada.Ubigueo,
                LlegadaDireccion = dto.Envio.Llegada.Direccion,
                LlegadaDepartamento = dto.Envio.Llegada.Departamento,
                LlegadaProvincia = dto.Envio.Llegada.Provincia,
                LlegadaDistrito = dto.Envio.Llegada.Distrito,
                PartidaUbigeo = dto.Envio.Partida.Ubigueo,
                PartidaDireccion = dto.Envio.Partida.Direccion,
                PartidaDepartamento = dto.Envio.Partida.Departamento,
                PartidaProvincia = dto.Envio.Partida.Provincia,
                PartidaDistrito = dto.Envio.Partida.Distrito,
                TransportistaTipoDoc = dto.Envio.Transportista?.TipoDoc,
                TransportistaNumDoc = dto.Envio.Transportista?.NumDoc,
                TransportistaRegistroMTC = dto.Envio.Transportista?.RegistroMTC,
                TransportistaRznSocial = dto.Envio.Transportista?.RznSocial,
                IndVehiculoM1L = dto.Envio.Transportista?.IndVehiculoM1L ?? false,
                TransportistaPlaca = dto.Envio.Transportista?.Placa,
                AutorizacionVehiculoEntidad = dto.Envio.Transportista?.AutorizacionVehiculoEntidad,
                AutorizacionVehiculoNumero = dto.Envio.Transportista?.AutorizacionVehiculoNumero,
                PlacaSecundaria1 = dto.Envio.Transportista?.PlacaSecundaria1,
                PlacaSecundaria2 = dto.Envio.Transportista?.PlacaSecundaria2,
                PlacaSecundaria3 = dto.Envio.Transportista?.PlacaSecundaria3,
                ChoferSecundarioTipoDoc = dto.Envio.Transportista?.ChoferSecundarioTipoDoc,
                ChoferSecundarioDoc = dto.Envio.Transportista?.ChoferSecundarioDoc,
                ChoferSecundarioNombres = dto.Envio.Transportista?.ChoferSecundarioNombres,
                ChoferSecundarioApellidos = dto.Envio.Transportista?.ChoferSecundarioApellidos,
                ChoferSecundarioLicencia = dto.Envio.Transportista?.ChoferSecundarioLicencia,
                ChoferSecundario2TipoDoc = dto.Envio.Transportista?.ChoferSecundario2TipoDoc,
                ChoferSecundario2Doc = dto.Envio.Transportista?.ChoferSecundario2Doc,
                ChoferSecundario2Nombres = dto.Envio.Transportista?.ChoferSecundario2Nombres,
                ChoferSecundario2Apellidos = dto.Envio.Transportista?.ChoferSecundario2Apellidos,
                ChoferSecundario2Licencia = dto.Envio.Transportista?.ChoferSecundario2Licencia,
                ChoferTipoDoc = dto.Envio.Transportista?.ChoferTipoDoc,
                ChoferDoc = dto.Envio.Transportista?.ChoferDoc,
                ChoferNombres = dto.Envio.Transportista?.ChoferNombres,
                ChoferApellidos = dto.Envio.Transportista?.ChoferApellidos,
                ChoferLicencia = dto.Envio.Transportista?.ChoferLicencia,
                EstadoSunat = "PENDIENTE",
                FechaCreacion = AhoraLima(),
                ClienteCorreo = dto.ClienteCorreo,
                ClienteWhatsapp = dto.ClienteWhatsapp,
                UsuarioCreacion = dto.UsuarioCreacion,
                EnviadoPorCorreo = dto.EnviadoPorCorreo,
                EnviadoPorWhatsapp = dto.EnviadoPorWhatsapp
            };

            var newId = await _unitOfWork.Guias.CreateAsync(guia);

            // ── Bulk insert: un solo round-trip para todos los detalles ───────
            await _unitOfWork.GuiaDetalles.CreateBulkAsync(
                dto.Details.Select(d => new GuiaDetalleEntity
                {
                    GuiaId = newId,
                    Cantidad = d.Cantidad,
                    Unidad = d.Unidad,
                    Descripcion = d.Descripcion,
                    Codigo = d.Codigo
                })
            );

            _unitOfWork.Commit();

            return await GetByIdAsync(newId)
                ?? throw new InvalidOperationException("Error al recuperar la guía creada");
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // ── SendToSunat ───────────────────────────────────────────────────────────

    public async Task<GuiaDto> SendToSunatAsync(int guiaId)
    {
        // ── 1. Validar guía ───────────────────────────────────────────────
        var guia = await _unitOfWork.Guias.GetByIdAsync(guiaId)
            ?? throw new KeyNotFoundException($"Guía {guiaId} no encontrada");

        if (guia.EstadoSunat == "ACEPTADO")
            throw new InvalidOperationException("La guía ya fue aceptada por SUNAT");

        // Secuencial — sin cambiar nada más del método
        var empresa = await _unitOfWork.Empresas.GetEmpresaByIdAsync(guia.EmpresaId)
            ?? throw new KeyNotFoundException($"Empresa {guia.EmpresaId} no encontrada");

        var details = (await _unitOfWork.GuiaDetalles.GetByGuiaIdAsync(guiaId)).ToList();

        if (string.IsNullOrEmpty(empresa.CertificadoPem))
            throw new InvalidOperationException("La empresa no tiene certificado digital configurado");

        if (string.IsNullOrEmpty(empresa.SolUsuario) || string.IsNullOrEmpty(empresa.SolClave))
            throw new InvalidOperationException("La empresa no tiene credenciales SOL configuradas");

        if (string.IsNullOrEmpty(empresa.ClientId) || string.IsNullOrEmpty(empresa.ClientSecret))
            throw new InvalidOperationException("La empresa no tiene client_id y client_secret configurados para GRE");

        // ── 3. Generar XML y firmar ───────────────────────────────────────
        var xmlSinFirmar = guia.TipoDoc == "31"
            ? _xmlBuilder.BuildXmlTransportista(guia, details)
            : _xmlBuilder.BuildXml(guia, details);

        var xmlFirmadoBytes = _xmlSigner.SignXmlToBytes(
            xmlSinFirmar,
            empresa.CertificadoPem!,
            empresa.CertificadoPassword ?? "123456"
        );

        var nombreArchivo = $"{empresa.Ruc}-{guia.TipoDoc}-{guia.Serie}-{guia.Correlativo:D8}";

        // ── 4. Comprimir ZIP una sola vez ─────────────────────────────────
        byte[] zipBytes;
        using (var memStream = new MemoryStream())
        {
            using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zip.CreateEntry($"{nombreArchivo}.xml");
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(xmlFirmadoBytes);
            }
            zipBytes = memStream.ToArray();
        }

        // ── 5. Subir ZIP a storage y enviar a SUNAT en paralelo ───────────
        var subirZipTask = SubirZipSeguroAsync(empresa.Ruc, nombreArchivo, zipBytes, empresa.Environment);
        var sunatTask = _sunatGuia.SendGuiaAsync(
            xmlFirmadoBytes, nombreArchivo,
            empresa.Ruc, empresa.SolUsuario!, empresa.SolClave!,
            empresa.ClientId!, empresa.ClientSecret!,
            empresa.Environment
        );

        await Task.WhenAll(subirZipTask, sunatTask);

        var sunatResponse = await sunatTask;
        var rutaXml = await subirZipTask; // null si el storage falló

        // ── 6. Determinar nuevo estado ────────────────────────────────────
        string nuevoEstado = sunatResponse.CodigoRespuesta == "EN_PROCESO"
            ? "EN_PROCESO"
            : sunatResponse.Success ? "ACEPTADO" : "RECHAZADO";

        string? rutaCdr = sunatResponse.CdrBase64 is not null
            ? $"/{empresa.Ruc}/guias-remision/R-{nombreArchivo}.zip"
            : null;

        // ── 7. Un solo UPDATE con todo ────────────────────────────────────
        await _unitOfWork.Guias.UpdateEnvioSunatAsync(
            guiaId,
            estado: nuevoEstado,
            codigo: sunatResponse.CodigoRespuesta,
            mensaje: sunatResponse.Descripcion,
            ticket: sunatResponse.Ticket,
            fechaEnvio: sunatResponse.Success ? AhoraLima() : null,
            rutaXml: rutaXml,
            rutaCdr: rutaCdr
        );

        // ── 8. CDR + WebSocket en segundo plano (no bloquean la respuesta) ─
        _ = Task.WhenAll(
            SubirCdrSeguroAsync(empresa.Ruc, nombreArchivo, sunatResponse.CdrBase64, empresa.Environment),
            _wsNotifier.NotifyAsync(guia.SucursalId, guia.EmpresaRuc, "status")
        );

        return await GetByIdAsync(guiaId)
            ?? throw new InvalidOperationException("Error al recuperar la guía actualizada");
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    public async Task DeleteAsync(int guiaId)
    {
        var guia = await _unitOfWork.Guias.GetByIdAsync(guiaId)
            ?? throw new KeyNotFoundException($"Guía {guiaId} no encontrada");

        if (guia.EstadoSunat == "ACEPTADO")
            throw new InvalidOperationException("No se puede eliminar una guía ya aceptada por SUNAT");

        await _unitOfWork.Guias.UpdateEstadoAsync(
            guiaId, "ANULADO", null, "Anulado por el usuario", null, null, null);
    }

    // ── Helpers de storage (aíslan el try/catch del flujo principal) ──────────

    private async Task<string?> SubirZipSeguroAsync(string ruc, string nombreArchivo, byte[] zipBytes, string entorno)
    {
        try
        {
            var ruta = await _storageService.SubirZipAsync(ruc, "09", nombreArchivo, zipBytes, entorno);
            _logger.LogInformation("[STORAGE] ZIP subido correctamente: {Ruta}", ruta);
            return ruta;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[STORAGE] Error subiendo ZIP de guía {Archivo}", nombreArchivo);
            return null;
        }
    }

    private async Task SubirCdrSeguroAsync(string ruc, string nombreArchivo, string? cdrBase64, string entorno)
    {
        if (string.IsNullOrEmpty(cdrBase64)) return;
        try
        {
            await _storageService.SubirCdrAsync(ruc, "09", nombreArchivo, cdrBase64, entorno);
            _logger.LogInformation("[STORAGE] CDR subido correctamente: {Archivo}", nombreArchivo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[STORAGE] Error subiendo CDR de guía {Archivo}", nombreArchivo);
        }
    }

    // ── Helpers generales ─────────────────────────────────────────────────────

    private static DateTime AhoraLima() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TzLima);

    private async Task<IEnumerable<GuiaDto>> MapListAsync(IEnumerable<GuiaEntity> guias)
    {
        var result = new List<GuiaDto>();
        foreach (var guia in guias)
        {
            var dto = MapToDto(guia);
            dto.Details = (await _unitOfWork.GuiaDetalles.GetByGuiaIdAsync(guia.GuiaId))
                .Select(MapDetalleToDto).ToList();
            result.Add(dto);
        }
        return result;
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static GuiaListadoDto MapToListadoDto(GuiaEntity g) => new()
    {
        GuiaId = g.GuiaId,
        SucursalId = g.SucursalId,
        TipoDoc = g.TipoDoc,
        NumeroCompleto = g.NumeroCompleto,
        FechaEmision = g.FechaEmision,
        FechaCreacion = g.FechaCreacion,
        DestinatarioNumDoc = g.DestinatarioNumDoc,
        DestinatarioRznSocial = g.DestinatarioRznSocial,
        PartidaDireccion = g.PartidaDireccion,
        LlegadaDireccion = g.LlegadaDireccion,
        TransportistaRznSocial = g.TransportistaRznSocial,
        TransportistaPlaca = g.TransportistaPlaca,
        ClienteCorreo = g.ClienteCorreo,
        EnviadoPorCorreo = g.EnviadoPorCorreo,
        ClienteWhatsapp = g.ClienteWhatsapp,
        EnviadoPorWhatsapp = g.EnviadoPorWhatsapp,
        EstadoSunat = g.EstadoSunat,
        CodigoRespuestaSunat = g.CodigoRespuestaSunat,
        MensajeRespuestaSunat = g.MensajeRespuestaSunat,
        XmlGenerado = g.XmlGenerado,
        XmlRespuestaSunat = g.XmlRespuestaSunat
    };

    private static GuiaDto MapToDto(GuiaEntity g) => new()
    {
        SucursalId = g.SucursalId,
        GuiaId = g.GuiaId,
        Version = g.Version,
        TipoDoc = g.TipoDoc,
        Serie = g.Serie,
        Correlativo = g.Correlativo,
        NumeroCompleto = g.NumeroCompleto,
        FechaEmision = g.FechaEmision,
        EmpresaRuc = g.EmpresaRuc,
        EmpresaRazonSocial = g.EmpresaRazonSocial,
        DestinatarioTipoDoc = g.DestinatarioTipoDoc,
        DestinatarioNumDoc = g.DestinatarioNumDoc,
        DestinatarioRznSocial = g.DestinatarioRznSocial,
        Observacion = g.Observacion,
        RelDocTipoDoc = g.RelDocTipoDoc,
        RelDocNroDoc = g.RelDocNroDoc,
        TerceroTipoDoc = g.TerceroTipoDoc,
        TerceroNumDoc = g.TerceroNumDoc,
        TerceroRznSocial = g.TerceroRznSocial,
        CodTraslado = g.CodTraslado,
        DesTraslado = g.DesTraslado,
        ModTraslado = g.ModTraslado,
        FecTraslado = g.FecTraslado,
        PesoTotal = g.PesoTotal,
        UndPesoTotal = g.UndPesoTotal,
        IndTransbordo = g.IndTransbordo,
        MatPeligrosoClase = g.MatPeligrosoClase,
        MatPeligrosoNroONU = g.MatPeligrosoNroONU,
        LlegadaUbigeo = g.LlegadaUbigeo,
        LlegadaDireccion = g.LlegadaDireccion,
        LlegadaDepartamento = g.LlegadaDepartamento,
        LlegadaProvincia = g.LlegadaProvincia,
        LlegadaDistrito = g.LlegadaDistrito,
        PartidaUbigeo = g.PartidaUbigeo,
        PartidaDireccion = g.PartidaDireccion,
        PartidaDepartamento = g.PartidaDepartamento,
        PartidaProvincia = g.PartidaProvincia,
        PartidaDistrito = g.PartidaDistrito,
        TransportistaNumDoc = g.TransportistaNumDoc,
        TransportistaRznSocial = g.TransportistaRznSocial,
        TransportistaRegistroMTC = g.TransportistaRegistroMTC,
        IndVehiculoM1L = g.IndVehiculoM1L,
        TransportistaPlaca = g.TransportistaPlaca,
        PlacaSecundaria1 = g.PlacaSecundaria1,
        PlacaSecundaria2 = g.PlacaSecundaria2,
        PlacaSecundaria3 = g.PlacaSecundaria3,
        ChoferTipoDoc = g.ChoferTipoDoc,
        ChoferDoc = g.ChoferDoc,
        ChoferNombres = g.ChoferNombres,
        ChoferApellidos = g.ChoferApellidos,
        ChoferLicencia = g.ChoferLicencia,
        ChoferSecundarioTipoDoc = g.ChoferSecundarioTipoDoc,
        ChoferSecundarioDoc = g.ChoferSecundarioDoc,
        ChoferSecundarioNombres = g.ChoferSecundarioNombres,
        ChoferSecundarioApellidos = g.ChoferSecundarioApellidos,
        ChoferSecundarioLicencia = g.ChoferSecundarioLicencia,
        ChoferSecundario2TipoDoc = g.ChoferSecundario2TipoDoc,
        ChoferSecundario2Doc = g.ChoferSecundario2Doc,
        ChoferSecundario2Nombres = g.ChoferSecundario2Nombres,
        ChoferSecundario2Apellidos = g.ChoferSecundario2Apellidos,
        ChoferSecundario2Licencia = g.ChoferSecundario2Licencia,
        AutorizacionVehiculoEntidad = g.AutorizacionVehiculoEntidad,
        AutorizacionVehiculoNumero = g.AutorizacionVehiculoNumero,
        EstadoSunat = g.EstadoSunat,
        CodigoRespuestaSunat = g.CodigoRespuestaSunat,
        MensajeRespuestaSunat = g.MensajeRespuestaSunat,
        TicketSunat = g.TicketSunat,
        FechaEnvioSunat = g.FechaEnvioSunat,
        FechaCreacion = g.FechaCreacion,
        ClienteCorreo = g.ClienteCorreo,
        EnviadoPorCorreo = g.EnviadoPorCorreo,
        ClienteWhatsapp = g.ClienteWhatsapp,
        EnviadoPorWhatsapp = g.EnviadoPorWhatsapp,
        UsuarioCreacion = g.UsuarioCreacion
    };

    private static GuiaDetalleDto MapDetalleToDto(GuiaDetalleEntity d) => new()
    {
        DetalleId = d.DetalleId,
        Cantidad = d.Cantidad,
        Unidad = d.Unidad,
        Descripcion = d.Descripcion,
        Codigo = d.Codigo
    };
}