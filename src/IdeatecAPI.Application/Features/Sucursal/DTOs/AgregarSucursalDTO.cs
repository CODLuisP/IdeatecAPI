namespace IdeatecAPI.Application.Features.Sucursal.DTOs;
public class AgregarSucursalDTO
{
    public string? EmpresaRuc { get; set; }
    public string? CodEstablecimiento { get; set; }
    public string? Nombre { get; set; }
    public string? SerieFactura { get; set; }
    public int CorrelativoFactura { get; set; } = 1;
    public string? SerieBoleta { get; set; }
    public int CorrelativoBoleta { get; set; } = 1;
    public string? SerieNotaCreditoFactura { get; set; }
    public int CorrelativoNotaCreditoFactura { get; set; } = 1;
    public string? SerieNotaCreditoBoleta { get; set; }
    public int CorrelativoNotaCreditoBoleta { get; set; } = 1;
    public string? SerieNotaDebitoFactura { get; set; }
    public int CorrelativoNotaDebitoFactura { get; set; } = 1;
    public string? SerieNotaDebitoBoleta { get; set; }
    public int CorrelativoNotaDebitoBoleta { get; set; } = 1;
    public string? SerieGuiaRemision { get; set; }
    public int CorrelativoGuiaRemision { get; set; } = 1;
    public string? SerieGuiaTransportista { get; set; }
    public int CorrelativoGuiaTransportista { get; set; } = 1;

    // ── Nuevos campos ──
    public string NombreSucursal { get; set; } = string.Empty;
    public string? Direccion { get; set; }

    public string UsernameAdminSucursal { get; set; } = string.Empty;
    public string EmailAdmin { get; set; } = string.Empty;
    public string UsernameAdminActual { get; set; } = string.Empty; // para generar superadmin
}