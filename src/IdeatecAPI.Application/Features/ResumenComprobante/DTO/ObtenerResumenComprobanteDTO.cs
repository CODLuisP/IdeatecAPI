namespace IdeatecAPI.Application.Features.ResumenComprobante.DTO;

public class ObtenerResumenComprobanteDTO
{
    public int ResumenComprobanteId { get; set; }
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

    public ICollection<ObtenerResumenDetalleDTO> DetallesResumen { get; set; } = [];
}

public class ObtenerResumenDetalleDTO
{
    public int ResumenComprobanteDetalleId { get; set; }
    public int LineID { get; set; }
    public int ComprobanteId { get; set; }
    public int? ResumenComprobanteId { get; set; }
    public string TipoComprobante { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Correlativo { get; set; } = string.Empty;

    // ── Nuevos campos cliente ──────────────────────────
    public string? ClienteTipoDoc { get; set; }
    public string? ClienteNumDoc { get; set; }
    public string? ClienteNombre { get; set; }

    // ── Nuevos campos doc afectado ─────────────────────
    public string? DocumentoAfectadoTipo { get; set; }
    public string? DocumentoAfectadoNumero { get; set; }

    public string CodigoCondicion { get; set; } = "1";
    public string Moneda { get; set; } = "PEN";
    public decimal MontoTotalVenta { get; set; }
    public decimal TotalGravado { get; set; }
    public decimal TotalExonerado { get; set; }
    public decimal TotalInafecto { get; set; }
    public decimal TotalGratuito { get; set; }
    public decimal TotalIGV { get; set; }
    public decimal IGVReferencial { get; set; }
}