using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.Productos.DTO;

public class RegistrarProductoDTO
{
    public string Codigo { get; set; } = string.Empty;
    public string? TipoProducto { get; set; }
    public string? CodigoSunat { get; set; }
    public string? NomProducto { get; set; }
    public string? UnidadMedida { get; set; }
    public string? TipoAfectacionIGV { get; set; }
    public bool? IncluirIGV { get; set; }
    public int? CategoriaId { get; set; }
    public string? UrlImagenProducto { get; set; }
    public string? CodigoBarras { get; set; }
    public bool? EsPaquete { get; set; }
    public int? ProductoBaseId { get; set; }
    public decimal? FactorConversion { get; set; }

    // Datos iniciales de precio y stock por sucursal
    public int SucursalId { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal? Stock { get; set; }
    // Costo de compra del stock inicial: si se informa junto con Stock, se registra
    // como lote PEPS (origen SALDO_INICIAL) para que kardex/valorizado/rentabilidad lo reconozcan.
    public decimal? CostoUnitario { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public decimal? PrecioMayorista { get; set; }
    public int? CantidadMinimaMayorista { get; set; }
    public bool? EnPromocion { get; set; }
    public decimal? PorcentajeDescuento { get; set; }

    // Datos informativos opcionales
    public int? UsuarioId { get; set; }
    public string? UbicacionTienda { get; set; }

    // Alertas configurables por producto (null = queda como alerta activa por defecto).
    public bool? AlertaVencimientoActiva { get; set; }
    public bool? AlertaStockBajoActiva { get; set; }
    public decimal? StockMinimoAlerta { get; set; }
}
