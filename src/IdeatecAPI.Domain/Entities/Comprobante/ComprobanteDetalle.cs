namespace IdeatecAPI.Domain.Entities;
public class ComprobanteDetalle
{
    public int DetalleId { get; set; }
    public int ComprobanteId { get; set; }
    public int? Item { get; set; }
    public int? ProductoId { get; set; }
    public string? Codigo { get; set; }
    public string? Descripcion { get; set; }
    public decimal Cantidad { get; set; }
    public string? UnidadMedida { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string? TipoAfectacionIGV { get; set; }
    public decimal? PorcentajeIGV { get; set; }
    public decimal? MontoIGV { get; set; }
    public decimal? BaseIgv { get; set; }
    public decimal? DescuentoUnitario { get; set; }
    public decimal? DescuentoTotal { get; set; }
    public decimal? ValorVenta { get; set; }
    public decimal? PrecioVenta { get; set; }
    public decimal? Icbper { get; set; }
    public decimal? FactorIcbper { get; set; }
}

