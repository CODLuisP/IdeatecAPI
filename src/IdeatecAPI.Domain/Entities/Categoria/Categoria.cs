namespace IdeatecAPI.Domain.Entities;

public class Categoria
{
    public int CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public string? Descripcion { get; set; }
    public bool? Estado { get; set; }
}