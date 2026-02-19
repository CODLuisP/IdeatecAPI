namespace IdeatecAPI.Domain.Entities;

public class Note
{
    public int ComprobanteId { get; set; }
    public int EmpresaId { get; set; }
    public int ClienteId { get; set; }

    // Identificación del comprobante
    public string UblVersion { get; set; } = "2.1";
    public string TipoDoc { get; set; } = string.Empty;       // '07' o '08'
    public string Serie { get; set; } = string.Empty;
    public int Correlativo { get; set; }
    public string NumeroCompleto { get; set; } = string.Empty; // columna generada
    public DateTime FechaEmision { get; set; }
    public string TipoMoneda { get; set; } = "PEN";
    public string? TipoOperacion { get; set; }

    // Referencia al comprobante afectado
    public int? ComprobanteAfectadoId { get; set; }            // FK opcional
    public string? TipDocAfectado { get; set; }                // '01', '03'
    public string? NumDocAfectado { get; set; }                // 'F001-00001'
    public string TipoNotaCreditoDebito { get; set; } = string.Empty; // codMotivo
    public string MotivoNota { get; set; } = string.Empty;            // desMotivo

    // Cliente (denormalizado para el XML)
    public string ClienteTipoDoc { get; set; } = string.Empty;
    public string ClienteNumDoc { get; set; } = string.Empty;
    public string ClienteRznSocial { get; set; } = string.Empty;
    public string? ClienteDireccion { get; set; }
    public string? ClienteProvincia { get; set; }
    public string? ClienteDepartamento { get; set; }
    public string? ClienteDistrito { get; set; }
    public string? ClienteUbigeo { get; set; }

    // Forma de pago
    public string? FormaPagoMoneda { get; set; }
    public string? FormaPagoTipo { get; set; }

    // Totales
    public decimal MtoOperGravadas { get; set; }
    public decimal MtoIGV { get; set; }
    public decimal? ValorVenta { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal MtoImpVenta { get; set; }

    // Resultado SUNAT
    public string? XmlGenerado { get; set; }
    public string? XmlRespuestaSunat { get; set; }
    public string? CdrSunat { get; set; }
    public string? CodigoRespuestaSunat { get; set; }
    public string? MensajeRespuestaSunat { get; set; }
    public string EstadoSunat { get; set; } = "PENDIENTE";
    public DateTime? FechaEnvioSunat { get; set; }

    // Auditoría
    public string? UsuarioCreacion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? UsuarioModificacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    // Navegación
    public List<NoteDetail> Details { get; set; } = new();
    public List<NoteLegend> Legends { get; set; } = new();
}