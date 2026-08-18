namespace IdeatecAPI.Application.Features.Comprobante.DTOs;

// Resultado de reservar el siguiente numero de una serie: identifica la sucursal y
// devuelve la serie y el correlativo que le tocan a esta venta.
public class AsignacionSerieDTO
{
    public int SucursalId { get; set; }
    public string? Serie { get; set; }
    public int Correlativo { get; set; }
}
