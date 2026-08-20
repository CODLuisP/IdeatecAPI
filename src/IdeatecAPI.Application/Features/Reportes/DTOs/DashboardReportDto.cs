namespace IdeatecAPI.Application.Features.Reportes.DTOs;

// ── Request ──────────────────────────────────────────────────────────────────
public class DashboardReportRequestDto
{
    public string Ruc { get; set; } = string.Empty;
    public string? CodEstablecimiento { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    public int? UsuarioCreacion { get; set; }
    public int TopLimit { get; set; } = 10;
}

// ── Response principal ───────────────────────────────────────────────────────
public class DashboardReportDto
{
    // Cabecera
    public string EmpresaRazonSocial { get; set; } = string.Empty;
    public string EmpresaRuc { get; set; } = string.Empty;
    public string? NombreSucursal { get; set; }
    public string? NombreUsuario { get; set; }
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public DateTime FechaGeneracion { get; set; } = DateTime.Now;

    // KPIs principales
    public decimal VentasTributarias { get; set; }   // facturas + boletas (sin NV ni NC/ND)
    public decimal VentasTotal { get; set; }          // facturas + boletas + NV ± NC/ND
    public decimal TotalIGV { get; set; }
    public int TotalDocumentos { get; set; }
    public decimal TotalNotasVenta { get; set; }
    public int CantidadNotasVenta { get; set; }
    public decimal TotalComisionPOS { get; set; }

    // Rentabilidad
    public decimal Ganancias { get; set; }
    public decimal PorcentajeGanancias { get; set; }

    // Tendencia vs período anterior
    public decimal VentasTributariasAnterior { get; set; }
    public decimal TotalIGVAnterior { get; set; }
    public int TotalDocumentosAnterior { get; set; }

    // Totales tributarios separados
    public TotalesTributariosDto TotalesTributarios { get; set; } = new();

    // Desglose NC/ND
    public int CantidadNC { get; set; }
    public int CantidadND { get; set; }
    public decimal TotalNC { get; set; }
    public decimal TotalND { get; set; }

    // Distribución documentos (para donut)
    public DistribucionDocumentosDto Distribucion { get; set; } = new();

    // Gráfico barras
    public List<GraficoBarraDto> Grafico { get; set; } = new();

    // Tops
    public List<ComprobanteTopDto> TopComprobantes { get; set; } = new();
    public List<ComprobanteTopDto> TopNotasVenta { get; set; } = new();
    public List<ComprobanteTopDto> TopComisionTarjeta { get; set; } = new();
    public List<ClienteResumenDto> TopClientes { get; set; } = new();
    public List<ProductoTopDTO> TopProductos { get; set; } = new();
    public List<MedioPagoTopDTO> MediosPago { get; set; } = new();

    // Ventas por usuario (solo cuando el reporte es de un único día)
    public List<VentasPorUsuarioDto> VentasPorUsuario { get; set; } = new();
}

// ── Ventas por usuario ───────────────────────────────────────────────────────
public class VentasPorUsuarioDto
{
    public string NombreUsuario { get; set; } = string.Empty;
    public int TotalDocumentos { get; set; }
    public decimal TotalVentas { get; set; }
}

// ── Totales tributarios separados ────────────────────────────────────────────
public class TotalesTributariosDto
{
    public int CantidadFacturas { get; set; }
    public decimal TotalFacturas { get; set; }
    public decimal IgvFacturas { get; set; }

    public int CantidadBoletas { get; set; }
    public decimal TotalBoletas { get; set; }
    public decimal IgvBoletas { get; set; }

    public decimal VentasNetas { get; set; }

    // NC/ND totales (monto)
    public decimal TotalNC { get; set; }
    public decimal TotalND { get; set; }
}

// ── Ganancias consolidadas del período ──────────────────────────────────────
public class GananciasDashboardDto
{
    public decimal IngresoVentas { get; set; }
    public decimal CostoVentas { get; set; }
}

// ── Top comprobante genérico ─────────────────────────────────────────────────
public class ComprobanteTopDto
{
    public string NumeroCompleto { get; set; } = string.Empty;
    public string TipoComprobante { get; set; } = string.Empty;
    public string ClienteRznSocial { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public decimal ImporteTotal { get; set; }
    public decimal ComisionTarjeta { get; set; }
    public string? MedioPago { get; set; }
}
