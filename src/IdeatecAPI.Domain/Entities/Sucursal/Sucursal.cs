namespace IdeatecAPI.Domain.Entities;
public class Sucursal
{
    public int SucursalId { get; set; }
    public string? EmpresaRuc { get; set; }
    public string? CodEstablecimiento { get; set; }
    public string? Nombre { get; set; }
    public string? Direccion { get; set; }
    public string? SerieFactura { get; set; }
    public int? CorrelativoFactura { get; set; }
    public string? SerieBoleta { get; set; }
    public int? CorrelativoBoleta { get; set; }
    public string? SerieNotaCreditoFactura { get; set; }
    public int? CorrelativoNotaCreditoFactura { get; set; }
    public string? SerieNotaCreditoBoleta { get; set; }
    public int? CorrelativoNotaCreditoBoleta { get; set; }
    public string? SerieNotaDebitoFactura { get; set; }
    public int? CorrelativoNotaDebitoFactura { get; set; }
    public string? SerieNotaDebitoBoleta { get; set; }
    public int? CorrelativoNotaDebitoBoleta { get; set; }
    public string? SerieGuiaRemision { get; set; }
    public int? CorrelativoGuiaRemision { get; set; }
    public string? SerieGuiaTransportista { get; set; }
    public int? CorrelativoGuiaTransportista { get; set; }
    public bool? Estado { get; set; }
}