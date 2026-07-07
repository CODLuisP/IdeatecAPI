namespace IdeatecAPI.Application.Features.Inventario.DTOs;

public class StockValorizadoDTO
{
    public int SucursalProductoId { get; set; }
    public string? NomProducto { get; set; }
    public string? Codigo { get; set; }
    public decimal StockActual { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal CostoPromedioActual => StockActual == 0 ? 0 : ValorTotal / StockActual;
    public IEnumerable<LoteReporteDTO> Lotes { get; set; } = [];
}
