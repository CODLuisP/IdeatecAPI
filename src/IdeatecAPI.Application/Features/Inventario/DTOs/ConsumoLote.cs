namespace IdeatecAPI.Application.Features.Inventario.DTOs;

/// <summary>Cuánto se descuenta de un lote concreto.</summary>
public class ConsumoLote
{
    public int InventarioLoteId { get; set; }
    public decimal Cantidad { get; set; }
}

/// <summary>Saldo en cantidad y saldo valorizado de los lotes de un producto.</summary>
public class SaldosLote
{
    public decimal Cantidad { get; set; }
    public decimal Valor { get; set; }
}

/// <summary>Una línea de venta ya resuelta al producto cuyo stock se descuenta.</summary>
public class ConsumoProducto
{
    public int SucursalProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public string? ReferenciaTipo { get; set; }
    public int? ReferenciaId { get; set; }
}
