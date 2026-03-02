
using IdeatecAPI.Application.Features.Notas.DTOs;
namespace IdeatecAPI.Application.Features.Comprobante.DTOs;

public class GenerarComprobanteDTO
{
    public string UblVersion { get; set; } = "2.1"; 
    public string TipoOperacion { get; set; } = "0101";
    public string? TipoComprobante { get; set; }
    public string Serie { get; set; } = string.Empty;
    public string Correlativo { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string TipoMoneda { get; set; } = "PEN";
    public string? TipoPago { get; set; } = "Contado";

    public ClienteDTO Cliente { get; set; } = new();
    public EmpresaDTO Company { get; set; } = new();

    // Totales
    public decimal MtoOperGravadas { get; set; }
    public decimal MtoOperExoneradas { get; set; }
    public decimal MtoOperInafectas { get; set; }
    public decimal MtoIGV { get; set; }
    public decimal TotalIcbper { get; set; } 
    public decimal TotalImpuestos { get; set; }
    public decimal ValorVenta { get; set; }  //Total antes de impuestos
    public decimal SubTotal { get; set; }
    public decimal MtoImpVenta { get; set; }
    public decimal TotalDescuentos { get; set; } = 0;
    public decimal TotalOtrosCargos { get; set; } = 0;
    public decimal MontoCredito { get; set; } = 0; 

    public List<DetalleFacturaDTO> Details { get; set; } = [];
    public List<DetallePagosDTO>? Pagos { get; set; } = [];
    public List<DetalleCuotasDTO>? Cuotas { get; set; } = [];
    public NoteLegendDto? Legends { get; set; }
}

public class DetallePagosDTO
{
    public int? ComprobanteId { get; set; }
    public string? MedioPago { get; set; }
    public decimal? Monto { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? EntidadFinanciera { get; set; }
    public string? Observaciones { get; set; }
}

public class DetalleCuotasDTO
{
    public int? ComprobanteId { get; set; }
    public string? NumeroCuota { get; set; }
    public decimal? Monto { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string? MontoPagado { get; set; }
    public DateTime? FechaPago { get; set; } 
    public string? Estado { get; set; }
}

public class EmpresaDTO
{
    public int EmpresaId { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? RazonSocial { get; set; }
    public string? NombreComercial { get; set; }
    public string? EstablecimientoAnexo { get; set; } = "0000"; 
    public string? Ubigeo { get; set; }
    public string? DireccionLineal { get; set; }
    public string? Departamento { get; set; }
    public string? Provincia { get; set; }
    public string? Distrito { get; set; }
}

public class ClienteDTO
{
    public int? ClienteId { get; set; }
    public string? TipoDocumento { get; set; } //Catalogo 06
    public string? NumeroDocumento { get; set; }
    public string? RazonSocial { get; set; }
    public string? Ubigeo { get; set; }
    public string? DireccionLineal { get; set; }
    public string? Departamento { get; set; }
    public string? Provincia { get; set; }
    public string? Distrito { get; set; }
}
public class DetalleFacturaDTO
{
    public int DetalleId { get; set; }
    public int ComprobanteId { get; set; }
    public int? Item { get; set; }
    public int? ProductoId { get; set; }
    public string? Codigo { get; set; }
    public string? Descripcion { get; set; }
    public decimal Cantidad { get; set; }
    public string? UnidadMedida { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string? TipoAfectacionIGV { get; set; }
    public decimal PorcentajeIGV { get; set; }
    public decimal MontoIGV { get; set; }
    public decimal BaseIgv { get; set; }
    public decimal DescuentoUnitario { get; set; }
    public decimal DescuentoTotal { get; set; }
    public decimal ValorVenta { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal Icbper { get; set; }
    public decimal FactorIcbper { get; set; }
}


