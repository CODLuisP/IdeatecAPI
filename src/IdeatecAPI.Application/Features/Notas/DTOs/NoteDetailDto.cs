namespace IdeatecAPI.Application.Features.Notas.DTOs;

public class NoteDetailDto
{
    public int DetalleId { get; set; }
    public string? CodProducto { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal MtoValorUnitario { get; set; }
    public decimal MtoValorVenta { get; set; }
    public decimal MtoBaseIgv { get; set; }
    public decimal PorcentajeIgv { get; set; }
    public decimal Igv { get; set; }
    public int TipAfeIgv { get; set; }
    public decimal MtoPrecioUnitario { get; set; }
}