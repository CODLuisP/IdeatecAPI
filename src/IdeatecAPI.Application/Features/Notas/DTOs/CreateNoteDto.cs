namespace IdeatecAPI.Application.Features.Notas.DTOs;

public class CreateNoteDto
{
    // Identificación
    public string UblVersion { get; set; } = "2.1";
    public string TipoDoc { get; set; } = string.Empty;        // '07' o '08'
    public string Serie { get; set; } = string.Empty;
    public string Correlativo { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public string TipoMoneda { get; set; } = "PEN";
    public string? TipoOperacion { get; set; }

    // Referencia al comprobante afectado
    public string TipDocAfectado { get; set; } = string.Empty;  // '01', '03'
    public string NumDocAfectado { get; set; } = string.Empty;  // 'F001-00001'
    public int? ComprobanteAfectadoId { get; set; }             // FK opcional
    public string CodMotivo { get; set; } = string.Empty;       // '01', '02'...
    public string DesMotivo { get; set; } = string.Empty;

    // Cliente
    public CreateNoteClientDto Client { get; set; } = new();

    // Forma de pago (opcional, aplica más a notas de débito)
    public CreateNoteFormaPagoDto? FormaPago { get; set; }

    // Totales
    public decimal MtoOperGravadas { get; set; }
    public decimal MtoIGV { get; set; }
    public decimal? ValorVenta { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal MtoImpVenta { get; set; }

    // Detalle y leyendas
    public List<CreateNoteDetailDto> Details { get; set; } = new();
    public List<NoteLegendDto> Legends { get; set; } = new();
}

public class CreateNoteClientDto
{
    public string TipoDoc { get; set; } = string.Empty;
    public string NumDoc { get; set; } = string.Empty;
    public string RznSocial { get; set; } = string.Empty;
    public NoteAddressDto? Address { get; set; }
}

public class CreateNoteFormaPagoDto
{
    public string Moneda { get; set; } = "PEN";
    public string Tipo { get; set; } = "Contado";
}

public class CreateNoteDetailDto
{
    public int ProductoId { get; set; }
    public string? CodProducto { get; set; }
    public string Unidad { get; set; } = "NIU";
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal MtoValorUnitario { get; set; }
    public decimal MtoValorVenta { get; set; }
    public decimal MtoBaseIgv { get; set; }
    public decimal PorcentajeIgv { get; set; } = 18;
    public decimal Igv { get; set; }
    public int TipAfeIgv { get; set; } = 10;
    public decimal MtoPrecioUnitario { get; set; }
}