
using IdeatecAPI.Application.Features.Detraccion.DTOs;
using IdeatecAPI.Application.Features.Notas.DTOs;

namespace IdeatecAPI.Application.Features.Comprobante.DTOs;
public class ObtenerComprobanteDTO
{
    public int ComprobanteId { get; set; }
    public string UblVersion { get; set; } = "2.1"; 
    public string TipoOperacion { get; set; } = "0101";
    public string? TipoComprobante { get; set; }
    public string Serie { get; set; } = string.Empty;
    public string Correlativo { get; set; } = string.Empty;
    public string NumeroCompleto { get; set; } = string.Empty;
    public decimal TipoCambio { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime HoraEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string TipoMoneda { get; set; } = "PEN";
    public string? TipoPago { get; set; } = "Contado";

    public ClienteDTO Cliente { get; set; } = new();
    public EmpresaDTO Company { get; set; } = new();

    // Totales
    public string CodigoTipoDescGlobal { get; set; } = string.Empty;
    public decimal DescuentoGlobal { get; set; }
    public decimal TotalOperacionesGravadas { get; set; }
    public decimal TotalOperacionesExoneradas { get; set; }
    public decimal TotalOperacionesInafectas { get; set; }
    public decimal TotalOperacionesGratuitas { get; set; }
    public decimal TotalIgvGratuitas { get; set; }
    public decimal TotalIGV { get; set; }
    public decimal TotalImpuestos { get; set; }
    public decimal TotalDescuentos { get; set; }
    public decimal TotalOtrosCargos { get; set; }
    public decimal TotalIcbper { get; set; }          
    public decimal ValorVenta { get; set; }                
    public decimal SubTotal { get; set; }                 
    public decimal ImporteTotal { get; set; }
    public decimal MontoCredito { get; set; }

    //Para notas Y su PDF
    public string? TipDocAfectado { get; set; }
    public string? NumDocAfectado { get; set; }
    public string? TipoNotaCreditoDebito { get; set; }
    public string? MotivoNota { get; set; }

    public List<DetalleFacturaDTO> Details { get; set; } = [];
    public List<DetallePagosDTO>? Pagos { get; set; } = [];
    public List<DetalleCuotasDTO>? Cuotas { get; set; } = [];
    public List<NoteLegendDto>? Legends { get; set; } = [];
    public List<GuiaComprobanteDTO>? Guias { get; set; } = [];
    public List<DetraccionDTO>? Detracciones { get; set; } = [];

    // Estado SUNAT
    public string? EstadoSunat { get; set; }
    public string? CodigoHashCPE { get; set; }
    public string? CodigoRespuestaSunat { get; set; }
    public string? MensajeRespuestaSunat { get; set; }
    public string? PdfGenerado { get; set; }
    public bool? EnviadoEnResumen { get; set; }
    public DateTime? FechaEnvioSunat { get; set; }

    // Auditoría
    public int? UsuarioCreacion { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? UsuarioModificacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}

