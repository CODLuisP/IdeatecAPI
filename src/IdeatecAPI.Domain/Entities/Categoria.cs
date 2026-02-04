namespace IdeatecAPI.Domain.Entities;

public class Categoria
{
    public int Id { get; set; }
    public string NombreCategoria { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string ImagenCatalogId { get; set; } = string.Empty;
    public int IsContinue { get; set; }
}