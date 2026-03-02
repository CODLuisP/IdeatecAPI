namespace IdeatecAPI.Domain.Entities;

public class Comprobante
{
    public int ComprobanteId { get; set; }
    // Empresa (aplanado)
    public int EmpresaId { get; set; }
    public string? EmpresaRuc { get; set; }
    public string? EmpresaRazonSocial { get; set; }
    public string? EmpresaNombreComercial { get; set; }
    public string? EmpresaEstablecimientoAnexo { get; set; }
    public string? EmpresaDireccion { get; set; }
    public string? EmpresaProvincia { get; set; }
    public string? EmpresaDepartamento { get; set; }
    public string? EmpresaDistrito { get; set; }
    public string? EmpresaUbigeo { get; set; }

    // Cliente (aplanado)
    public int? ClienteId { get; set; }
    public string? ClienteTipoDoc { get; set; }
    public string? ClienteNumDoc { get; set; }
    public string? ClienteRazonSocial { get; set; }
    public string? ClienteDireccion { get; set; }
    public string? ClienteProvincia { get; set; }
    public string? ClienteDepartamento { get; set; }
    public string? ClienteDistrito { get; set; }
    public string? ClienteUbigeo { get; set; }

    // Cabecera
    public string? TipoOperacion { get; set; }
    public string? TipoComprobante { get; set; }
    public string? Serie { get; set; }
    public int? Correlativo { get; set; }
    public string? NumeroCompleto { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime HoraEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string? TipoMoneda { get; set; }
    public decimal? TipoCambio { get; set; }

    // Forma de Pago
    public string? TipoPago { get; set; } 

    // Totales
    public decimal? TotalOperacionesGravadas { get; set; }
    public decimal? TotalOperacionesExoneradas { get; set; }
    public decimal? TotalOperacionesInafectas { get; set; }
    public decimal? TotalIGV { get; set; }
    public decimal? TotalImpuestos { get; set; }            // ✅ falta
    public decimal? ValorVenta { get; set; }                // ✅ falta (sin IGV)
    public decimal? SubTotal { get; set; }                  // ✅ falta
    public decimal? TotalDescuentos { get; set; }
    public decimal? TotalOtrosCargos { get; set; }
    public decimal? TotalIcbper { get; set; }
    public decimal? ImporteTotal { get; set; }

    // Relaciones
    public ICollection<ComprobanteDetalle> Detalles { get; set; } = [];   // ✅ falta
    public ICollection<NoteLegend> Leyendas { get; set; } = [];   // ✅ falta
    public ICollection<Pago> Pagos { get; set; } = [];
    public ICollection<Cuota> Cuotas { get; set; } = [];

    // Estado SUNAT
    public string? EstadoSunat { get; set; }
    public string? CodigoHashCPE { get; set; }
    public string? CodigoRespuestaSunat { get; set; }
    public string? MensajeRespuestaSunat { get; set; }
    public string? XmlGenerado { get; set; }
    public string? XmlRespuestaSunat { get; set; }
    public string? CdrSunat { get; set; }
    public DateTime? FechaEnvioSunat { get; set; }

    // Auditoría
    public string? UsuarioCreacion { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public string? UsuarioModificacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}