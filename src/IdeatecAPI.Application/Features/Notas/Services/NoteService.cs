using System.IO.Compression;
using System.Text;
using IdeatecAPI.Application.Common.Interfaces;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Notas.DTOs;
using IdeatecAPI.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace IdeatecAPI.Application.Features.Notas.Services;

public interface INoteService
{
    Task<IEnumerable<NoteDto>> GetAllNotesAsync(int empresaId);
    Task<NoteDto?> GetNoteByIdAsync(int comprobanteId);
    Task<NoteDto> CreateNoteAsync(CreateNoteDto dto);
    Task<NoteDto> UpdateEstadoSunatAsync(int comprobanteId, UpdateNoteEstadoDto dto);
    Task<NoteDto> SendToSunatAsync(int comprobanteId);
    Task DeleteNoteAsync(int comprobanteId);
}

public class NoteService : INoteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IXmlNoteBuilderService _xmlBuilder;
    private readonly IXmlSignerService _xmlSigner;
    private readonly ISunatSenderService _sunatSender;
    private readonly string _rutaXml;
    private readonly IWebSocketNotifier _wsNotifier;

    public NoteService(
        IUnitOfWork unitOfWork,
        IXmlNoteBuilderService xmlBuilder,
        IXmlSignerService xmlSigner,
        ISunatSenderService sunatSender,
        IWebSocketNotifier wsNotifier,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _xmlBuilder = xmlBuilder;
        _xmlSigner = xmlSigner;
        _sunatSender = sunatSender;
        _wsNotifier = wsNotifier;
        _rutaXml = configuration["Storage:RutaXml"] ?? "C:/FacturacionStorage";
    }

    public async Task<IEnumerable<NoteDto>> GetAllNotesAsync(int empresaId)
    {
        var notes = await _unitOfWork.Notes.GetAllNotesAsync(empresaId);
        var result = new List<NoteDto>();

        foreach (var note in notes)
        {
            var dto = MapToDto(note);
            dto.Details = (await _unitOfWork.NoteDetails.GetByComprobanteIdAsync(note.ComprobanteId))
                .Select(MapDetailToDto).ToList();
            dto.Legends = (await _unitOfWork.NoteLegends.GetByComprobanteIdAsync(note.ComprobanteId))
                .Select(MapLegendToDto).ToList();
            result.Add(dto);
        }

        return result;
    }

    public async Task<NoteDto?> GetNoteByIdAsync(int comprobanteId)
    {
        var note = await _unitOfWork.Notes.GetNoteByIdAsync(comprobanteId);
        if (note is null) return null;

        var dto = MapToDto(note);
        dto.Details = (await _unitOfWork.NoteDetails.GetByComprobanteIdAsync(comprobanteId))
            .Select(MapDetailToDto).ToList();
        dto.Legends = (await _unitOfWork.NoteLegends.GetByComprobanteIdAsync(comprobanteId))
            .Select(MapLegendToDto).ToList();

        return dto;
    }

    public async Task<NoteDto> CreateNoteAsync(CreateNoteDto dto)
    {
        // ── 1. Validaciones puras ─────────────────────────────────────────────
        if (dto.TipoDoc != "07" && dto.TipoDoc != "08")
            throw new InvalidOperationException("TipoDoc debe ser '07' o '08'");

        if (!int.TryParse(dto.Correlativo, out var correlativoInt))
            throw new InvalidOperationException("El correlativo debe ser un número entero");

        if (dto.Details == null || dto.Details.Count == 0)
            throw new InvalidOperationException("La nota debe tener al menos un detalle");

        // ── Validación especial para nota débito motivo 01 ────────────────────
        if (dto.TipoDoc == "08" && dto.CodMotivo == "01")
        {
            var comprobanteAfectado = await _unitOfWork.Comprobantes
                .GetByIdAsync(dto.ComprobanteAfectadoId ?? 0)
                ?? throw new KeyNotFoundException("Comprobante afectado no encontrado");

            if (comprobanteAfectado.TipoPago?.ToLower() == "contado")
                throw new InvalidOperationException(
                    "No se puede emitir nota de débito por mora a un comprobante de pago al contado");

            var cuotas = await _unitOfWork.Comprobantes
                .GetCuotasByIdAsync(dto.ComprobanteAfectadoId ?? 0);

            var cuotasVencidasNoPagadas = cuotas.Where(c =>
                c.FechaVencimiento < DateTime.Today &&
                c.Estado?.ToUpper() != "PAGADO");

            if (!cuotasVencidasNoPagadas.Any())
                throw new InvalidOperationException(
                    "El comprobante no tiene cuotas vencidas pendientes de pago");
        }

        // ── 2. Buscar empresa por RUC ─────────────────────────────────────────
        var empresa = await _unitOfWork.Empresas.GetEmpresaByRucAsync(dto.Company.Ruc)
            ?? throw new KeyNotFoundException($"Empresa con RUC {dto.Company.Ruc} no encontrada");

        // ── 3. ClienteId  ────────────
        var cliente = await _unitOfWork.Clientes.GetByNumDocAsync(dto.Client.NumDoc);
        var clienteId = cliente?.ClienteId;

        // ── Pendiente 2: Validar que el comprobante afectado no esté anulado ──
        if (dto.ComprobanteAfectadoId.HasValue && dto.ComprobanteAfectadoId > 0)
        {
            var comprobanteAfectado = await _unitOfWork.Comprobantes
                .GetByIdAsync(dto.ComprobanteAfectadoId.Value)
                ?? throw new KeyNotFoundException(
                    $"Comprobante afectado {dto.NumDocAfectado} no encontrado");

            if (comprobanteAfectado.EstadoSunat == "ANULADO")
                throw new InvalidOperationException(
                    $"No se puede emitir una nota contra el comprobante {dto.NumDocAfectado} porque está ANULADO");
        }

        // ── 4. BeginTransaction ───────────────────────────────────────────────
        _unitOfWork.BeginTransaction();
        try
        {
            if (await _unitOfWork.Notes.ExisteNoteAsync(empresa.Id, dto.TipoDoc, dto.Serie, correlativoInt))
                throw new InvalidOperationException($"Ya existe una nota {dto.Serie}-{dto.Correlativo}");

            var horaLima = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time")
            );
            var note = new Note
            {
                EmpresaId = empresa.Id,
                ClienteId = clienteId,
                TipoDoc = dto.TipoDoc,
                Serie = dto.Serie,
                Correlativo = correlativoInt,
                FechaEmision = dto.FechaEmision.Date.Add(horaLima.TimeOfDay),
                TipoMoneda = dto.TipoMoneda,
                TipoOperacion = dto.TipoOperacion,
                ComprobanteAfectadoId = dto.ComprobanteAfectadoId,
                TipDocAfectado = dto.TipDocAfectado,
                NumDocAfectado = dto.NumDocAfectado,
                TipoNotaCreditoDebito = dto.CodMotivo,
                MotivoNota = dto.DesMotivo,
                ClienteTipoDoc = dto.Client.TipoDoc,
                ClienteNumDoc = dto.Client.NumDoc,
                ClienteRznSocial = dto.Client.RznSocial,
                ClienteDireccion = dto.Client.Address?.Direccion,
                ClienteProvincia = dto.Client.Address?.Provincia,
                ClienteDepartamento = dto.Client.Address?.Departamento,
                ClienteDistrito = dto.Client.Address?.Distrito,
                ClienteUbigeo = dto.Client.Address?.Ubigueo,
                ClienteCorreo = dto.Client?.ClienteCorreo,
                EnviadoPorCorreo = dto.Client?.EnviadoPorCorreo,
                ClienteWhatsApp = dto.Client?.ClienteWhatsApp,
                EnviadoPorWhatsApp = dto.Client?.EnviadoPorWhatsApp,
                EmpresaRuc = dto.Company.Ruc,
                EstablecimientoAnexo = dto.Company.CodEstablecimiento,
                EmpresaRazonSocial = dto.Company.RazonSocial,
                EmpresaNombreComercial = dto.Company.NombreComercial,
                EmpresaDireccion = dto.Company.Address?.Direccion,
                EmpresaProvincia = dto.Company.Address?.Provincia,
                EmpresaDepartamento = dto.Company.Address?.Departamento,
                EmpresaDistrito = dto.Company.Address?.Distrito,
                EmpresaUbigeo = dto.Company.Address?.Ubigueo,
                MtoOperGravadas = dto.MtoOperGravadas,
                MtoOperExoneradas = dto.MtoOperExoneradas,
                MtoIGV = dto.MtoIGV,
                TotalIcbper = dto.TotalIcbper,
                ValorVenta = dto.ValorVenta ?? dto.MtoOperGravadas,
                SubTotal = dto.SubTotal ?? dto.MtoImpVenta,
                MtoImpVenta = dto.MtoImpVenta,
                UsuarioCreacion = dto.UsuarioCreacion,
                EstadoSunat = "PENDIENTE",
                FechaCreacion = DateTime.UtcNow
            };

            var newId = await _unitOfWork.Notes.CreateNoteAsync(note);

            for (int i = 0; i < dto.Details.Count; i++)
            {
                var d = dto.Details[i];
                await _unitOfWork.NoteDetails.CreateDetailAsync(new NoteDetail
                {
                    ComprobanteId = newId,
                    Item = i + 1,
                    ProductoId = d.ProductoId,
                    CodProducto = d.CodProducto,
                    Unidad = d.Unidad,
                    Descripcion = d.Descripcion,
                    Cantidad = d.Cantidad,
                    MtoValorUnitario = d.MtoValorUnitario,
                    MtoValorVenta = d.MtoValorVenta,
                    MtoBaseIgv = d.MtoBaseIgv,
                    PorcentajeIGV = d.PorcentajeIgv,
                    Igv = d.Igv,
                    TipoAfectacionIGV = d.TipAfeIgv.ToString("D2"),
                    MtoPrecioUnitario = d.MtoPrecioUnitario,
                    TotalVentaItem = d.TotalVentaItem,
                    Icbper = d.Icbper,
                    FactorIcbper = d.FactorIcbper
                });
            }

            foreach (var l in dto.Legends)
            {
                await _unitOfWork.NoteLegends.CreateLegendAsync(new NoteLegend
                {
                    ComprobanteId = newId,
                    Code = l.Code,
                    Value = l.Value
                });
            }

            _unitOfWork.Commit();

            return await GetNoteByIdAsync(newId)
                ?? throw new InvalidOperationException("Error al recuperar la nota creada");
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<NoteDto> SendToSunatAsync(int comprobanteId)
    {
        var note = await _unitOfWork.Notes.GetNoteByIdAsync(comprobanteId)
            ?? throw new KeyNotFoundException($"Nota {comprobanteId} no encontrada");

        if (note.EstadoSunat == "ACEPTADO")
            throw new InvalidOperationException("La nota ya fue aceptada por SUNAT");

        // ← Usar EmpresaId guardado en la nota, sin parámetro externo
        var empresa = await _unitOfWork.Empresas.GetEmpresaByIdAsync(note.EmpresaId)
            ?? throw new KeyNotFoundException($"Empresa {note.EmpresaId} no encontrada");

        if (string.IsNullOrEmpty(empresa.CertificadoPem))
            throw new InvalidOperationException("La empresa no tiene certificado digital configurado");

        if (string.IsNullOrEmpty(empresa.SolUsuario) || string.IsNullOrEmpty(empresa.SolClave))
            throw new InvalidOperationException("La empresa no tiene credenciales SOL configuradas");

        var details = (await _unitOfWork.NoteDetails.GetByComprobanteIdAsync(comprobanteId)).ToList();
        var legends = (await _unitOfWork.NoteLegends.GetByComprobanteIdAsync(comprobanteId)).ToList();

        var xmlSinFirmar = _xmlBuilder.BuildXml(note, details, legends);

        var xmlFirmadoBytes = _xmlSigner.SignXmlToBytes(
            xmlSinFirmar,
            empresa.CertificadoPem!,
            empresa.CertificadoPassword ?? "123456"
        );

        var xmlFirmadoString = Encoding.UTF8.GetString(xmlFirmadoBytes);
        var nombreArchivo = $"{empresa.Ruc}-{note.TipoDoc}-{note.Serie}-{note.Correlativo:D8}";

        var sunatResponse = await _sunatSender.SendNoteAsync(
            xmlFirmadoBytes,
            nombreArchivo,
            empresa.Ruc,
            empresa.SolUsuario!,
            empresa.SolClave!,
            empresa.Environment
        );

        // ← Guardar archivos localmente
        await GuardarArchivosAsync(
            empresa.Ruc,
            note.EmpresaRazonSocial ?? empresa.RazonSocial,
            note.TipoDoc,
            note.Serie,
            note.Correlativo.ToString(),
            xmlFirmadoBytes,
            sunatResponse.CdrBase64
        );

        var nuevoEstado = sunatResponse.Success ? (sunatResponse.TieneObservaciones ? "ACEPTADO_CON_OBSERVACIONES" : "ACEPTADO") : "RECHAZADO";

        await _unitOfWork.Notes.UpdateEstadoSunatAsync(
            comprobanteId,
            nuevoEstado,
            sunatResponse.CodigoRespuesta,
            sunatResponse.Descripcion,
            xmlFirmadoString,
            sunatResponse.CdrBase64
        );

        // ── Pendiente 1: Anular comprobante original si es anulación total aceptada ──
        if (sunatResponse.Success && note.ComprobanteAfectadoId.HasValue)
        {
            var esAnulacionTotal = note.TipoDoc == "07" && new[] { "01", "02" }.Contains(note.TipoNotaCreditoDebito);

            if (esAnulacionTotal)
            {
                await _unitOfWork.Comprobantes.UpdateEstadoSunatAsync(
                    note.ComprobanteAfectadoId.Value,
                    "ANULADO",
                    null,
                    $"Anulado por nota de crédito {note.Serie}-{note.Correlativo:D8}",
                    null,
                    null
                );
            }
        }

        var sucursalId = await _unitOfWork.Comprobantes.GetSucursalIdByRucAndAnexoAsync(
            note.EmpresaRuc!,
            note.EstablecimientoAnexo!
        );

        _ = Task.Run(() => _wsNotifier.NotifyWithDelayAsync(sucursalId, note.EmpresaRuc));

        return await GetNoteByIdAsync(comprobanteId)
            ?? throw new InvalidOperationException("Error al recuperar la nota actualizada");
    }

    public async Task<NoteDto> UpdateEstadoSunatAsync(int comprobanteId, UpdateNoteEstadoDto dto)
    {
        var note = await _unitOfWork.Notes.GetNoteByIdAsync(comprobanteId)
            ?? throw new KeyNotFoundException($"Nota {comprobanteId} no encontrada");

        await _unitOfWork.Notes.UpdateEstadoSunatAsync(
            comprobanteId, dto.Estado, dto.Codigo, dto.Mensaje, dto.Xml, dto.Cdr);

        return await GetNoteByIdAsync(comprobanteId)
            ?? throw new InvalidOperationException("Error al recuperar la nota actualizada");
    }

    public async Task DeleteNoteAsync(int comprobanteId)
    {
        var note = await _unitOfWork.Notes.GetNoteByIdAsync(comprobanteId)
            ?? throw new KeyNotFoundException($"Nota {comprobanteId} no encontrada");

        if (note.EstadoSunat == "ACEPTADO")
            throw new InvalidOperationException("No se puede eliminar una nota ya aceptada por SUNAT");

        await _unitOfWork.Notes.UpdateEstadoSunatAsync(
            comprobanteId, "ANULADO", null, "Anulado por el usuario", null, null);
    }

    // ── Mappers ───────────────────────────────────────────────────────────

    private static NoteDto MapToDto(Note n) => new()
    {
        ComprobanteId = n.ComprobanteId,
        TipoDoc = n.TipoDoc,
        Serie = n.Serie,
        Correlativo = n.Correlativo,
        NumeroCompleto = n.NumeroCompleto,
        FechaEmision = n.FechaEmision,
        TipoMoneda = n.TipoMoneda,
        ComprobanteAfectadoId = n.ComprobanteAfectadoId,
        TipDocAfectado = n.TipDocAfectado,
        NumDocAfectado = n.NumDocAfectado,
        CodMotivo = n.TipoNotaCreditoDebito,
        DesMotivo = n.MotivoNota,
        ClienteTipoDoc = n.ClienteTipoDoc,
        ClienteNumDoc = n.ClienteNumDoc,
        ClienteRznSocial = n.ClienteRznSocial,
        ClienteCorreo = n.ClienteCorreo,
        EnviadoPorCorreo = n.EnviadoPorCorreo,
        ClienteWhatsApp = n.ClienteWhatsApp,
        EnviadoPorWhatsApp = n.EnviadoPorWhatsApp,
        MtoOperGravadas = n.MtoOperGravadas,
        MtoIGV = n.MtoIGV,
        MtoImpVenta = n.MtoImpVenta,
        EstadoSunat = n.EstadoSunat,
        CodigoRespuestaSunat = n.CodigoRespuestaSunat,
        MensajeRespuestaSunat = n.MensajeRespuestaSunat,
        FechaEnvioSunat = n.FechaEnvioSunat,
        UsuarioCreacion = n.UsuarioCreacion,
        PdfGenerado = n.PdfGenerado,
        EnviadoEnResumen = n.EnviadoEnResumen,
        FechaCreacion = n.FechaCreacion
    };

    private static NoteDetailDto MapDetailToDto(NoteDetail d) => new()
    {
        DetalleId = d.DetalleId,
        CodProducto = d.CodProducto,
        Unidad = d.Unidad,
        Descripcion = d.Descripcion,
        Cantidad = d.Cantidad,
        MtoValorUnitario = d.MtoValorUnitario,
        Icbper = d.Icbper,
        FactorIcbper = d.FactorIcbper,
        MtoValorVenta = d.MtoValorVenta,
        MtoBaseIgv = d.MtoBaseIgv,
        PorcentajeIgv = d.PorcentajeIGV,
        Igv = d.Igv,
        TipoAfectacionIGV = d.TipoAfectacionIGV,
        MtoPrecioUnitario = d.MtoPrecioUnitario,
        TotalVentaItem = d.TotalVentaItem
    };

    private static NoteLegendDto MapLegendToDto(NoteLegend l) => new()
    {
        Code = l.Code,
        Value = l.Value
    };

    private async Task GuardarArchivosAsync(
        string ruc, string razonSocial, string tipoComprobante,
        string serie, string correlativo,
        byte[] xmlFirmadoBytes, string? cdrBase64)
    {
        var empresaCarpeta = string.Concat(razonSocial
            .Replace("/", "").Replace("\\", "").Replace(":", "")
            .Replace("*", "").Replace("?", "").Replace("\"", "")
            .Replace("<", "").Replace(">", "").Replace("|", "").Trim());

        var tipoCarpeta = tipoComprobante switch
        {
            "07" => "NotasCredito",
            "08" => "NotasDebito",
            _ => tipoComprobante
        };

        var nombreArchivo = $"{ruc}-{tipoComprobante}-{serie}-{correlativo.PadLeft(8, '0')}";
        var carpeta = Path.Combine(_rutaXml, empresaCarpeta, tipoCarpeta);
        Directory.CreateDirectory(carpeta);

        // ── Guardar ZIP enviado ───────────────────────────────────────────────
        var rutaZip = Path.Combine(carpeta, $"{nombreArchivo}.zip");
        using (var zipStream = new FileStream(rutaZip, FileMode.Create))
        using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create))
        {
            var entry = zipArchive.CreateEntry($"{nombreArchivo}.xml");
            using var entryStream = entry.Open();
            await entryStream.WriteAsync(xmlFirmadoBytes);
        }

        // ── Guardar CDR recibido ──────────────────────────────────────────────
        if (!string.IsNullOrEmpty(cdrBase64))
        {
            var cdrBytes = Convert.FromBase64String(cdrBase64);
            var rutaCdr = Path.Combine(carpeta, $"R-{nombreArchivo}.zip");
            await File.WriteAllBytesAsync(rutaCdr, cdrBytes);
        }
    }
}