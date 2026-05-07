namespace IdeatecAPI.Application.Features.Reportes.DTOs;

public class MedioPagoTopDTO
{
    public string? MedioPago { get; set; }
    public int VecesUsado { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal PromedioMonto { get; set; }
}