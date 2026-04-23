using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.Dashboard.DTOs;

    public class DashboardResponseDto
{
    public decimal VentasDelDia { get; set; }
    public int FacturasEmitidas { get; set; }
    public int BoletasEmitidas { get; set; }
    public int NotasCreditoEmitidas { get; set; }
    public int NotasDebitoEmitidas { get; set; }
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
