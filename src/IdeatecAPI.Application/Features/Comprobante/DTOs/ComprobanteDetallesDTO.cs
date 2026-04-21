using IdeatecAPI.Application.Features.Detraccion.DTOs;
using IdeatecAPI.Application.Features.Notas.DTOs;

namespace IdeatecAPI.Application.Features.Comprobante.DTOs;
public class ComprobanteDetallesDTO
{
    public int ComprobanteId { get; set; }
    public List<DetalleFacturaDTO> Details { get; set; } = [];
    public List<DetallePagosDTO> Pagos { get; set; } = [];
    public List<DetalleCuotasDTO> Cuotas { get; set; } = [];
    public List<NoteLegendDto> Legends { get; set; } = [];
    public List<GuiaComprobanteDTO> Guias { get; set; } = [];
    public List<DetraccionDTO> Detracciones { get; set; } = [];
}