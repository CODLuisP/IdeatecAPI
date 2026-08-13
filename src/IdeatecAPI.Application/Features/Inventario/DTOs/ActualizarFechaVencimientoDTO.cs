namespace IdeatecAPI.Application.Features.Inventario.DTOs;

public class ActualizarFechaVencimientoDTO
{
    public DateTime? FechaVencimiento { get; set; }

    // Confirma el cambio aunque el lote tenga ventas parciales (ver ActualizarFechaVencimientoResultDTO).
    public bool Confirmar { get; set; } = false;
}

public class ActualizarFechaVencimientoResultDTO
{
    public bool Encontrado { get; set; } = true;
    public bool Actualizado { get; set; }
    public bool RequiereConfirmacion { get; set; }
    public decimal? CantidadVendida { get; set; }
    public decimal? CantidadOriginal { get; set; }
    public decimal? SaldoCantidad { get; set; }
}
