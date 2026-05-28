// IdeatecAPI.Domain.Entities/PlantillaVelsat.cs
namespace IdeatecAPI.Domain.Entities;

public class PlantillaVelsat
{
    public int Id { get; set; }
    public string? Numdoc { get; set; }
    public string? RazonSocial { get; set; }
    public string? Periodo { get; set; }
    public string? Concepto { get; set; }
    public string? Moneda { get; set; }
    public double Importe { get; set; }
    public DateTime Fechaini { get; set; }
    public DateTime Fechafin { get; set; }
    public string? Placa { get; set; }
    public byte Estado { get; set; }
    public string? Correo { get; set; }
    public string? Whatsapp { get; set; }
}