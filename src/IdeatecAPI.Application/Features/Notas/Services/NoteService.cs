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
    private readonly IStorageService _storageService;
    private readonly IWebSocketNotifier _wsNotifier;

    public NoteService(
        IUnitOfWork unitOfWork,
        IXmlNoteBuilderService xmlBuilder,
        IXmlSignerService xmlSigner,
        ISunatSenderService sunatSender,
        IWebSocketNotifier wsNotifier,
        IStorageService storageService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _xmlBuilder = xmlBuilder;
        _xmlSigner = xmlSigner;
        _sunatSender = sunatSender;
        _wsNotifier = wsNotifier;
        _storageService = storageService;
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

            // ── NUEVO: Firmar XML al crear para tener el Hash inmediatamente ──
            if (!string.IsNullOrEmpty(empresa.CertificadoPem))
            {
                try
                {
                    var xmlBase = _xmlBuilder.BuildXml(note, dto.Details.Select((d, i) => new NoteDetail
                    {
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
                    }).ToList(), dto.Legends.Select(l => new NoteLegend { Code = l.Code, Value = l.Value }).ToList());

                    var firmaRes = _xmlSigner.SignXmlFull(
                        xmlBase,
                        empresa.CertificadoPem,
                        empresa.CertificadoPassword ?? ""
                    );
                    note.CodigoHashCPE = firmaRes.DigestValue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AVISO] No se pudo firmar nota en creación: {ex.Message}");
                }
            }

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

        var empresa = await _unitOfWork.Empresas.GetEmpresaByIdAsync(note.EmpresaId)
            ?? throw new KeyNotFoundException($"Empresa {note.EmpresaId} no encontrada");

        if (string.IsNullOrEmpty(empresa.CertificadoPem))
            throw new InvalidOperationException("La empresa no tiene certificado digital configurado");

        if (string.IsNullOrEmpty(empresa.SolUsuario) || string.IsNullOrEmpty(empresa.SolClave))
            throw new InvalidOperationException("La empresa no tiene credenciales SOL configuradas");

        var details = (await _unitOfWork.NoteDetails.GetByComprobanteIdAsync(comprobanteId)).ToList();
        var legends = (await _unitOfWork.NoteLegends.GetByComprobanteIdAsync(comprobanteId)).ToList();

        // 1. Generar y firmar XML
        var xmlSinFirmar = _xmlBuilder.BuildXml(note, details, legends);
        var firmaResultado = _xmlSigner.SignXmlFull(
            xmlSinFirmar,
            empresa.CertificadoPem!,
            empresa.CertificadoPassword ?? "123456"
        );
        var xmlFirmadoBytes = firmaResultado.SignedXmlBytes;

        var nombreArchivo = $"{empresa.Ruc}-{note.TipoDoc}-{note.Serie}-{note.Correlativo:D8}";

        // 2. Subir ZIP al microservicio (hilo principal)
        using var memStream = new MemoryStream();
        using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = zip.CreateEntry($"{nombreArchivo}.xml");
            using var entryStream = entry.Open();
            await entryStream.WriteAsync(xmlFirmadoBytes);
        }
        try
        {
            var rutaXml = await _storageService.SubirZipAsync(
                empresa.Ruc,
                note.TipoDoc!,
                nombreArchivo,
                memStream.ToArray()
            );
            await _unitOfWork.Notes.UpdateXmlGeneradoAsync(comprobanteId, rutaXml);
            Console.WriteLine($"[STORAGE ✅] xmlGenerado nota guardado: {rutaXml}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[STORAGE ❌] Error subiendo ZIP nota: {ex.Message}");
        }

        // 3. Enviar a SUNAT
        SunatResponse sunatResponse;
        try
        {
            sunatResponse = await _sunatSender.SendNoteAsync(
                xmlFirmadoBytes,
                nombreArchivo,
                empresa.Ruc,
                empresa.SolUsuario!,
                empresa.SolClave!,
                empresa.Environment
            );
        }
        catch (HttpRequestException ex)
        {
            // SUNAT no responde / caída de red — no es un rechazo real, queda PENDIENTE para reintento
            await _unitOfWork.Notes.UpdateEstadoSunatAsync(
                comprobanteId,
                "PENDIENTE",
                null,
                $"Error de conexión con SUNAT: {ex.Message}",
                null,
                null,
                firmaResultado.DigestValue
            );

            var sucursalIdError = await _unitOfWork.Comprobantes.GetSucursalIdByRucAndAnexoAsync(
                note.EmpresaRuc!,
                note.EstablecimientoAnexo!
            );
            _ = Task.Run(() => _wsNotifier.NotifyAsync(sucursalIdError, note.EmpresaRuc, "status"));

            return await GetNoteByIdAsync(comprobanteId)
                ?? throw new InvalidOperationException("Error al recuperar la nota actualizada");
        }

        // 4. Subir CDR en segundo plano
        if (!string.IsNullOrEmpty(sunatResponse.CdrBase64))
        {
            var cdrBase64Capture = sunatResponse.CdrBase64;
            _ = Task.Run(async () =>
            {
                try
                {
                    var rutaCdr = await _storageService.SubirCdrAsync(
                        empresa.Ruc,
                        note.TipoDoc!,
                        nombreArchivo,
                        cdrBase64Capture
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[STORAGE ❌] Error subiendo CDR nota: {ex.Message}");
                }
            });
        }

        // 5. Actualizar estado en BD
        string nuevoEstado;
        if (sunatResponse.Success)
        {
            nuevoEstado = sunatResponse.TieneObservaciones ? "ACEPTADO_CON_OBSERVACIONES" : "ACEPTADO";
        }
        else if (sunatResponse.CodigoRespuesta == "SUNAT_ERROR_HTML" || sunatResponse.CodigoRespuesta == "ERROR_RED")
        {
            // Caída de servidor o red de SUNAT — no es un rechazo de validación, queda PENDIENTE para reintento
            nuevoEstado = "PENDIENTE";
        }
        else
        {
            // Error de validación real de SUNAT
            nuevoEstado = "RECHAZADO";
        }

        await _unitOfWork.Notes.UpdateEstadoSunatAsync(
            comprobanteId,
            nuevoEstado,
            sunatResponse.CodigoRespuesta,
            sunatResponse.Descripcion,
            null,
            null,
            firmaResultado.DigestValue
        );

        // 6. Guardar ruta CDR en BD (hilo principal)
        if (!string.IsNullOrEmpty(sunatResponse.CdrBase64))
        {
            var rutaCdr = $"/{empresa.Ruc}/{ObtenerTipoCarpeta(note.TipoDoc!)}/R-{nombreArchivo}.zip";
            await _unitOfWork.Notes.UpdateXmlRespuestaSunatAsync(comprobanteId, rutaCdr);
        }

        // 7. Anular comprobante original si aplica
        if (sunatResponse.Success && note.ComprobanteAfectadoId.HasValue)
        {
            var esNC = note.TipoDoc == "07";
            var esND = note.TipoDoc == "08";
            var motivo = note.TipoNotaCreditoDebito;
            var referencia = $"{note.Serie}-{note.Correlativo:D8}";

            var motivosAnulacion = new[] { "01", "02" };
            var motivosDevolucion = new[] { "06" };
            var motivosDescuento = new[] { "04", "05", "08", "09" };

            string? mensajeAfectado = null;
            string? estadoAfectado = null;

            if (esNC)
            {
                if (motivosAnulacion.Contains(motivo))
                {
                    estadoAfectado   = "ANULADO";
                    mensajeAfectado  = $"Anulado por NC {referencia}";
                }
                else if (motivosDevolucion.Contains(motivo))
                {
                    estadoAfectado   = "DEVOLUCION_TOTAL";
                    mensajeAfectado  = $"Devolución total por NC {referencia}";
                }
                else if (motivosDescuento.Contains(motivo))
                {
                    mensajeAfectado  = $"Descuento/ajuste por NC {referencia}";
                }
                else
                {
                    mensajeAfectado  = $"Afectado por NC {referencia}";
                }
            }
            else if (esND)
            {
                mensajeAfectado = $"Afectado por ND {referencia}";
            }

            await _unitOfWork.Comprobantes.UpdateEstadoSunatAsync(
                note.ComprobanteAfectadoId.Value,
                estadoAfectado ?? "ACEPTADO",
                null,
                null,
                null,
                null,
                null, // No tenemos hash del afectado aquí
                mensajeAfectado
            );
        }

        // 8. Notificar WebSocket
        var sucursalId = await _unitOfWork.Comprobantes.GetSucursalIdByRucAndAnexoAsync(
            note.EmpresaRuc!,
            note.EstablecimientoAnexo!
        );
        _ = Task.Run(() => _wsNotifier.NotifyAsync(sucursalId, note.EmpresaRuc, "status"));

        return await GetNoteByIdAsync(comprobanteId)
            ?? throw new InvalidOperationException("Error al recuperar la nota actualizada");
    }

    private static string ObtenerTipoCarpeta(string tipoDoc) => tipoDoc switch
    {
        "07" => "notas-credito",
        "08" => "notas-debito",
        _ => tipoDoc
    };

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
}