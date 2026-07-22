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
    public string? TipoAfectacionIGV { get; set; }
    public bool? IncluirIGV { get; set; }
    public string? UrlImagenProducto { get; set; }
    public bool? Estado { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public string? CodigoBarras { get; set; }
    public bool? EsPaquete { get; set; }
    public int? ProductoBaseId { get; set; }
    public decimal? FactorConversion { get; set; }
    public ObtenerCategoriaDTO? Categoria { get; set; }
    public ObtenerSucursalProductoDTO? SucursalProducto { get; set; }
}

public class ObtenerSucursalProductoDTO
{
    public int SucursalProductoId { get; set; }
    public string? NomSucursal { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public int? Stock { get; set; }
    public decimal? UltimoPrecioCompra { get; set; }
    public DateTime? FechaUltimaCompra { get; set; }
    public decimal? PrecioMayorista { get; set; }
    public int? CantidadMinimaMayorista { get; set; }
    public bool? EnPromocion { get; set; }
    public decimal? PorcentajeDescuento { get; set; }
    public int? UsuarioId { get; set; }
    public string? UbicacionTienda { get; set; }
    public DateTime? ProximoVencimiento { get; set; }
}

public class ObtenerCategoriaDTO
{
    public int CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
}