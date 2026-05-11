namespace IdeatecAPI.Application.Features.Trabajadores.DTOs;

public class ReporteTrabajadorDTO
{
    public int TrabajadorId { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    public string? Dni { get; set; }

    // Totales calculados
    public int TotalComprobantes { get; set; }      // comprobantes distintos en los que participó
    public int TotalServicios { get; set; }          // filas de detalle (uno por servicio)
    public decimal TotalMonto { get; set; }          // suma de totalVentaItem

    public List<ServicioTrabajadorDTO> Servicios { get; set; } = [];
}

public class ServicioTrabajadorDTO
{
    // Comprobante
    public int ComprobanteId { get; set; }
    public string? NumeroCompleto { get; set; }
    public string? TipoComprobante { get; set; }
    public DateTime FechaEmision { get; set; }
    public string? TipoMoneda { get; set; }
    public string? EstadoSunat { get; set; }

    // Cliente
    public string? ClienteNumDoc { get; set; }
    public string? ClienteRazonSocial { get; set; }

    // Detalle del servicio
    public int DetalleId { get; set; }
    public string? Codigo { get; set; }
    public string? Descripcion { get; set; }
    public decimal Cantidad { get; set; }
    public string? UnidadMedida { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal TotalVentaItem { get; set; }
}

public class RankingTrabajadorDTO
{
    public int TrabajadorId { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? NombreCompleto => $"{Nombres} {Apellidos}".Trim();
    public string? Dni { get; set; }
    public int TotalComprobantes { get; set; }
    public int TotalServicios { get; set; }
    public decimal TotalMonto { get; set; }
}

public class ServicioTopDTO
{
    public string? Descripcion { get; set; }
    public string? Codigo { get; set; }
    public int TotalVeces { get; set; }
    public decimal TotalCantidad { get; set; }
    public decimal TotalMonto { get; set; }
    public decimal PrecioPromedio { get; set; }
}

public class ReporteServicioRawDTO
{
    public int TrabajadorId { get; set; }
    public string? Nombres { get; set; }
    public string? Apellidos { get; set; }
    public string? Dni { get; set; }

    public int ComprobanteId { get; set; }
    public string? NumeroCompleto { get; set; }
    public string? TipoComprobante { get; set; }
    public DateTime FechaEmision { get; set; }
    public string? TipoMoneda { get; set; }
    public string? EstadoSunat { get; set; }

    public string? ClienteNumDoc { get; set; }
    public string? ClienteRazonSocial { get; set; }

    public int DetalleId { get; set; }
    public string? Codigo { get; set; }
    public string? Descripcion { get; set; }
    public decimal Cantidad { get; set; }
    public string? UnidadMedida { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal TotalVentaItem { get; set; }
}