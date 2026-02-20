using IdeatecAPI.Application.Features.Notas.DTOs;

public class CreateNoteCompanyDto
{
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public NoteAddressDto? Address { get; set; }
}