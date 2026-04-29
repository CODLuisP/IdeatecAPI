namespace IdeatecAPI.Application.Features.Categorias.DTOs;

public class EditarCategoriaDto
{
    public int CategoriaId { get; set; }
    public string? EmpresaRuc { get; set; }
    public string? CategoriaNombre { get; set; }
    public string? Descripcion { get; set; }
}