using System.Text;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.ComunicacionBaja.DTOs;
using BajaEntity        = IdeatecAPI.Domain.Entities.ComunicacionBaja;
using BajaDetalleEntity = IdeatecAPI.Domain.Entities.ComunicacionBajaDetalle;
using IdeatecAPI.Application.Features.Notas.Services;
using Microsoft.Extensions.Configuration;

namespace IdeatecAPI.Application.Features.ComunicacionBaja.Services;

public interface IBajaService
{
    Task<IEnumerable<BajaDto>> GetAllAsync(int empresaId);
    Task<BajaDto?> GetByIdAsync(int bajaId);
    Task<BajaDto> CreateAsync(CreateBajaDto dto);
    Task<BajaDto> SendToSunatAsync(int bajaId);
    Task DeleteAsync(int bajaId);
}

public class BajaService : IBajaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IXmlBajaBuilderService _xmlBuilder;
    private readonly IXmlSignerService _xmlSigner;
    private readonly ISunatBajaService _sunatBaja;
    private readonly IConfiguration _configuration;

    public BajaService(
        IUnitOfWork unitOfWork,
        IXmlBajaBuilderService xmlBuilder,
        IXmlSignerService xmlSigner,
        ISunatBajaService sunatBaja,
        IConfiguration configuration)
    {
        _unitOfWork    = unitOfWork;
        _xmlBuilder    = xmlBuilder;
        _xmlSigner     = xmlSigner;
        _sunatBaja     = sunatBaja;
        _configuration = configuration;
    }

    public async Task<IEnumerable<BajaDto>> GetAllAsync(int empresaId)
    {
        var bajas  = await _unitOfWork.Bajas.GetAllAsync(empresaId);
        var result = new List<BajaDto>();

        foreach (var baja in bajas)
        {
            var dto = MapToDto(baja);
            dto.Details = (await _unitOfWork.BajaDetalles.GetByBajaIdAsync(baja.BajaId))
                .Select(MapDetalleToDto).ToList();
            result.Add(dto);
        }

        return result;
    }

    public async Task<BajaDto?> GetByIdAsync(int bajaId)
    {
        var baja = await _unitOfWork.Bajas.GetByIdAsync(bajaId);
        if (baja is null) return null;

        var dto = MapToDto(baja);
        dto.Details = (await _unitOfWork.BajaDetalles.GetByBajaIdAsync(bajaId))
            .Select(MapDetalleToDto).ToList();

        return dto;
    }

    public async Task<BajaDto> CreateAsync(CreateBajaDto dto)
    {
        // ── 1. Validaciones ───────────────────────────────────────────────
        if (dto.Details == null || dto.Details.Count == 0)
            throw new InvalidOperationException("La baja debe tener al menos un documento");

        var tiposValidos = new[] { "01", "03", "07", "08" };
        foreach (var d in dto.Details)
        {
            if (!tiposValidos.Contains(d.TipoDoc))
                throw new InvalidOperationException($"TipoDoc '{d.TipoDoc}' no válido para baja");
        }

        // ── 2. Buscar empresa por RUC ─────────────────────────────────────
        var empresa = await _unitOfWork.Empresas.GetEmpresaByRucAsync(dto.Company.Ruc)
            ?? throw new KeyNotFoundException($"Empresa con RUC {dto.Company.Ruc} no encontrada");

        // ── 3. BeginTransaction ───────────────────────────────────────────
        _unitOfWork.BeginTransaction();
        try
        {
            var baja = new BajaEntity
            {
                EmpresaId              = empresa.Id,
                Correlativo            = dto.Correlativo,
                FecGeneracion          = dto.FecGeneracion,
                FecComunicacion        = dto.FecComunicacion,
                EmpresaRuc             = dto.Company.Ruc,
                EmpresaRazonSocial     = dto.Company.RazonSocial,
                EmpresaNombreComercial = dto.Company.NombreComercial,
                EmpresaDireccion       = dto.Company.Address?.Direccion,
                EmpresaProvincia       = dto.Company.Address?.Provincia,
                EmpresaDepartamento    = dto.Company.Address?.Departamento,
                EmpresaDistrito        = dto.Company.Address?.Distrito,
                EmpresaUbigeo          = dto.Company.Address?.Ubigueo,
                EstadoSunat            = "PENDIENTE",
                FechaCreacion          = DateTime.UtcNow
            };

            var newId = await _unitOfWork.Bajas.CreateAsync(baja);

            for (int i = 0; i < dto.Details.Count; i++)
            {
                var d = dto.Details[i];
                await _unitOfWork.BajaDetalles.CreateAsync(new BajaDetalleEntity
                {
                    BajaId        = newId,
                    TipoDoc       = d.TipoDoc,
                    Serie         = d.Serie,
                    Correlativo   = d.Correlativo,
                    DesMotivoBaja = d.DesMotivoBaja
                });
            }

            _unitOfWork.Commit();

            return await GetByIdAsync(newId)
                ?? throw new InvalidOperationException("Error al recuperar la baja creada");
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<BajaDto> SendToSunatAsync(int bajaId)
    {
        var baja = await _unitOfWork.Bajas.GetByIdAsync(bajaId)
            ?? throw new KeyNotFoundException($"Baja {bajaId} no encontrada");

        if (baja.EstadoSunat == "ACEPTADO")
            throw new InvalidOperationException("La baja ya fue aceptada por SUNAT");

        var empresa = await _unitOfWork.Empresas.GetEmpresaByIdAsync(baja.EmpresaId)
            ?? throw new KeyNotFoundException($"Empresa {baja.EmpresaId} no encontrada");

        if (string.IsNullOrEmpty(empresa.CertificadoPem))
            throw new InvalidOperationException("La empresa no tiene certificado digital configurado");

        if (string.IsNullOrEmpty(empresa.SolUsuario) || string.IsNullOrEmpty(empresa.SolClave))
            throw new InvalidOperationException("La empresa no tiene credenciales SOL configuradas");

        var details = (await _unitOfWork.BajaDetalles.GetByBajaIdAsync(bajaId)).ToList();

        // ── Construir y firmar XML ────────────────────────────────────────
        var xmlSinFirmar   = _xmlBuilder.BuildXml(baja, details);
        var xmlFirmadoBytes = _xmlSigner.SignXmlToBytes(
            xmlSinFirmar,
            empresa.CertificadoPem!,
            empresa.CertificadoPassword ?? "123456"
        );

        var xmlFirmadoString = Encoding.UTF8.GetString(xmlFirmadoBytes);
        var serie            = baja.FecGeneracion.ToString("yyyyMMdd");
        var nombreArchivo    = $"{empresa.Ruc}-RA-{serie}-{baja.Correlativo}";

        // ── Guardar archivos localmente ───────────────────────────────────
        await GuardarArchivosAsync(empresa.Ruc, nombreArchivo, xmlFirmadoBytes, null);

        // ── Enviar a SUNAT ────────────────────────────────────────────────
        var sunatResponse = await _sunatBaja.SendBajaAsync(
    xmlFirmadoBytes,
    nombreArchivo,
    empresa.Ruc,
    empresa.SolUsuario!,
    empresa.SolClave!,
    empresa.Environment
);

        // ── Guardar CDR si existe ─────────────────────────────────────────
        if (!string.IsNullOrEmpty(sunatResponse.CdrBase64))
            await GuardarArchivosAsync(empresa.Ruc, nombreArchivo, null, sunatResponse.CdrBase64);

        var nuevoEstado = sunatResponse.Success ? "ACEPTADO" : "RECHAZADO";

        await _unitOfWork.Bajas.UpdateEstadoAsync(
            bajaId,
            nuevoEstado,
            sunatResponse.CodigoRespuesta,
            sunatResponse.Descripcion,
            sunatResponse.Ticket,
            null, // ← no guardar XML en BD, ya está en disco
            null
        );

        return await GetByIdAsync(bajaId)
            ?? throw new InvalidOperationException("Error al recuperar la baja actualizada");
    }

    public async Task DeleteAsync(int bajaId)
    {
        var baja = await _unitOfWork.Bajas.GetByIdAsync(bajaId)
            ?? throw new KeyNotFoundException($"Baja {bajaId} no encontrada");

        if (baja.EstadoSunat == "ACEPTADO")
            throw new InvalidOperationException("No se puede eliminar una baja ya aceptada por SUNAT");

        await _unitOfWork.Bajas.UpdateEstadoAsync(
            bajaId, "ANULADO", null, "Anulado por el usuario", null, null, null);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task GuardarArchivosAsync(
        string ruc, string nombreArchivo,
        byte[]? xmlFirmadoBytes, string? cdrBase64)
    {
        var rutaBase = Path.Combine(
            _configuration["Storage:RutaBajas"] ?? "C:/FacturacionStorage/ComunicacionBaja",
            ruc,
            DateTime.Now.Year.ToString()
        );
        Directory.CreateDirectory(rutaBase);

        if (xmlFirmadoBytes != null)
        {
            using var memStream = new MemoryStream();
            using (var zip = new System.IO.Compression.ZipArchive(memStream,
                System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = zip.CreateEntry(nombreArchivo + ".xml");
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(xmlFirmadoBytes);
            }
            await File.WriteAllBytesAsync(
                Path.Combine(rutaBase, $"{nombreArchivo}.zip"),
                memStream.ToArray());
        }

        if (!string.IsNullOrEmpty(cdrBase64))
        {
            var cdrBytes = Convert.FromBase64String(cdrBase64);
            await File.WriteAllBytesAsync(
                Path.Combine(rutaBase, $"R-{nombreArchivo}.zip"),
                cdrBytes);
        }
    }

    // ── Mappers ───────────────────────────────────────────────────────────

    private static BajaDto MapToDto(BajaEntity b) => new()
    {
        BajaId               = b.BajaId,
        Correlativo          = b.Correlativo,
        FecGeneracion        = b.FecGeneracion,
        FecComunicacion      = b.FecComunicacion,
        EmpresaRuc           = b.EmpresaRuc,
        EmpresaRazonSocial   = b.EmpresaRazonSocial,
        EstadoSunat          = b.EstadoSunat,
        CodigoRespuestaSunat = b.CodigoRespuestaSunat,
        MensajeRespuestaSunat = b.MensajeRespuestaSunat,
        TicketSunat          = b.TicketSunat,
        FechaEnvioSunat      = b.FechaEnvioSunat,
        FechaCreacion        = b.FechaCreacion
    };

    private static BajaDetalleDto MapDetalleToDto(BajaDetalleEntity d) => new()
    {
        DetalleId     = d.DetalleId,
        TipoDoc       = d.TipoDoc,
        Serie         = d.Serie,
        Correlativo   = d.Correlativo,
        DesMotivoBaja = d.DesMotivoBaja
    };
}