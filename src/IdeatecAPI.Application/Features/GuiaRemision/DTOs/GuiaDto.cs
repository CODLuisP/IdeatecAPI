namespace IdeatecAPI.Application.Features.GuiaRemision.DTOs;

public class GuiaDto
{
    public int GuiaId { get; set; }
    public int Version { get; set; }
    public string TipoDoc { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public int Correlativo { get; set; }
    public string? NumeroCompleto { get; set; }
    public DateTime FechaEmision { get; set; }

    public string? EmpresaRuc { get; set; }
    public string? EmpresaRazonSocial { get; set; }

    public string? DestinatarioTipoDoc { get; set; }
    public string? DestinatarioNumDoc { get; set; }
    public string? DestinatarioRznSocial { get; set; }

    public string? CodTraslado { get; set; }
    public string? DesTraslado { get; set; }
    public string? ModTraslado { get; set; }
    public DateTime? FecTraslado { get; set; }
    public decimal? PesoTotal { get; set; }
    public string? UndPesoTotal { get; set; }

    public string? LlegadaUbigeo { get; set; }
    public string? LlegadaDireccion { get; set; }
    public string? PartidaUbigeo { get; set; }
    public string? PartidaDireccion { get; set; }

    public string? ChoferTipoDoc { get; set; }
    public string? ChoferDoc { get; set; }
    public string? ChoferNombres { get; set; }    // ← nuevo
    public string? ChoferApellidos { get; set; }  // ← nuevo
    public string? ChoferLicencia { get; set; }   // ← nuevo

    public string? TransportistaNumDoc { get; set; }
    public string? TransportistaRznSocial { get; set; }
    public string? TransportistaPlaca { get; set; }

    public string EstadoSunat { get; set; } = string.Empty;
    public string? CodigoRespuestaSunat { get; set; }
    public string? MensajeRespuestaSunat { get; set; }
    public string? TicketSunat { get; set; }
    public DateTime? FechaEnvioSunat { get; set; }
    public DateTime FechaCreacion { get; set; }

    public List<GuiaDetalleDto> Details { get; set; } = new();
}

public class GuiaDetalleDto
{
    public int DetalleId { get; set; }
    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Codigo { get; set; }
}