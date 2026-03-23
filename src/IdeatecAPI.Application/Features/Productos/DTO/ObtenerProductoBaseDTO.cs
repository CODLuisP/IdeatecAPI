using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.Productos.DTO;
public class ObtenerProductoBaseDTO
{
    public int ProductoId { get; set; }
    public string? Codigo { get; set; }
    public string? TipoProducto { get; set; }
    public string? CodigoSunat { get; set; }
    public string? NomProducto { get; set; }
    public string? UnidadMedida { get; set; }
    public string? TipoAfectacionIGV { get; set; }
    public bool? IncluirIGV { get; set; }
    public bool? Estado { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public ObtenerCategoriaDTO? Categoria { get; set; }
}
