namespace IdeatecAPI.Application.Features.Inventario.DTOs;

public class HistorialVencidoDTO
{
    public int KardexMovimientoId { get; set; }
    public int SucursalProductoId { get; set; }
    public string? NomProducto { get; set; }
    public string? Codigo { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitarioPromedio { get; set; }
    public decimal CostoTotal { get; set; }
    public DateTime FechaMovimiento { get; set; }
}
