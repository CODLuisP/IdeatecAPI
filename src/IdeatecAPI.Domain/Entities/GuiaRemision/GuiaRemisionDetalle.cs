namespace IdeatecAPI.Domain.Entities;

public class GuiaRemisionDetalle
{
    public int DetalleId { get; set; }
    public int GuiaId { get; set; }
    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Codigo { get; set; }
}