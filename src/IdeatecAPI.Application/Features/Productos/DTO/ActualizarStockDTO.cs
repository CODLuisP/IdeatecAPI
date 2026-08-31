using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.Productos.DTO;

public class ActualizarStockDTO
{
    public int SucursalProductoId { get; set; }
    // Decimal para admitir venta por peso/volumen (1.3 kg, 0.5 L, 2.75 m).
    public decimal Cantidad { get; set; }
    public string? ReferenciaTipo { get; set; }
    public int? ReferenciaId { get; set; }
    // Número de línea (comprobantedetalle.item) del comprobante al que corresponde este
    // ítem de stock. Se usa para resolver ComprobanteDetalleId una vez insertado el
    // comprobante, y así poder atar el movimiento de kardex a su línea de venta exacta.
    public int? Item { get; set; }
    public int? ComprobanteDetalleId { get; set; }
}