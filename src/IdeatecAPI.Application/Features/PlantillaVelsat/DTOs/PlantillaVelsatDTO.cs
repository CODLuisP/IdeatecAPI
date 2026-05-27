// IdeatecAPI.Application.Features.PlantillaVelsat/DTOs/PlantillaVelsatDTO.cs
namespace IdeatecAPI.Application.Features.PlantillaVelsat.DTOs;

public class ObtenerPlantillaVelsatDTO
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
}

public class CrearPlantillaVelsatDTO
{
    public string? Numdoc { get; set; }
    public string? RazonSocial { get; set; }
    public string? Periodo { get; set; }
    public string? Concepto { get; set; }
    public string? Moneda { get; set; }
    public double Importe { get; set; }
    public DateTime Fechaini { get; set; }
    public DateTime Fechafin { get; set; }
    public string? Placa { get; set; }
}

public class EditarPlantillaVelsatDTO
{
    public string? Numdoc { get; set; }
    public string? RazonSocial { get; set; }
    public string? Periodo { get; set; }
    public string? Concepto { get; set; }
    public double? Importe { get; set; }
    public DateTime? Fechaini { get; set; }
    public DateTime? Fechafin { get; set; }
    public string? Placa { get; set; }
    public string? Moneda { get; set; }
}