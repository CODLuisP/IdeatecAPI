namespace IdeatecAPI.Domain.Entities;
public class KardexMovimientoLote
{
    public int KardexMovimientoLoteId { get; set; }
    public int KardexMovimientoId { get; set; }
    public int InventarioLoteId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
}
