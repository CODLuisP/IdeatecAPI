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
    public string? NomProducto { get; set; }
    public string? UnidadMedida { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public string? TipoAfectacionIGV { get; set; }
    public bool? IncluirIGV { get; set; }
    public decimal? Stock { get; set; }
    public bool? Estado { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public ObtenerCategoriaDTO? Categoria { get; set; }
    public ObtenerSucursalProducto? SucursalProducto { get; set; }
}

public class ObtenerSucursalProducto
{
    public int SucursalProductoId { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public int? Stock { get; set; }
}

public class ObtenerCategoriaDTO
{
    public int CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
}