namespace IdeatecAPI.Application.Features.Reportes.DTOs;

public class ControlCajaTicketItemDto
{
    public int    ComprobanteId       { get; set; }
    public string? TipoComprobante    { get; set; }
    public string  Serie              { get; set; } = "";
    public int?    Correlativo        { get; set; }
    public string  NumeroCompleto     { get; set; } = "";
    public DateTime FechaEmision      { get; set; }
    public decimal  ImporteTotal      { get; set; }
    public decimal  ValorVenta        { get; set; }
    public decimal  TotalIGV          { get; set; }
    public string   TipoMoneda        { get; set; } = "PEN";
    public string?  EstadoSunat       { get; set; }
    public int?     ComprobanteAfectadoId { get; set; }
    public string?  NumDocAfectado    { get; set; }
    public List<PagoResumenDto> Pagos { get; set; } = new();
}

public class PagoResumenDto
{
    public string? MedioPago { get; set; }
    public decimal Monto     { get; set; }
}
