namespace IdeatecAPI.Application.Features.Productos.DTO;

public class ReporteProductoFiltroDTO
{
    public string EmpresaRuc { get; set; } = string.Empty;

    public int? SucursalId { get; set; }

    public int? CategoriaId { get; set; }

    public string? IgvTipo { get; set; }

    public string? TipoProducto { get; set; }

    public string? StockFiltro { get; set; }

    public int? StockValor { get; set; }

    public string? TituloReporte { get; set; }
}

public class ReporteProductoItemDTO
{
    public string? Codigo { get; set; }
    public string? NomProducto { get; set; }
    public string? CategoriaNombre { get; set; }
    public string? TipoProducto { get; set; }
    public string? UnidadMedida { get; set; }
    public string? TipoAfectacionIGV { get; set; }
    public bool? IncluirIGV { get; set; }
    public string? NomSucursal { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public int? Stock { get; set; }
}