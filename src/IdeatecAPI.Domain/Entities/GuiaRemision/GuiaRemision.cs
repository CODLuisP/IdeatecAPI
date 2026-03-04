namespace IdeatecAPI.Domain.Entities;

public class GuiaRemision
{
    public int GuiaId { get; set; }
    public int EmpresaId { get; set; }
    public int Version { get; set; } = 2022;
    public string TipoDoc { get; set; } = "09";
    public string Serie { get; set; } = string.Empty;
    public int Correlativo { get; set; }
    public string? NumeroCompleto { get; set; }
    public DateTime FechaEmision { get; set; }

    // Empresa denormalizada
    public string? EmpresaRuc { get; set; }
    public string? EmpresaRazonSocial { get; set; }
    public string? EmpresaNombreComercial { get; set; }
    public string? EmpresaDireccion { get; set; }
    public string? EmpresaProvincia { get; set; }
    public string? EmpresaDepartamento { get; set; }
    public string? EmpresaDistrito { get; set; }
    public string? EmpresaUbigeo { get; set; }

    // Destinatario
    public string? DestinatarioTipoDoc { get; set; }
    public string? DestinatarioNumDoc { get; set; }
    public string? DestinatarioRznSocial { get; set; }

    // Tercero (opcional)
    public string? TerceroTipoDoc { get; set; }
    public string? TerceroNumDoc { get; set; }
    public string? TerceroRznSocial { get; set; }

    // Observación y docs relacionados
    public string? Observacion { get; set; }
    public string? DocBajaTipoDoc { get; set; }
    public string? DocBajaNroDoc { get; set; }
    public string? RelDocTipoDoc { get; set; }
    public string? RelDocNroDoc { get; set; }

    // Envío
    public string? CodTraslado { get; set; }
    public string? DesTraslado { get; set; }
    public string? ModTraslado { get; set; }
    public DateTime? FecTraslado { get; set; }
    public string? CodPuerto { get; set; }
    public bool IndTransbordo { get; set; } = false;
    public decimal? PesoTotal { get; set; }
    public string? UndPesoTotal { get; set; }
    public string? NumContenedor { get; set; }

    // Llegada
    public string? LlegadaUbigeo { get; set; }
    public string? LlegadaDireccion { get; set; }

    // Partida
    public string? PartidaUbigeo { get; set; }
    public string? PartidaDireccion { get; set; }

    // Transportista
    public string? TransportistaTipoDoc { get; set; }
    public string? TransportistaNumDoc { get; set; }
    public string? TransportistaRznSocial { get; set; }
    public string? TransportistaPlaca { get; set; }
    public string? ChoferTipoDoc { get; set; }
    public string? ChoferDoc { get; set; }
    public string? ChoferNombres { get; set; }
    public string? ChoferApellidos { get; set; }
    public string? ChoferLicencia { get; set; }

    // Estado SUNAT
    public string EstadoSunat { get; set; } = "PENDIENTE";
    public string? CodigoRespuestaSunat { get; set; }
    public string? MensajeRespuestaSunat { get; set; }
    public string? TicketSunat { get; set; }
    public string? CdrBase64 { get; set; }
    public DateTime? FechaEnvioSunat { get; set; }

    // Auditoría
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}