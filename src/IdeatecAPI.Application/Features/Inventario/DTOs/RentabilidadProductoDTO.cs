namespace IdeatecAPI.Application.Features.Inventario.DTOs;

public class RentabilidadProductoDTO
{
    public int ProductoId { get; set; }
    public string? NomProducto { get; set; }
    public string? Codigo { get; set; }
    public decimal CantidadVendida { get; set; }
    public decimal IngresoVentas { get; set; }
    public decimal CostoVentas { get; set; }
    public decimal UtilidadBruta => IngresoVentas - CostoVentas;
    public decimal MargenPorcentaje => IngresoVentas == 0 ? 0 : (UtilidadBruta / IngresoVentas) * 100;
}
