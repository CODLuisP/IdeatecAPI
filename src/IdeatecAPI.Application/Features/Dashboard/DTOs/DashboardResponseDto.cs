namespace IdeatecAPI.Application.Features.Dashboard.DTOs;

public class DashboardResponseDto
{
    // ── Ventas ────────────────────────────────────────────────────────────────
    public decimal VentasDelDia { get; set; }              // 01 + 03 del día
    public decimal VentasNetas { get; set; }               // VentasDelDia + NDDelDia - NCDelDia

    // ── Conteos ───────────────────────────────────────────────────────────────
    public int FacturasEmitidas { get; set; }
    public int BoletasEmitidas { get; set; }
    public int NotasCreditoEmitidas { get; set; }
    public int NotasDebitoEmitidas { get; set; }
    public int NotasVentaEmitidas { get; set; }

    // ── Notas de Venta (NV) ───────────────────────────────────────────────────
    public decimal TotalNotasVentaDelDia { get; set; }

    // ── Notas de Crédito (07) ─────────────────────────────────────────────────
    public decimal TotalNotasCreditoDelDia { get; set; }       // doc afectado es de hoy
    public decimal TotalNotasCreditoOtrasFechas { get; set; }  // doc afectado es de otro día

    // ── Notas de Débito (08) ──────────────────────────────────────────────────
    public decimal TotalNotasDebitoDelDia { get; set; }        // doc afectado es de hoy
    public decimal TotalNotasDebitoOtrasFechas { get; set; }   // doc afectado es de otro día

    // ── Listas ────────────────────────────────────────────────────────────────
    public List<RendimientoVentasDto> RendimientoVentas { get; set; } = new();
    public List<ComprobanteRecienteDto> ComprobantesRecientes { get; set; } = new();
}

public class RendimientoVentasDto
{
    public DateTime Fecha { get; set; }
    public decimal TotalVentas { get; set; }
}

public class ComprobanteRecienteDto
{
    public int ComprobanteID { get; set; }
    public string NumeroCompleto { get; set; } = string.Empty;
    public string TipoComprobante { get; set; } = string.Empty;
    public string ClienteRznSocial { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; }
    public decimal ImporteTotal { get; set; }
    public string EstadoSunat { get; set; } = string.Empty;
}