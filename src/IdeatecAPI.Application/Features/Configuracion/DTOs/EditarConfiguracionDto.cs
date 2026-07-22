namespace IdeatecAPI.Application.Features.Configuracion.DTOs;

public class EditarConfiguracionDto
{
    public bool? IsImprime { get; set; }
    public string? TamañoImpresion { get; set; }
    public string? Igv { get; set; }
    public bool? IsConsumo { get; set; }
    public bool? GuiaRemision { get; set; }
    public bool? IsCredito { get; set; }
    public bool? ItemsDefecto { get; set; }
    public string? IsBoletaOrFactura { get; set; }
    public bool? IsEnvioResumen { get; set; }
    public bool? IsVale { get; set; }
    public bool? DeudasCobrar { get; set; }
    public bool? Trabajadores { get; set; }
    public bool? CargaComprobantes { get; set; }
    public bool? AfectacionIgv { get; set; }
    public bool? DescUnitario { get; set; }
    public bool? IsStock { get; set; }
    public int? UmbralStockBajo { get; set; }
    public bool? UseNotaVenta { get; set; }
    public bool? IsCajaAutopago { get; set; }
    public bool? UsaSire { get; set; }
}
