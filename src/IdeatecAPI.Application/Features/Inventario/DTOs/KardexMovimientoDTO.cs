namespace IdeatecAPI.Application.Features.Inventario.DTOs;

public class KardexMovimientoDTO
{
    public int KardexMovimientoId { get; set; }
    public int SucursalProductoId { get; set; }
    public string TipoMovimiento { get; set; } = string.Empty;
    public string? ReferenciaTipo { get; set; }
    public int? ReferenciaId { get; set; }
    public int? ComprobanteDetalleId { get; set; }
    // Cantidad/costo REAL del inventario (unidades base, costo PEPS): la ficha del
    // movimiento tal como afectó el stock físico. No cambia según sea venta de
    // paquete o de unidad suelta.
    public decimal Cantidad { get; set; }
    public decimal? CostoUnitarioPromedio { get; set; }
    public decimal? CostoTotal { get; set; }
    public decimal SaldoCantidadPost { get; set; }
    public decimal SaldoValorPost { get; set; }
    public DateTime FechaMovimiento { get; set; }
    public int LotesConsumidos { get; set; }

    // Qué se vendió realmente en esa línea (puede ser un paquete, no el producto base
    // cuyo stock aparece arriba). Igual que en Rentabilidad: si la línea no tiene
    // comprobanteDetalleId (movimiento legado, o no es una venta), estos campos caen
    // de vuelta al producto base y a Cantidad/CostoTotal.
    public int? ProductoId { get; set; }
    public string? NomProducto { get; set; }
    public string? Codigo { get; set; }
    public bool EsPaquete { get; set; }
    public decimal CantidadVenta { get; set; }
    public decimal? CostoVenta { get; set; }
}
