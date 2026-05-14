namespace IdeatecAPI.Application.Features.CuentasPorCobrar.DTOs;

public class ReporteCuentasPorCobrarItemDto
{
    public int ComprobanteId { get; set; }
    public string? NumeroCompleto { get; set; }
    public string? TipoComprobante { get; set; }
    public DateTime? FechaEmision { get; set; }
    public string? TipoMoneda { get; set; }
    public string? ClienteNumDoc { get; set; }
    public string? ClienteRznSocial { get; set; }
    public decimal? ImporteTotal { get; set; }
    public decimal? MontoCredito { get; set; }
    public string? Estado { get; set; } // PAGADO | PENDIENTE

    public List<ReporteCuotaItemDto> Cuotas { get; set; } = new();
}

public class ReporteCuotaItemDto
{
    public string? NumeroCuota { get; set; }
    public decimal? Monto { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal? MontoPagado { get; set; }
    public decimal? Saldo { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? Estado { get; set; }
}