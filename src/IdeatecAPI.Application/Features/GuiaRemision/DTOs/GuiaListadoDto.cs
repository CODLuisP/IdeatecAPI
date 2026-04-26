public class GuiaListadoDto
{
    public int GuiaId { get; set; }
    public int? SucursalId { get; set; }
    public string TipoDoc { get; set; } = string.Empty;
    public string? NumeroCompleto { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime FechaCreacion { get; set; }

    // Destinatario
    public string? DestinatarioNumDoc { get; set; }
    public string? DestinatarioRznSocial { get; set; }

    // Puntos
    public string? PartidaDireccion { get; set; }
    public string? LlegadaDireccion { get; set; }

    // Transportista / placa (tabla transportista)
    public string? TransportistaRznSocial { get; set; }
    public string? TransportistaPlaca { get; set; }

    // Correo / WhatsApp
    public string? ClienteCorreo { get; set; }
    public bool EnviadoPorCorreo { get; set; }
    public string? ClienteWhatsapp { get; set; }
    public bool EnviadoPorWhatsapp { get; set; }

    // SUNAT
    public string EstadoSunat { get; set; } = string.Empty;
    public string? CodigoRespuestaSunat { get; set; }
    public string? MensajeRespuestaSunat { get; set; }
}