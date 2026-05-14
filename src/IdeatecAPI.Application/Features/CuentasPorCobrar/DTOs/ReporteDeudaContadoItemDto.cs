namespace IdeatecAPI.Application.Features.DeudaContado.DTOs;

public class ReporteDeudaContadoItemDto
{
    // Comprobante
    public int ComprobanteId { get; set; }
    public string? NumeroCompleto { get; set; }
    public string? TipoComprobante { get; set; }
    public DateTime? FechaEmision { get; set; }
    public string? TipoMoneda { get; set; }
    public string? ClienteNumDoc { get; set; }
    public string? ClienteRznSocial { get; set; }
    public decimal? MontoTotal { get; set; }
    public decimal? MontoPagado { get; set; }
    public decimal? Saldo { get; set; }
    public string? Estado { get; set; }

    // Pagos registrados
    public List<ReporteDeudaPagoItemDto> Pagos { get; set; } = new();
}

public class ReporteDeudaPagoItemDto
{
    public DateTime FechaPago { get; set; }
    public decimal MontoPagado { get; set; }
    public string? MedioPago { get; set; }
    public string? EntidadFinanciera { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? Observaciones { get; set; }
}