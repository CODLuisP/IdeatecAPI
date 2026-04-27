namespace IdeatecAPI.Application.Features.CuentasPorCobrar.DTOs;

public class PagarCuotaDto
{
    public int CuotaId { get; set; }
    public decimal MontoPagado { get; set; }
    public DateTime FechaPago { get; set; }
    public string? MedioPago { get; set; }
    public string? EntidadFinanciera { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? Observaciones { get; set; }
    public int UsuarioRegistroPago { get; set; }

    // Campos de descuento por pronto pago
    public decimal? TasaDescuentoDiaria { get; set; }
    public int? DiasAnticipacion { get; set; }
    public decimal? PorcentajeDescuento { get; set; }
    public decimal? MontoDescuento { get; set; }
    public string? MotivoDescuento { get; set; }
    public decimal? MontoFinal { get; set; }
}