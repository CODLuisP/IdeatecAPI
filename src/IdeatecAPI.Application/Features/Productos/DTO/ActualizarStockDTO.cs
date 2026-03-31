using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.Productos.DTO;

public class ActualizarStockDTO
{
    public int SucursalProductoId { get; set; }
    public int Cantidad { get; set; }
}