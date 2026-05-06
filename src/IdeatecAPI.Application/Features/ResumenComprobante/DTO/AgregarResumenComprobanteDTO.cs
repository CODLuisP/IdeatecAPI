namespace IdeatecAPI.Application.Features.ResumenComprobante.DTO;

public class AgregarResumenComprobanteDTO
{
    public int EmpresaId { get; set; }
    public string? EmpresaRuc { get; set; }
    public string? EmpresaRazonSocial { get; set; }
    
    // ── Nuevos campos empresa ──────────────────────────
    public string? EmpresaDireccion { get; set; }
    public string? EmpresaProvincia { get; set; }
    public string? EmpresaDepartamento { get; set; }
    public string? EmpresaDistrito { get; set; }
    public string? EmpresaUbigeo { get; set; }
    public string? EstablecimientoAnexo { get; set; }

    public int NumeroEnvio { get; set; }
    public DateTime FechaEmisionDocumentos { get; set; }
    public DateTime FechaGeneracion { get; set; }
    public string Identificador { get; set; } = string.Empty;
    public string EstadoSunat { get; set; } = string.Empty;
    public string Ticket { get; set; } = string.Empty;
    public string CodigoRespuesta { get; set; } = string.Empty;
    public string MensajeRespuesta { get; set; } = string.Empty;
    public string XmlGenerado { get; set; } = string.Empty;
    public string? PdfGenerado { get; set; }
    public int? UsuarioCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }

    public ICollection<AgregarResumenDetalleDTO> DetallesResumen { get; set; } = [];
}