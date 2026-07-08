namespace IdeatecAPI.Application.Features.Inventario.DTOs;

public class ConsumoPepsResultDTO
{
    public int SucursalProductoId { get; set; }
    public decimal CantidadConsumida { get; set; }
    public decimal CostoUnitarioPromedio { get; set; }
    public decimal CostoTotal { get; set; }
    public int KardexMovimientoId { get; set; }
}
