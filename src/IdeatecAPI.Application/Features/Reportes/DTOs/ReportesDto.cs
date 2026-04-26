namespace IdeatecAPI.Application.Features.Reportes.DTOs;

// ── Request ──────────────────────────────────────────────────────────────────
public class ReporteRequestDto
{
    public string Periodo { get; set; } = "hoy"; // hoy | semana | mes | año | personalizado
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Limite { get; set; } = 10;
    public int? UsuarioId { get; set; }
}

// ── Response principal ────────────────────────────────────────────────────────
public class ReporteResponseDto
{
    public KpiDto Kpi { get; set; } = new();
    public List<GraficoBarraDto> Grafico { get; set; } = new();
    public DistribucionDocumentosDto Distribucion { get; set; } = new();
    public List<ClienteResumenDto> TopClientes { get; set; } = new();
    public TotalesClientesDto TotalesClientes { get; set; } = new();
}

// ── KPI Cards ────────────────────────────────────────────────────────────────
public class KpiDto
{
    // Período actual
    public decimal TotalVentas { get; set; }      // importeTotal facturas+boletas
    public decimal TotalIGV { get; set; }          // totalIGV facturas+boletas
    public int TotalDocumentos { get; set; }       // todos los tipos

    // Período anterior (para calcular tendencia en front)
    public decimal TotalVentasAnterior { get; set; }
    public decimal TotalIGVAnterior { get; set; }
    public int TotalDocumentosAnterior { get; set; }
}

// ── Gráfico de barras ─────────────────────────────────────────────────────────
public class GraficoBarraDto
{
    public string Etiqueta { get; set; } = string.Empty; // "Lun", "01", "Ene", etc.
    public decimal Ventas { get; set; }
    public decimal Igv { get; set; }
}

// ── Distribución donut ────────────────────────────────────────────────────────
public class DistribucionDocumentosDto
{
    public int Facturas { get; set; }
    public int Boletas { get; set; }
    public int NotasCredito { get; set; }
    public int NotasDebito { get; set; }
}

// ── Tabla clientes ────────────────────────────────────────────────────────────
public class ClienteResumenDto
{
    public string ClienteRznSocial { get; set; } = string.Empty;
    public string ClienteNumDoc { get; set; } = string.Empty;
    public int NumDocs { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Igv { get; set; }
    public decimal Total { get; set; }
}

public class TotalesClientesDto
{
    public int TotalDocs { get; set; }
    public decimal TotalSubtotal { get; set; }
    public decimal TotalIgv { get; set; }
    public decimal TotalGeneral { get; set; }
}

// ── Para exportar Excel (sin límite) ─────────────────────────────────────────
public class ClienteExportDto : ClienteResumenDto { }