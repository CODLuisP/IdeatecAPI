using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Categorias.DTOs;

namespace IdeatecAPI.Application.Features.Categorias.Services;

public interface ICategoriaService
{
    Task<IEnumerable<CategoriaDto>> GetAllCategoriasAsync();
    Task<CategoriaDto?> GetCategoriaByIdAsync(int id);
    Task<IEnumerable<CategoriaDto>> GetCategoriasByEmpresaRucAsync(string empresaRuc);
    Task<bool> RegistrarCategoriaAsync(RegistrarCategoriaDto categoria);
    Task<bool> EditarCategoriaAsync(EditarCategoriaDto categoria);
    Task<bool> EliminarCategoriaAsync(int CategoriaId);
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
            CategoriaId = c.CategoriaId,
            EmpresaRuc = c.EmpresaRuc,
            CategoriaNombre = c.CategoriaNombre,
            Descripcion = c.Descripcion,
            Estado = c.Estado
        });
    }

    public async Task<CategoriaDto?> GetCategoriaByIdAsync(int id)
    {
        var categoria = await _unitOfWork.Categorias.GetCategoriaByIdAsync(id);
        
        if (categoria == null)
            return null;

        return new CategoriaDto
        {
            CategoriaId = categoria.CategoriaId,
            EmpresaRuc = categoria.EmpresaRuc,
            CategoriaNombre = categoria.CategoriaNombre,
            Descripcion = categoria.Descripcion,
            Estado = categoria.Estado
        };
    }

    public async Task<IEnumerable<CategoriaDto>> GetCategoriasByEmpresaRucAsync(string empresaRuc)
    {
        var categorias = await _unitOfWork.Categorias.GetCategoriasByEmpresaRucAsync(empresaRuc);

        return categorias.Select(c => new CategoriaDto
        {
            CategoriaId = c.CategoriaId,
            EmpresaRuc = c.EmpresaRuc,
            CategoriaNombre = c.CategoriaNombre,
            Descripcion = c.Descripcion,
            Estado = c.Estado
        });
    }

    public async Task<bool> RegistrarCategoriaAsync(RegistrarCategoriaDto dto)
    {
        _unitOfWork.BeginTransaction();

        try
        {
            var categoria = new Domain.Entities.Categoria
            {
                EmpresaRuc = dto.EmpresaRuc,
                CategoriaNombre = dto.CategoriaNombre,
                Descripcion = dto.Descripcion,
            };

            var result = await _unitOfWork.Categorias.RegistrarCategoriaAsync(categoria);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarCategoriaAsync(EditarCategoriaDto dto)
    {
        if (dto.CategoriaId <= 0)
            throw new ArgumentException("CategoriaId inválido");

        _unitOfWork.BeginTransaction();

        try
        {
            var categoria = new Domain.Entities.Categoria
            {
                CategoriaId = dto.CategoriaId,
                EmpresaRuc = dto.EmpresaRuc,
                CategoriaNombre = dto.CategoriaNombre,
                Descripcion = dto.Descripcion,
            };

            var result = await _unitOfWork.Categorias.EditarCategoriaAsync(categoria);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EliminarCategoriaAsync(int categoriaId)
    {
        if (categoriaId <= 0)
            throw new ArgumentException("CategoriaId inválido");

        _unitOfWork.BeginTransaction();

        try
        {
            var result = await _unitOfWork.Categorias.EliminarCategoriaAsync(categoriaId);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

}