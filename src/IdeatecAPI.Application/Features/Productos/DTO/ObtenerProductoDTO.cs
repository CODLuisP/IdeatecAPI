using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Productos.DTO;
public class ObtenerProductoDTO
{
    public int ProductoId { get; set; }
    public string? Codigo { get; set; }
    public string? TipoProducto { get; set; }
    public string? CodigoSunat { get; set; }
    public string? Descripcion { get; set; }
    public string? UnidadMedida { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public string? TipoAfectacionIGV { get; set; }
    public bool? IncluirIGV { get; set; }
    public decimal? Stock { get; set; }
    public bool? Estado { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public Categoria? Categoria { get; set; }
}