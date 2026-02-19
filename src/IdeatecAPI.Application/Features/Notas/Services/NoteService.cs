using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Notas.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Notas.Services;

public interface INoteService
{
    Task<IEnumerable<NoteDto>> GetAllNotesAsync(int empresaId);
    Task<NoteDto?> GetNoteByIdAsync(int comprobanteId);
    Task<NoteDto> CreateNoteAsync(int empresaId, int clienteId, CreateNoteDto dto);
    Task<NoteDto> UpdateEstadoSunatAsync(int comprobanteId, UpdateNoteEstadoDto dto);
    Task DeleteNoteAsync(int comprobanteId);
}

public class NoteService : INoteService
{
    private readonly IUnitOfWork _unitOfWork;

    public NoteService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

    public async Task<NoteDto> CreateNoteAsync(int empresaId, int clienteId, CreateNoteDto dto)
    {
        // ── 1. Validaciones puras de negocio (sin tocar la BD) ───────────────
        if (dto.TipoDoc != "07" && dto.TipoDoc != "08")
            throw new InvalidOperationException("TipoDoc debe ser '07' (Nota de Crédito) o '08' (Nota de Débito)");

        if (!int.TryParse(dto.Correlativo, out var correlativoInt))
            throw new InvalidOperationException("El correlativo debe ser un número entero");

        if (dto.Details == null || dto.Details.Count == 0)
            throw new InvalidOperationException("La nota debe tener al menos un detalle");

        // ── 2. BeginTransaction ANTES de cualquier acceso al repositorio ─────
        _unitOfWork.BeginTransaction();
        try
        {
            // ── 3. Validaciones que SÍ requieren BD (dentro de la transacción) ─
            if (await _unitOfWork.Notes.ExisteNoteAsync(empresaId, dto.TipoDoc, dto.Serie, correlativoInt))
                throw new InvalidOperationException($"Ya existe una nota {dto.Serie}-{dto.Correlativo}");

            // ── 4. Crear la nota ─────────────────────────────────────────────
            var note = new Note
            {
                EmpresaId             = empresaId,
                ClienteId             = clienteId,
                TipoDoc               = dto.TipoDoc,
                Serie                 = dto.Serie,
                Correlativo           = correlativoInt,
                FechaEmision          = dto.FechaEmision,
                TipoMoneda            = dto.TipoMoneda,
                TipoOperacion         = dto.TipoOperacion,
                ComprobanteAfectadoId = dto.ComprobanteAfectadoId,
                TipDocAfectado        = dto.TipDocAfectado,
                NumDocAfectado        = dto.NumDocAfectado,
                TipoNotaCreditoDebito = dto.CodMotivo,
                MotivoNota            = dto.DesMotivo,
                ClienteTipoDoc        = dto.Client.TipoDoc,
                ClienteNumDoc         = dto.Client.NumDoc,
                ClienteRznSocial      = dto.Client.RznSocial,
                ClienteDireccion      = dto.Client.Address?.Direccion,
                ClienteProvincia      = dto.Client.Address?.Provincia,
                ClienteDepartamento   = dto.Client.Address?.Departamento,
                ClienteDistrito       = dto.Client.Address?.Distrito,
                ClienteUbigeo         = dto.Client.Address?.Ubigueo,
                FormaPagoMoneda       = dto.FormaPago?.Moneda,
                FormaPagoTipo         = dto.FormaPago?.Tipo,
                MtoOperGravadas       = dto.MtoOperGravadas,
                MtoIGV                = dto.MtoIGV,
                ValorVenta            = dto.ValorVenta,
                TotalImpuestos        = dto.TotalImpuestos,
                SubTotal              = dto.SubTotal,
                MtoImpVenta           = dto.MtoImpVenta,
                EstadoSunat           = "PENDIENTE",
                FechaCreacion         = DateTime.UtcNow
            };

            var newId = await _unitOfWork.Notes.CreateNoteAsync(note);
            note.ComprobanteId = newId;

            // ── 5. Insertar detalles ─────────────────────────────────────────
            for (int i = 0; i < dto.Details.Count; i++)
            {
                var d = dto.Details[i];
                var detail = new NoteDetail
                {
                    ComprobanteId     = newId,
                    Item              = i + 1,
                    CodProducto       = d.CodProducto,
                    Unidad            = d.Unidad,
                    Descripcion       = d.Descripcion,
                    Cantidad          = d.Cantidad,
                    MtoValorUnitario  = d.MtoValorUnitario,
                    MtoValorVenta     = d.MtoValorVenta,
                    MtoBaseIgv        = d.MtoBaseIgv,
                    PorcentajeIGV     = d.PorcentajeIgv,
                    Igv               = d.Igv,
                    TipAfeIgv         = d.TipAfeIgv,
                    TotalImpuestos    = d.TotalImpuestos,
                    MtoPrecioUnitario = d.MtoPrecioUnitario,
                    TipoAfectacionIGV = d.TipAfeIgv.ToString()
                };
                await _unitOfWork.NoteDetails.CreateDetailAsync(detail);
            }

            // ── 6. Insertar leyendas ─────────────────────────────────────────
            foreach (var l in dto.Legends)
            {
                var legend = new NoteLegend
                {
                    ComprobanteId = newId,
                    Code          = l.Code,
                    Value         = l.Value
                };
                await _unitOfWork.NoteLegends.CreateLegendAsync(legend);
            }

            _unitOfWork.Commit();

            // ── 7. Recuperar con detalles y leyendas ya confirmados ──────────
            return await GetNoteByIdAsync(newId)
                ?? throw new InvalidOperationException("Error al recuperar la nota creada");
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
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

    // ── Mappers ──────────────────────────────────────────────────────────────

    private static NoteDto MapToDto(Note n) => new()
    {
        ComprobanteId         = n.ComprobanteId,
        TipoDoc               = n.TipoDoc,
        Serie                 = n.Serie,
        Correlativo           = n.Correlativo,
        NumeroCompleto        = n.NumeroCompleto,
        FechaEmision          = n.FechaEmision,
        TipoMoneda            = n.TipoMoneda,
        ComprobanteAfectadoId = n.ComprobanteAfectadoId,
        TipDocAfectado        = n.TipDocAfectado,
        NumDocAfectado        = n.NumDocAfectado,
        CodMotivo             = n.TipoNotaCreditoDebito,
        DesMotivo             = n.MotivoNota,
        ClienteTipoDoc        = n.ClienteTipoDoc,
        ClienteNumDoc         = n.ClienteNumDoc,
        ClienteRznSocial      = n.ClienteRznSocial,
        MtoOperGravadas       = n.MtoOperGravadas,
        MtoIGV                = n.MtoIGV,
        MtoImpVenta           = n.MtoImpVenta,
        EstadoSunat           = n.EstadoSunat,
        CodigoRespuestaSunat  = n.CodigoRespuestaSunat,
        MensajeRespuestaSunat = n.MensajeRespuestaSunat,
        FechaEnvioSunat       = n.FechaEnvioSunat,
        FechaCreacion         = n.FechaCreacion
    };

    private static NoteDetailDto MapDetailToDto(NoteDetail d) => new()
    {
        DetalleId         = d.DetalleId,
        CodProducto       = d.CodProducto,
        Unidad            = d.Unidad,
        Descripcion       = d.Descripcion,
        Cantidad          = d.Cantidad,
        MtoValorUnitario  = d.MtoValorUnitario,
        MtoValorVenta     = d.MtoValorVenta,
        MtoBaseIgv        = d.MtoBaseIgv,
        PorcentajeIgv     = d.PorcentajeIGV,
        Igv               = d.Igv,
        TipAfeIgv         = d.TipAfeIgv,
        TotalImpuestos    = d.TotalImpuestos,
        MtoPrecioUnitario = d.MtoPrecioUnitario
    };

    private static NoteLegendDto MapLegendToDto(NoteLegend l) => new()
    {
        Code  = l.Code,
        Value = l.Value
    };
}