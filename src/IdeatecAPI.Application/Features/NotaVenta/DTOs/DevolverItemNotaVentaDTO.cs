namespace IdeatecAPI.Application.Features.NotaVenta.DTOs;

public class DevolverItemNotaVentaDTO
{
    public decimal Cantidad { get; set; }
    public string? Motivo { get; set; }
    public int? UsuarioId { get; set; }
}
