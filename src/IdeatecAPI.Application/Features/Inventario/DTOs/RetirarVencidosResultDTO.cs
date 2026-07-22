namespace IdeatecAPI.Application.Features.Inventario.DTOs;

public class RetirarVencidosResultDTO
{
    public int TotalLotesRetirados { get; set; }
    public int TotalProductosAfectados { get; set; }
    public decimal TotalCantidadRetirada { get; set; }
    public decimal TotalCostoRetirado { get; set; }
}
