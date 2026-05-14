namespace IdeatecAPI.Application.Features.CuentasPorCobrar.DTOs;

public class ReporteCuentasPorCobrarFiltroDto
{
    public string EmpresaRuc { get; set; } = string.Empty;
    public string? EstablecimientoAnexo { get; set; }
    public string? ClienteNumDoc { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? Estado { get; set; }
    public string? TituloReporte { get; set; }
}