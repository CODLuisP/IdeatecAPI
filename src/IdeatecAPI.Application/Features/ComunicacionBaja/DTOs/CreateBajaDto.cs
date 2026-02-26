using IdeatecAPI.Application.Features.Notas.DTOs;

namespace IdeatecAPI.Application.Features.ComunicacionBaja.DTOs;

public class CreateBajaDto
{
    public string Correlativo { get; set; } = string.Empty;
    public DateTime FecGeneracion { get; set; }
    public DateTime FecComunicacion { get; set; }
    public CreateNoteCompanyDto Company { get; set; } = new();
    public List<CreateBajaDetalleDto> Details { get; set; } = new();
}