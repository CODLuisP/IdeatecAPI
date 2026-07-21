namespace IdeatecAPI.Application.Features.Sire.DTOs;

public class SirePeriodoDto
{
    public string? Periodo { get; set; }
    public string? Estado { get; set; }
    public string? Descripcion { get; set; }
}

public class SirePeriodosResponse
{
    public bool Success { get; set; }
    public string? Mensaje { get; set; }
    public List<SirePeriodoDto> Periodos { get; set; } = new();
    public string? RespuestaCruda { get; set; }
}

public class SireAceptarPropuestaResponse
{
    public bool Success { get; set; }
    public string? NumTicket { get; set; }
    public string? Mensaje { get; set; }
    public string? RespuestaCruda { get; set; }
}

public class SireRegistrarPreliminarResponse
{
    public bool Success { get; set; }
    public string? Mensaje { get; set; }
    public string? RespuestaCruda { get; set; }
}

public class SireComprobanteDto
{
    public string? RucEmisor { get; set; }
    public string? RazonSocialEmisor { get; set; }
    public string? Periodo { get; set; }
    public string? CarSunat { get; set; }
    public string? Correlativo { get; set; }
    public string? FechaEmision { get; set; }
    public string? TipoComprobante { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public string? TipoDocCliente { get; set; }
    public string? NumDocCliente { get; set; }
    public string? RazonSocialCliente { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal Igv { get; set; }
    public decimal ImporteTotal { get; set; }
    public bool Activo { get; set; }
    public decimal? TipoCambio { get; set; }
    public string? CodMoneda { get; set; }
    public string? Inconsistencias { get; set; }
}

public class SireDescargarPropuestaResponse
{
    public bool Success { get; set; }
    public string? Mensaje { get; set; }
    public string? NumTicket { get; set; }
    public List<SireComprobanteDto> Comprobantes { get; set; } = new();
}
