using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Productos.DTO;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Productos.Services;

public interface IProductoService
{
    Task<IEnumerable<ObtenerProductoDTO>> GetAllProductosAsync();
    Task<ObtenerProductoDTO?> GetProductoByIdAsync(int id);
    Task<bool> ExisteRucAsync(string codigo);
    Task<bool> RegistrarProductoAsync(RegistrarProductoDTO producto);
    Task<bool> EditarProductoAsync(EditarProductoDTO producto);
    Task<bool> EliminarProductoAsync(int productoId);
}

public class ProductoService : IProductoService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductoService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ObtenerProductoDTO>> GetAllProductosAsync()
    {
        var productos = await _unitOfWork.Productos.GetAllProductosAsync();
        return productos.Select(MapToDto);
    }

    public async Task<ObtenerProductoDTO?> GetProductoByIdAsync(int id)
    {
        var producto = await _unitOfWork.Productos.GetProductoByIdAsync(id);

        if (producto == null)
            return null;

        return MapToDto(producto);
    }

    public Task<bool> ExisteRucAsync(string codigo)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> RegistrarProductoAsync(RegistrarProductoDTO dto)
    {
        _unitOfWork.BeginTransaction();
        
         if (await _unitOfWork.Productos.ExisteProductoAsync(dto.Codigo))
            throw new InvalidOperationException($"Ya existe un producto con código {dto.Codigo}");
        try
        {
            var producto = new Producto
            {
                Codigo = dto.Codigo ?? string.Empty,
                TipoProducto = dto.TipoProducto ?? string.Empty,
                CodigoSunat = dto.CodigoSunat ?? string.Empty,
                Descripcion = dto.Descripcion ?? string.Empty,
                UnidadMedida = dto.UnidadMedida ?? string.Empty,
                PrecioUnitario = dto.PrecioUnitario ?? 0,
                TipoAfectacionIGV = dto.TipoAfectacionIGV ?? string.Empty,
                IncluirIGV = dto.IncluirIGV ?? false,
                Stock = dto.Stock ?? 0,
                CategoriaId = dto.CategoriaId ?? 0,
                Estado = true,
                FechaCreacion = DateTime.Now
            };

            var result = await _unitOfWork.Productos.RegistrarProductoAsync(producto);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarProductoAsync(EditarProductoDTO dto)
    {
        if (dto.ProductoId <= 0)
            throw new ArgumentException("ProductoId inválido");

        _unitOfWork.BeginTransaction();

        try
        {
            var producto = new Producto
            {
                ProductoId = dto.ProductoId,
                Codigo = dto.Codigo ?? string.Empty,
                TipoProducto = dto.TipoProducto ?? string.Empty,
                CodigoSunat = dto.CodigoSunat ?? string.Empty,
                Descripcion = dto.Descripcion ?? string.Empty,
                UnidadMedida = dto.UnidadMedida ?? string.Empty,
                PrecioUnitario = dto.PrecioUnitario ?? 0,
                TipoAfectacionIGV = dto.TipoAfectacionIGV ?? string.Empty,
                IncluirIGV = dto.IncluirIGV ?? false,
                Stock = dto.Stock ?? 0,
                CategoriaId = dto.CategoriaId ?? 0
            };

            var result = await _unitOfWork.Productos.EditarProductoAsync(producto);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EliminarProductoAsync(int productoId)
    {
        if (productoId <= 0)
            throw new ArgumentException("ProductoId inválido");

        _unitOfWork.BeginTransaction();

        try
        {
            var result = await _unitOfWork.Productos.EliminarProductoAsync(productoId);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    private static ObtenerProductoDTO MapToDto(Producto p)
    {
        return new ObtenerProductoDTO
        {
            ProductoId = p.ProductoId,
            Codigo = p.Codigo,
            TipoProducto = p.TipoProducto,
            CodigoSunat = p.CodigoSunat,
            Descripcion = p.Descripcion,
            UnidadMedida = p.UnidadMedida,
            PrecioUnitario = p.PrecioUnitario,
            TipoAfectacionIGV = p.TipoAfectacionIGV,
            IncluirIGV = p.IncluirIGV,
            Stock = p.Stock,
            Estado = p.Estado,
            FechaCreacion = p.FechaCreacion,
            Categoria = p.Categoria
        };
    }
}

