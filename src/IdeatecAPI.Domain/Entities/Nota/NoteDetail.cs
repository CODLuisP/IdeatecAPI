namespace IdeatecAPI.Domain.Entities;

public class NoteDetail
{
    public int DetalleId { get; set; }
    public int ComprobanteId { get; set; }
    public int Item { get; set; }
    public int? ProductoId { get; set; }
    public string? CodProducto { get; set; }
    public string Unidad { get; set; } = "NIU";
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string TipoAfectacionIGV { get; set; } = "10";
    public decimal PorcentajeIGV { get; set; } = 18;
    public decimal MontoIGV { get; set; }
    public decimal DescuentoUnitario { get; set; }
    public decimal DescuentoTotal { get; set; }
    public decimal MtoValorUnitario { get; set; }
    public decimal MtoValorVenta { get; set; }
    public decimal MtoBaseIgv { get; set; }
    public decimal Igv { get; set; }
    public int TipAfeIgv { get; set; } = 10;
    public decimal TotalImpuestos { get; set; }
    public decimal MtoPrecioUnitario { get; set; }
}