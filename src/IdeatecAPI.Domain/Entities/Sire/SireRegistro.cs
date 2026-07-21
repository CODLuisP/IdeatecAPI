namespace IdeatecAPI.Domain.Entities;

public class SireRegistro
{
    public int Id { get; set; }
    public string RucEmpresa { get; set; } = string.Empty;
    public string PerTributario { get; set; } = string.Empty;
    public string? NumTicket { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
    public string? RespuestaSunat { get; set; }
    public string? Mensaje { get; set; }
    public DateTime? FechaConsulta { get; set; }
    public DateTime? FechaCierre { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}
