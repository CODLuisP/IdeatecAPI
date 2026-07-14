using IdeatecAPI.Application.Features.Comprobante.DTOs;

namespace IdeatecAPI.Application.Features.NotaVenta.DTOs;

public class GenerarNotaVentaDTO
{
    public int SucursalId { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string TipoMoneda { get; set; } = "PEN";
    public decimal? TipoCambio { get; set; }
    public string TipoPago { get; set; } = "Contado";
    public string? Observaciones { get; set; }
    public int? UsuarioCreacion { get; set; }

    public ClienteDTO? Cliente { get; set; }
    public EmpresaDTO Company { get; set; } = new();

    // Totales calculados por el frontend
    public decimal DescuentoGlobal { get; set; }
    public decimal TotalDescuentos { get; set; }
    public decimal TotalIGV { get; set; }
    public decimal ValorVenta { get; set; }
    public decimal SubTotal { get; set; }
    public decimal ImporteTotal { get; set; }
    public decimal MontoCredito { get; set; }

    public List<DetalleNotaVentaDTO> Detalles { get; set; } = [];
    public List<DetallePagosDTO>? Pagos { get; set; } = [];
    public List<DetalleCuotasDTO>? Cuotas { get; set; } = [];
}

public class DetalleNotaVentaDTO
{
    public int? TrabajadorId { get; set; }
    public int? Item { get; set; }
    public int? ProductoId { get; set; }
    public string? Codigo { get; set; }
    public string? Descripcion { get; set; }
    public decimal Cantidad { get; set; }
    public string? UnidadMedida { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal? DescuentoUnitario { get; set; }
    public decimal? DescuentoTotal { get; set; }
    public decimal? MontoIGV { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal TotalVentaItem { get; set; }
}
