namespace IdeatecAPI.Domain.Entities;
public class ComprobanteDetalle
{
    public int DetalleId { get; set; }
    public int ComprobanteId { get; set; }
    public int? Item { get; set; }
    public int? ProductoId { get; set; }
    public string? Codigo { get; set; }
    public string? Descripcion { get; set; }
    public decimal Cantidad { get; set; } // Cantidad de productos a vender   
    public string? UnidadMedida { get; set; }
    public decimal PrecioUnitario { get; set; } // precio unitario sin igv 
    public string? TipoAfectacionIGV { get; set; }
    public decimal? PorcentajeIGV { get; set; }
    public decimal? MontoIGV { get; set; }
    public decimal? BaseIgv { get; set; } // total de venta sin igv = cantidad * (precioventa - descuentounitario)
    public string? CodigoTipoDescuento { get; set; }
    public decimal? DescuentoUnitario { get; set; }
    public decimal? DescuentoTotal { get; set; }
    public decimal? ValorVenta { get; set; } // total venta sin igv = cantidad * precio unitario
    public decimal? PrecioVenta { get; set; } // precio unitario con igv = precio unitario + igv
    public decimal? TotalVentaItem { get; set; } // Total de venta incluido igv = cantidad por precio venta
    public decimal? Icbper { get; set; }
    public decimal? FactorIcbper { get; set; }
}

