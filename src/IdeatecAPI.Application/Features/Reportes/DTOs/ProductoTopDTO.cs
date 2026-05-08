namespace IdeatecAPI.Application.Features.Reportes.DTOs;

public class ProductoTopDTO
{
    public string? Codigo { get; set; }
    public string? Descripcion { get; set; }
    public decimal TotalCantidad { get; set; }
    public decimal TotalMonto { get; set; }
    public decimal TotalIGV { get; set; }
    public int VecesVendido { get; set; }
    public decimal PrecioPromedio { get; set; }
}