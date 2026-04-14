using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities;
public class SucursalProducto
{
    public int SucursalProductoId { get; set; }
    public int ProductoId { get; set; }
    public int SucursalId { get; set; }
    public string? NomSucursal { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public int? Stock { get; set; }
    public bool? Estado { get; set; }
    public DateTime? FechaCreacion { get; set; }
}