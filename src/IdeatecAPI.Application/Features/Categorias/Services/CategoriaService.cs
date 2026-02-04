using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Categorias.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Categorias.Services;

public interface ICategoriaService
{
    Task<IEnumerable<CategoriaDto>> GetAllCategoriasAsync();
    Task<CategoriaDto?> GetCategoriaByIdAsync(int id);
}

public class CategoriaService : ICategoriaService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoriaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CategoriaDto>> GetAllCategoriasAsync()
    {
        var categorias = await _unitOfWork.Categorias.GetAllCategoriasAsync();
        

        return categorias.Select(c => new CategoriaDto
        {
            Id = c.Id,
            NombreCategoria = c.NombreCategoria,
            Descripcion = c.Descripcion,
            ImagenCatalogId = c.ImagenCatalogId,
            IsContinue = c.IsContinue
        });
    }

    public async Task<CategoriaDto?> GetCategoriaByIdAsync(int id)
    {
        var categoria = await _unitOfWork.Categorias.GetCategoriaByIdAsync(id);
        
        if (categoria == null)
            return null;

        return new CategoriaDto
        {
            Id = categoria.Id,
            NombreCategoria = categoria.NombreCategoria,
            Descripcion = categoria.Descripcion,
            ImagenCatalogId = categoria.ImagenCatalogId,
            IsContinue = categoria.IsContinue
        };
    }
}