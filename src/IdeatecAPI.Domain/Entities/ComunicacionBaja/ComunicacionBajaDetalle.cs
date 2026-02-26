namespace IdeatecAPI.Domain.Entities;

public class ComunicacionBajaDetalle
{
    public int DetalleId { get; set; }
    public int BajaId { get; set; }
    public string TipoDoc { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Correlativo { get; set; } = string.Empty;
    public string DesMotivoBaja { get; set; } = string.Empty;
}