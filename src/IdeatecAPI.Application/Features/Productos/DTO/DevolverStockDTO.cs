namespace IdeatecAPI.Application.Features.Productos.DTO;

public class DevolverStockDTO
{
    public int ProductoId { get; set; }
    public int SucursalId { get; set; }
    public int Cantidad { get; set; }
    public string? ReferenciaTipo { get; set; }
    public int? ReferenciaId { get; set; }
}