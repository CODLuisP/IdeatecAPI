namespace IdeatecAPI.Domain.Entities;
public class Producto
{
    public int ProductoId { get; set; }
    public string? Codigo { get; set; }
    public string? TipoProducto { get; set; }
    public string? CodigoSunat { get; set; }
    public string? NomProducto { get; set; }
    public string? UnidadMedida { get; set; }
    public string? TipoAfectacionIGV { get; set; }
    public bool? IncluirIGV { get; set; }
    public int? CategoriaId { get; set; }
    public string? UrlImagenProducto { get; set; }
    public bool? Estado { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public string? CodigoBarras { get; set; }
    public bool? EsPaquete { get; set; }
    public int? ProductoBaseId { get; set; }
    public decimal? FactorConversion { get; set; }
    public Categoria? Categoria { get; set; }
    public SucursalProducto? SucursalProducto { get; set; }
}