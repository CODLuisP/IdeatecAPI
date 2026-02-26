namespace IdeatecAPI.Application.Features.Categorias.DTOs;

public class CategoriaDto
{
    public int CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public string? Descripcion { get; set; }
    public bool? Estado { get; set; }
}