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
}