namespace IdeatecAPI.Application.Features.Inventario.DTOs;

public class RegistrarSaldoInicialDTO
{
    public int SucursalProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public DateTime? Fecha { get; set; }
    public int? IdUsuario { get; set; }
}
