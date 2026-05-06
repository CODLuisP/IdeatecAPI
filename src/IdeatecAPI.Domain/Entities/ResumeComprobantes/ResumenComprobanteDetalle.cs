
namespace IdeatecAPI.Domain.Entities;
public class ResumenComprobanteDetalle
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