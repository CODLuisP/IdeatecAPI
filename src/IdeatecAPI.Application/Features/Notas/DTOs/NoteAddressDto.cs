namespace IdeatecAPI.Application.Features.Notas.DTOs;

// Reutilizado tanto para client como para company en el JSON
public class NoteAddressDto
{
    public string? Direccion { get; set; }
    public string? Provincia { get; set; }
    public string? Departamento { get; set; }
    public string? Distrito { get; set; }
    public string? Ubigueo { get; set; }
}