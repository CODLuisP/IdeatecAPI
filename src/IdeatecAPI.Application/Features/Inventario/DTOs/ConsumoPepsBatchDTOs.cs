using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Inventario.DTOs;

// Una salida PEPS a procesar. Se agrupan varias en un solo lote para que el descuento
// de stock de una venta completa no dispare una tanda de consultas por cada producto.
public class ConsumoPepsRequestDTO
{
    public int SucursalProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public string TipoMovimiento { get; set; } = "SALIDA_VENTA";
    public string? ReferenciaTipo { get; set; }
    public int? ReferenciaId { get; set; }
}

// Saldo acumulado de los lotes de un producto, leído en bloque para todos los productos
// de la venta en vez de con dos ExecuteScalar por producto.
public class SaldoLotesDTO
{
    public int SucursalProductoId { get; set; }
    public decimal SaldoCantidad { get; set; }
    public decimal SaldoValor { get; set; }
}

// Cabecera de kardex junto con el detalle de lotes que la componen, para insertarlas
// todas de una sola vez.
public class KardexMovimientoConDetalle
{
    public required KardexMovimiento Movimiento { get; set; }
    public required IReadOnlyList<KardexMovimientoLote> Lotes { get; set; }
}
