using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.Sucursal.DTOs;
public class AgregarSucursalDTO
{
    public string? EmpresaRuc { get; set; }
    public string? CodEstablecimiento { get; set; }
    public string? SerieFactura { get; set; }
    public int CorrelativoFactura { get; set; } = 1;
    public string? SerieBoleta { get; set; }
    public int CorrelativoBoleta { get; set; } = 1;
    public string? SerieNotaCredito { get; set; }
    public int CorrelativoNotaCredito { get; set; } = 1;
    public string? SerieNotaDebito { get; set; }
    public int CorrelativoNotaDebito { get; set; } = 1;
    public string? SerieGuiaRemision { get; set; }
    public int CorrelativoGuiaRemision { get; set; } = 1;
    public string? SerieGuiaTransportista { get; set; }
    public int CorrelativoGuiaTransportista { get; set; } = 1;

    // ── Nuevos campos ──
    public string NombreSucursal { get; set; } = string.Empty;
    public string UsernameAdminSucursal { get; set; } = string.Empty;
    public string EmailAdmin { get; set; } = string.Empty;
    public string UsernameAdminActual { get; set; } = string.Empty; // para generar superadmin
}