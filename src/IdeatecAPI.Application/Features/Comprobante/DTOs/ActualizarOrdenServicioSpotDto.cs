namespace IdeatecAPI.Application.Features.Comprobante.DTOs;

public class ActualizarOrdenServicioSpotDto
{
    public string? OrdenServicio { get; set; }
    public bool? Spot { get; set; }

    // Contenido editable del recuadro SPOT (null = no tocar, "" = ocultar la línea en el PDF)
    public string? SpotLeyenda { get; set; }
    public string? SpotBienServicio { get; set; }
    public string? SpotMedioPago { get; set; }
    public string? SpotCuentaBanco { get; set; }
    public decimal? SpotPorcentaje { get; set; }
}
