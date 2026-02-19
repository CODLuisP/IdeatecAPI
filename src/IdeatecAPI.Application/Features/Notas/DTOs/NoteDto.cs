namespace IdeatecAPI.Application.Features.Notas.DTOs;

public class NoteDto
{
    public int ComprobanteId { get; set; }
    public string TipoDoc { get; set; } = string.Empty;
    public string TipoDocDescripcion => TipoDoc == "07" ? "Nota de Crédito" : "Nota de Débito";
    public string Serie { get; set; } = string.Empty;
    public int Correlativo { get; set; }
    public string NumeroCompleto { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public string TipoMoneda { get; set; } = string.Empty;

    // Referencia al afectado
    public int? ComprobanteAfectadoId { get; set; }
    public string? TipDocAfectado { get; set; }
    public string? NumDocAfectado { get; set; }
    public string CodMotivo { get; set; } = string.Empty;
    public string DesMotivo { get; set; } = string.Empty;

    // Cliente
    public string ClienteTipoDoc { get; set; } = string.Empty;
    public string ClienteNumDoc { get; set; } = string.Empty;
    public string ClienteRznSocial { get; set; } = string.Empty;

    // Totales
    public decimal MtoOperGravadas { get; set; }
    public decimal MtoIGV { get; set; }
    public decimal MtoImpVenta { get; set; }

    // Estado SUNAT
    public string EstadoSunat { get; set; } = string.Empty;
    public string? CodigoRespuestaSunat { get; set; }
    public string? MensajeRespuestaSunat { get; set; }
    public DateTime? FechaEnvioSunat { get; set; }

    // Detalle
    public List<NoteDetailDto> Details { get; set; } = new();
    public List<NoteLegendDto> Legends { get; set; } = new();

    public DateTime FechaCreacion { get; set; }
}