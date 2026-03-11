namespace IdeatecAPI.Domain.Entities;

public class ComunicacionBaja
{
    public int BajaId { get; set; }
    public int EmpresaId { get; set; }
    public string Correlativo { get; set; } = string.Empty;
    public DateTime FecGeneracion { get; set; }
    public DateTime FecComunicacion { get; set; }
    public string? EmpresaRuc { get; set; }
    public string? EmpresaRazonSocial { get; set; }
    public string? EmpresaNombreComercial { get; set; }
    public string? EmpresaDireccion { get; set; }
    public string? EmpresaProvincia { get; set; }
    public string? EmpresaDepartamento { get; set; }
    public string? EmpresaDistrito { get; set; }
    public string? EmpresaUbigeo { get; set; }
    public string EstadoSunat { get; set; } = "PENDIENTE";
    public string? CodigoRespuestaSunat { get; set; }
    public string? MensajeRespuestaSunat { get; set; }
    public string? TicketSunat { get; set; }
    public string? XmlEnviado { get; set; }
    public string? CdrBase64 { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvioSunat { get; set; }
}