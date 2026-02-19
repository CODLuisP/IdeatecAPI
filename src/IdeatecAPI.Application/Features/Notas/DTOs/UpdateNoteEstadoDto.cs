namespace IdeatecAPI.Application.Features.Notas.DTOs;

// Para cuando SUNAT responde y solo actualizamos el estado
public class UpdateNoteEstadoDto
{
    public string Estado { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? Mensaje { get; set; }
    public string? Xml { get; set; }
    public string? Cdr { get; set; }
}