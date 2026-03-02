using System.IO.Compression;
using System.Text;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Notas.DTOs;
using IdeatecAPI.Application.Features.Notas.Services;
using Microsoft.Extensions.Configuration;

namespace IdeatecAPI.Application.Features.Comprobante.Services;

public interface IComprobanteService
{
    Task<ComprobanteResponse> GenerarComprobanteAsync(GenerarComprobanteDTO dto); //Guardar en BD
    Task<ObtenerComprobanteDTO?> GetComprobanteByIdAsync(int comprobanteId);
    Task<IEnumerable<ObtenerComprobanteDTO>> GetComprobanteByEstadoAsync(string estado);
    Task<ComprobanteResponse> SendToSunatAsync(int comprobanteId); // Generar XML, firmar y enviar a sunat
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

public class ComprobanteService : IComprobanteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IComprobanteXmlService _xmlService;
    private readonly IXmlSignerService _xmlSigner;
    private readonly ISunatSenderService _sunatSender;
    private readonly IConfiguration _configuration;
    private readonly string _rutaXml;

    public ComprobanteService(
        IUnitOfWork unitOfWork,
        IComprobanteXmlService xmlService,
        IXmlSignerService xmlSigner,
        ISunatSenderService sunatSender,
        IConfiguration configuration)
    {
        _unitOfWork  = unitOfWork;
        _xmlService  = xmlService;
        _xmlSigner   = xmlSigner;
        _sunatSender = sunatSender;
        _configuration = configuration;
        _rutaXml = configuration["Storage:RutaXml"] ?? Path.Combine(Directory.GetCurrentDirectory(), "XmlFiles");
    }

    // ── 1. GENERAR Y GUARDAR ─────────────────────────────────────────────────
    public async Task<ComprobanteResponse> GenerarComprobanteAsync(GenerarComprobanteDTO dto)
    {
         // ── 1. Validaciones puras ─────────────────────────────────────────────
        if (dto.TipoComprobante != "01" && dto.TipoComprobante != "03")
            throw new InvalidOperationException("TipoDoc debe ser '01' o '03'");

        if (dto.Details == null || dto.Details.Count == 0)
        throw new InvalidOperationException("El comprobante debe tener al menos un detalle");

        if (dto.Cuotas?.Count == 0 && dto.Pagos?.Count == 0)
        throw new InvalidOperationException("El comprobante debe tener al menos un pago o una cuota");

        // ── 2. Buscar empresa por RUC ─────────────────────────────────────────
        var empresa = await _unitOfWork.Empresas.GetEmpresaByRucAsync(dto.Company.NumeroDocumento ?? "")
            ?? throw new KeyNotFoundException($"Empresa con RUC {dto.Company.NumeroDocumento} no encontrada");

        // ── 3. ClienteId  ────────────
        var cliente = await _unitOfWork.Clientes.GetByNumDocAsync(dto.Cliente.NumeroDocumento ?? "");
        var clienteId = cliente?.ClienteId;

        // ── 4. BeginTransaction ───────────────────────────────────────────────
        _unitOfWork.BeginTransaction();
        try
        {
            // Validación de duplicidad (SIN modificar tu mapeo)
            if (int.TryParse(dto.Correlativo, out var correlativoInt))
            {
                if (await _unitOfWork.Notes.ExisteNoteAsync(
                        empresa.Id,
                        dto.TipoComprobante,
                        dto.Serie,
                        correlativoInt))
                {
                    throw new InvalidOperationException(
                        $"Ya existe el comprobante {dto.Serie}-{dto.Correlativo}");
                }
            }
            
            var comprobante = new Domain.Entities.Comprobante
            {
                TipoOperacion               = dto.TipoOperacion,
                TipoComprobante             = dto.TipoComprobante,
                Serie                       = dto.Serie,
                Correlativo                 = int.TryParse(dto.Correlativo, out var corr) ? corr : null,
                NumeroCompleto              = $"{dto.Serie}-{dto.Correlativo}",
                FechaEmision                = dto.FechaEmision,
                HoraEmision                 = dto.FechaEmision,
                FechaVencimiento            = dto.FechaVencimiento,
                TipoMoneda                  = dto.TipoMoneda,
                TipoPago                    = dto.TipoPago,
                EmpresaId                   = dto.Company.EmpresaId,
                EmpresaRuc                  = dto.Company.NumeroDocumento,
                EmpresaRazonSocial          = dto.Company.RazonSocial,
                EmpresaNombreComercial      = dto.Company.NombreComercial,
                EmpresaEstablecimientoAnexo = dto.Company.EstablecimientoAnexo,
                EmpresaDireccion            = dto.Company.DireccionLineal,
                EmpresaProvincia            = dto.Company.Provincia,
                EmpresaDepartamento         = dto.Company.Departamento,
                EmpresaDistrito             = dto.Company.Distrito,
                EmpresaUbigeo               = dto.Company.Ubigeo,
                ClienteId                   = dto.Cliente.ClienteId,
                ClienteTipoDoc              = dto.Cliente.TipoDocumento,
                ClienteNumDoc               = dto.Cliente.NumeroDocumento,
                ClienteRazonSocial          = dto.Cliente.RazonSocial,
                ClienteDireccion            = dto.Cliente.DireccionLineal,
                ClienteProvincia            = dto.Cliente.Provincia,
                ClienteDepartamento         = dto.Cliente.Departamento,
                ClienteDistrito             = dto.Cliente.Distrito,
                ClienteUbigeo               = dto.Cliente.Ubigeo,
                TotalOperacionesGravadas    = dto.MtoOperGravadas,
                TotalOperacionesExoneradas  = dto.MtoOperExoneradas,
                TotalOperacionesInafectas   = dto.MtoOperInafectas,
                TotalIGV                    = dto.MtoIGV,
                TotalImpuestos              = dto.TotalImpuestos,
                TotalDescuentos             = dto.TotalDescuentos,
                TotalOtrosCargos            = dto.TotalOtrosCargos,
                ValorVenta                  = dto.ValorVenta,
                SubTotal                    = dto.SubTotal,
                TotalIcbper                 = dto.TotalIcbper,
                ImporteTotal                = dto.MtoImpVenta,
                EstadoSunat                 = "PENDIENTE",
                XmlGenerado                 = null,
                FechaCreacion               = DateTime.Now,

                Detalles = dto.Details.Select(d => new Domain.Entities.ComprobanteDetalle
                {
                    Item              = d.Item,
                    ProductoId        = d.ProductoId,
                    Codigo            = d.Codigo,
                    Descripcion       = d.Descripcion,
                    Cantidad          = d.Cantidad,
                    UnidadMedida      = d.UnidadMedida,
                    PrecioUnitario    = d.PrecioUnitario,
                    TipoAfectacionIGV = d.TipoAfectacionIGV?.ToString(),
                    PorcentajeIGV     = d.PorcentajeIGV,
                    MontoIGV          = d.MontoIGV,
                    BaseIgv           = d.BaseIgv,
                    DescuentoUnitario = d.DescuentoUnitario,
                    DescuentoTotal    = d.DescuentoTotal,
                    ValorVenta        = d.ValorVenta,
                    PrecioVenta       = d.PrecioVenta,
                    Icbper            = d.Icbper,
                    FactorIcbper      = d.FactorIcbper
                }).ToList(),

                Pagos = dto.Pagos?.Select(p => new Domain.Entities.Pago
                {
                    MedioPago         = p.MedioPago,
                    Monto             = p.Monto,
                    FechaPago         = p.FechaPago,
                    NumeroOperacion   = p.NumeroOperacion,
                    EntidadFinanciera = p.EntidadFinanciera,
                    Observaciones     = p.Observaciones
                }).ToList() ?? [],

                Cuotas = dto.Cuotas?.Select(c => new Domain.Entities.Cuota
                {
                    NumeroCuota      = c.NumeroCuota,
                    Monto            = c.Monto,
                    FechaVencimiento = c.FechaVencimiento,
                    MontoPagado      = c.MontoPagado,
                    FechaPago        = c.FechaPago,
                    Estado           = c.Estado
                }).ToList() ?? [],

                Leyendas = dto.Legends != null
                    ? [new Domain.Entities.NoteLegend { Code = dto.Legends.Code, Value = dto.Legends.Value }]
                    : []
            };

            int newComprobanteId;

            newComprobanteId = await _unitOfWork.Comprobantes
                .GenerarComprobanteAsync(comprobante);

            comprobante.ComprobanteId = newComprobanteId;

            _unitOfWork.Commit();

            return new ComprobanteResponse
            {
                Exitoso       = true,
                Mensaje       = "Comprobante generado correctamente",
                ComprobanteId = newComprobanteId,
                EstadoSunat   = "PENDIENTE"
            };
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // ── 2. FIRMAR + ENVIAR A SUNAT ───────────────────────────────────────────
    public async Task<ComprobanteResponse> SendToSunatAsync(int comprobanteId)
    {
        // 1. Cargar comprobante desde BD
        var comprobante = await _unitOfWork.Comprobantes.GetByIdAsync(comprobanteId)
            ?? throw new KeyNotFoundException($"Comprobante {comprobanteId} no encontrado");

        if (comprobante.EstadoSunat == "ACEPTADO")
            throw new InvalidOperationException("El comprobante ya fue aceptado por SUNAT");

        // 2. Cargar empresa
        var empresa = await _unitOfWork.Empresas.GetEmpresaByIdAsync(comprobante.EmpresaId)
            ?? throw new KeyNotFoundException($"Empresa {comprobante.EmpresaId} no encontrada");

        if (string.IsNullOrEmpty(empresa.CertificadoPem))
            throw new InvalidOperationException("La empresa no tiene certificado digital configurado");

        if (string.IsNullOrEmpty(empresa.SolUsuario) || string.IsNullOrEmpty(empresa.SolClave))
            throw new InvalidOperationException("La empresa no tiene credenciales SOL configuradas");

        // 3. Recuperar detalles, cuotas y leyendas de BD
        var detalles = (await _unitOfWork.Comprobantes.GetDetallesByIdAsync(comprobanteId)).ToList();
        var cuotas   = (await _unitOfWork.Comprobantes.GetCuotasByIdAsync(comprobanteId)).ToList();
        var leyendas = (await _unitOfWork.Comprobantes.GetLeyendasByIdAsync(comprobanteId)).ToList();

        // 4. Reconstruir DTO completo — igual que cuando se generó originalmente
        var dto = new GenerarComprobanteDTO
        {
            UblVersion       = "2.1",
            TipoOperacion    = comprobante.TipoOperacion!,
            TipoComprobante  = comprobante.TipoComprobante!,
            Serie            = comprobante.Serie!,
            Correlativo      = comprobante.Correlativo?.ToString() ?? "0",
            FechaEmision     = comprobante.FechaEmision,
            FechaVencimiento = comprobante.FechaVencimiento,
            TipoMoneda       = comprobante.TipoMoneda!,
            TipoPago         = comprobante.TipoPago,
            // ← decimales nullable necesitan ?? 0
            MtoOperGravadas  = comprobante.TotalOperacionesGravadas  ?? 0,
            MtoOperExoneradas= comprobante.TotalOperacionesExoneradas ?? 0,
            MtoOperInafectas = comprobante.TotalOperacionesInafectas  ?? 0,
            MtoIGV           = comprobante.TotalIGV           ?? 0,
            TotalImpuestos   = comprobante.TotalImpuestos      ?? 0,
            TotalDescuentos  = comprobante.TotalDescuentos     ?? 0,
            TotalOtrosCargos = comprobante.TotalOtrosCargos    ?? 0,
            ValorVenta       = comprobante.ValorVenta          ?? 0,
            SubTotal         = comprobante.SubTotal            ?? 0,
            TotalIcbper      = comprobante.TotalIcbper         ?? 0,
            MtoImpVenta      = comprobante.ImporteTotal        ?? 0,

            Company = new EmpresaDTO
            {
                EmpresaId            = comprobante.EmpresaId,
                NumeroDocumento      = comprobante.EmpresaRuc!,
                RazonSocial          = comprobante.EmpresaRazonSocial!,
                NombreComercial      = comprobante.EmpresaNombreComercial,
                EstablecimientoAnexo = comprobante.EmpresaEstablecimientoAnexo,
                DireccionLineal      = comprobante.EmpresaDireccion,
                Provincia            = comprobante.EmpresaProvincia,
                Departamento         = comprobante.EmpresaDepartamento,
                Distrito             = comprobante.EmpresaDistrito,
                Ubigeo               = comprobante.EmpresaUbigeo
            },

            Cliente = new ClienteDTO
            {
                ClienteId       = comprobante.ClienteId ?? 0,
                TipoDocumento   = comprobante.ClienteTipoDoc!,
                NumeroDocumento = comprobante.ClienteNumDoc!,
                RazonSocial     = comprobante.ClienteRazonSocial!,
                DireccionLineal = comprobante.ClienteDireccion,
                Provincia       = comprobante.ClienteProvincia,
                Departamento    = comprobante.ClienteDepartamento,
                Distrito        = comprobante.ClienteDistrito,
                Ubigeo          = comprobante.ClienteUbigeo
            },

            Details = detalles.Select(d => new DetalleFacturaDTO
            {
                Item              = d.Item,
                ProductoId        = d.ProductoId,
                Codigo            = d.Codigo,
                Descripcion       = d.Descripcion,
                Cantidad          = d.Cantidad,
                UnidadMedida      = d.UnidadMedida,
                PrecioUnitario    = d.PrecioUnitario,
                TipoAfectacionIGV = d.TipoAfectacionIGV,
                PorcentajeIGV     = d.PorcentajeIGV     ?? 0,
                MontoIGV          = d.MontoIGV           ?? 0,
                BaseIgv           = d.BaseIgv            ?? 0,
                DescuentoUnitario = d.DescuentoUnitario  ?? 0,
                DescuentoTotal    = d.DescuentoTotal      ?? 0,
                ValorVenta        = d.ValorVenta          ?? 0,
                PrecioVenta       = d.PrecioVenta         ?? 0,
                Icbper            = d.Icbper              ?? 0,
                FactorIcbper      = d.FactorIcbper        ?? 0
            }).ToList(),

            Cuotas = cuotas.Select(c => new DetalleCuotasDTO
            {
                NumeroCuota      = c.NumeroCuota,
                Monto            = c.Monto,
                FechaVencimiento = c.FechaVencimiento,
                MontoPagado      = c.MontoPagado,
                FechaPago        = c.FechaPago,
                Estado           = c.Estado
            }).ToList(),

            Legends = leyendas.FirstOrDefault() is { } ley
                ? new NoteLegendDto { Code = ley.Code, Value = ley.Value }
                : null
        };

        // 5. Regenerar XML usando el mismo servicio que al crear
        var xmlResultado = _xmlService.GenerarXml(dto);
        if (!xmlResultado.Exitoso)
            throw new InvalidOperationException($"Error regenerando XML: {xmlResultado.Error}");

        // 6. Firmar XML
        var xmlFirmadoBytes  = _xmlSigner.SignXmlToBytes(
            xmlResultado.XmlString!,
            empresa.CertificadoPem!,
            empresa.CertificadoPassword ?? ""
        );
        var xmlFirmadoString = Encoding.UTF8.GetString(xmlFirmadoBytes);
        var nombreArchivo    = $"{empresa.Ruc}-{comprobante.TipoComprobante}-{comprobante.Serie}-{comprobante.Correlativo:D8}";

        // 7. Guardar ZIP firmado localmente
        await GuardarArchivosAsync(
            ruc:             empresa.Ruc,
            razonSocial:     comprobante.EmpresaRazonSocial!,
            tipoComprobante: comprobante.TipoComprobante!,
            nombreArchivo:   nombreArchivo,
            xmlFirmadoBytes: xmlFirmadoBytes,
            cdrBase64:       null
        );

        // 8. Enviar a SUNAT
        var sunatResponse = await _sunatSender.SendNoteAsync(
            xmlFirmadoBytes,
            nombreArchivo,
            empresa.SolUsuario!,
            empresa.SolClave!,
            empresa.Environment
        );

        // 9. Guardar CDR
        if (!string.IsNullOrEmpty(sunatResponse.CdrBase64))
        {
            await GuardarArchivosAsync(
                ruc:             empresa.Ruc,
                razonSocial:     comprobante.EmpresaRazonSocial!,
                tipoComprobante: comprobante.TipoComprobante!,
                nombreArchivo:   nombreArchivo,
                xmlFirmadoBytes: null,
                cdrBase64:       sunatResponse.CdrBase64
            );
        }

        // 10. Actualizar estado BD
        var nuevoEstado = sunatResponse.Success
            ? (sunatResponse.TieneObservaciones ? "ACEPTADO_CON_OBSERVACIONES" : "ACEPTADO")
            : "RECHAZADO";

        await _unitOfWork.Comprobantes.UpdateEstadoSunatAsync(
            comprobanteId,
            nuevoEstado,
            sunatResponse.CodigoRespuesta,
            sunatResponse.Descripcion,
            xmlFirmadoString,
            sunatResponse.CdrBase64
        );

        return new ComprobanteResponse
        {
            Exitoso          = sunatResponse.Success,
            Mensaje          = sunatResponse.Descripcion,
            ComprobanteId    = comprobanteId,
            EstadoSunat      = nuevoEstado,
            CodigoRespuesta  = sunatResponse.CodigoRespuesta,
            MensajeRespuesta = sunatResponse.Descripcion,
            CdrBase64        = sunatResponse.CdrBase64
        };
    }
        
    // ── HELPERS ──────────────────────────────────────────────────────────────
    private async Task GuardarArchivosAsync(string ruc, string razonSocial, string tipoComprobante,
        string nombreArchivo, byte[]? xmlFirmadoBytes, string? cdrBase64)
    {
        var empresaCarpeta = LimpiarNombreCarpeta(razonSocial);
        var tipoCarpeta = tipoComprobante switch
        {
            "01" => "Facturas",
            "03" => "Boletas",
            "07" => "NotasCredito",
            "08" => "NotasDebito",
            _    => tipoComprobante
        };

        var carpeta = Path.Combine(_rutaXml, empresaCarpeta, tipoCarpeta);
        Directory.CreateDirectory(carpeta);

        // Guardar ZIP firmado
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

        // Guardar CDR
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


    public async Task<ObtenerComprobanteDTO?> GetComprobanteByIdAsync(int comprobanteId)
    {
        var comprobante = await _unitOfWork.Comprobantes.GetByIdAsync(comprobanteId);
        if (comprobante == null)
            return null;

        var detalles = (await _unitOfWork.Comprobantes.GetDetallesByIdAsync(comprobanteId)).ToList();
        var pagos    = (await _unitOfWork.Comprobantes.GetPagosByIdAsync(comprobanteId)).ToList();
        var cuotas   = (await _unitOfWork.Comprobantes.GetCuotasByIdAsync(comprobanteId)).ToList();
        var leyendas = (await _unitOfWork.Comprobantes.GetLeyendasByIdAsync(comprobanteId)).ToList();

        return MapToDto(comprobante, detalles, pagos, cuotas, leyendas);
    }

    public async Task<IEnumerable<ObtenerComprobanteDTO>> GetComprobanteByEstadoAsync(string estado)
    {
        var comprobantes = await _unitOfWork.Comprobantes.GetByEstadoAsync(estado);

        var lista = new List<ObtenerComprobanteDTO>();

        foreach (var comprobante in comprobantes)
        {
            var detalles = (await _unitOfWork.Comprobantes
                .GetDetallesByIdAsync(comprobante.ComprobanteId)).ToList();

            var pagos = (await _unitOfWork.Comprobantes
                .GetPagosByIdAsync(comprobante.ComprobanteId)).ToList();

            var cuotas = (await _unitOfWork.Comprobantes
                .GetCuotasByIdAsync(comprobante.ComprobanteId)).ToList();

            var leyendas = (await _unitOfWork.Comprobantes
                .GetLeyendasByIdAsync(comprobante.ComprobanteId)).ToList();

            lista.Add(MapToDto(comprobante, detalles, pagos, cuotas, leyendas));
        }

        return lista;
    }

    private static ObtenerComprobanteDTO MapToDto(
    Domain.Entities.Comprobante comprobante,
    List<Domain.Entities.ComprobanteDetalle> detalles,
    List<Domain.Entities.Pago> pagos,
    List<Domain.Entities.Cuota> cuotas,
    List<Domain.Entities.NoteLegend> leyendas)
    {
        return new ObtenerComprobanteDTO
        {
            UblVersion      = "2.1",
            TipoOperacion   = comprobante.TipoOperacion ?? "0101",
            TipoComprobante = comprobante.TipoComprobante,
            Serie           = comprobante.Serie ?? "",
            Correlativo     = comprobante.Correlativo?.ToString() ?? "",
            NumeroCompleto  = comprobante.NumeroCompleto ?? "",
            TipoCambio      = 0, // si luego manejas TC lo puedes mapear aquí
            FechaEmision    = comprobante.FechaEmision,
            FechaVencimiento= comprobante.FechaVencimiento,
            TipoMoneda      = comprobante.TipoMoneda ?? "PEN",
            TipoPago        = comprobante.TipoPago,

            Cliente = new ClienteDTO
            {
                ClienteId       = comprobante.ClienteId,
                TipoDocumento   = comprobante.ClienteTipoDoc,
                NumeroDocumento = comprobante.ClienteNumDoc,
                RazonSocial     = comprobante.ClienteRazonSocial,
                DireccionLineal = comprobante.ClienteDireccion,
                Provincia       = comprobante.ClienteProvincia,
                Departamento    = comprobante.ClienteDepartamento,
                Distrito        = comprobante.ClienteDistrito,
                Ubigeo          = comprobante.ClienteUbigeo
            },

            Company = new EmpresaDTO
            {
                EmpresaId            = comprobante.EmpresaId,
                NumeroDocumento      = comprobante.EmpresaRuc,
                RazonSocial          = comprobante.EmpresaRazonSocial,
                NombreComercial      = comprobante.EmpresaNombreComercial,
                EstablecimientoAnexo = comprobante.EmpresaEstablecimientoAnexo,
                DireccionLineal      = comprobante.EmpresaDireccion,
                Provincia            = comprobante.EmpresaProvincia,
                Departamento         = comprobante.EmpresaDepartamento,
                Distrito             = comprobante.EmpresaDistrito,
                Ubigeo               = comprobante.EmpresaUbigeo
            },

            // Totales
            MtoOperGravadas  = comprobante.TotalOperacionesGravadas ?? 0,
            MtoOperExoneradas= comprobante.TotalOperacionesExoneradas ?? 0,
            MtoOperInafectas = comprobante.TotalOperacionesInafectas ?? 0,
            MtoIGV           = comprobante.TotalIGV ?? 0,
            TotalImpuestos   = comprobante.TotalImpuestos ?? 0,
            TotalDescuentos  = comprobante.TotalDescuentos ?? 0,
            TotalOtrosCargos = comprobante.TotalOtrosCargos ?? 0,
            ValorVenta       = comprobante.ValorVenta ?? 0,
            SubTotal         = comprobante.SubTotal ?? 0,
            TotalIcbper      = comprobante.TotalIcbper ?? 0,
            MtoImpVenta      = comprobante.ImporteTotal ?? 0,

            Details = detalles.Select(d => new DetalleFacturaDTO
            {
                DetalleId        = d.DetalleId,
                ComprobanteId    = d.ComprobanteId,
                Item             = d.Item,
                ProductoId       = d.ProductoId,
                Codigo           = d.Codigo,
                Descripcion      = d.Descripcion,
                Cantidad         = d.Cantidad,
                UnidadMedida     = d.UnidadMedida,
                PrecioUnitario   = d.PrecioUnitario,
                TipoAfectacionIGV= d.TipoAfectacionIGV,
                PorcentajeIGV    = d.PorcentajeIGV ?? 0,
                MontoIGV         = d.MontoIGV ?? 0,
                BaseIgv          = d.BaseIgv ?? 0,
                DescuentoUnitario= d.DescuentoUnitario ?? 0,
                DescuentoTotal   = d.DescuentoTotal ?? 0,
                ValorVenta       = d.ValorVenta ?? 0,
                PrecioVenta      = d.PrecioVenta ?? 0,
                Icbper           = d.Icbper ?? 0,
                FactorIcbper     = d.FactorIcbper ?? 0
            }).ToList(),

            Pagos = pagos.Select(p => new DetallePagosDTO
            {
                ComprobanteId   = p.ComprobanteId,
                MedioPago       = p.MedioPago,
                Monto           = p.Monto,
                FechaPago       = p.FechaPago,
                NumeroOperacion = p.NumeroOperacion,
                EntidadFinanciera = p.EntidadFinanciera,
                Observaciones   = p.Observaciones
            }).ToList(),

            Cuotas = cuotas.Select(c => new DetalleCuotasDTO
            {
                ComprobanteId   = c.ComprobanteId,
                NumeroCuota     = c.NumeroCuota,
                Monto           = c.Monto,
                FechaVencimiento= c.FechaVencimiento,
                MontoPagado     = c.MontoPagado,
                FechaPago       = c.FechaPago,
                Estado          = c.Estado
            }).ToList(),

            Legends = leyendas.FirstOrDefault() is { } ley
                ? new NoteLegendDto
                {
                    Code  = ley.Code,
                    Value = ley.Value
                }
                : null,

            EstadoSunat             = comprobante.EstadoSunat,
            CodigoRespuestaSunat    = comprobante.CodigoRespuestaSunat,
            MensajeRespuestaSunat   = comprobante.MensajeRespuestaSunat,
            FechaEnvioSunat         = comprobante.FechaEnvioSunat,
            UsuarioCreacion         = comprobante.UsuarioCreacion,
            FechaCreacion           = comprobante.FechaCreacion,
            UsuarioModificacion     = comprobante.UsuarioModificacion,
            FechaModificacion       = comprobante.FechaModificacion
        };
    }

}