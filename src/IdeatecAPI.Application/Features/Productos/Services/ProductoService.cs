using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Productos.DTO;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Productos.Services;

public interface IProductoService
{
    Task<IEnumerable<ObtenerProductoDTO>> GetAllProductosAsync(int sucursalId);
    Task<ObtenerProductoDTO?> GetProductoByIdAsync(int productoId, int sucursalId);
    Task<ObtenerProductoDTO> RegistrarProductoAsync(RegistrarProductoDTO dto);
    Task<bool> EditarProductoAsync(EditarProductoDTO dto);
    Task<bool> EliminarSucursalProductoAsync(int sucursalProductoId);
}

public class ProductoService : IProductoService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductoService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ObtenerProductoDTO>> GetAllProductosAsync(int sucursalId)
    {
        var productos = await _unitOfWork.Productos.GetAllProductosAsync(sucursalId);
        return productos.Select(MapToDto);
    }

    public async Task<ObtenerProductoDTO?> GetProductoByIdAsync(int productoId, int sucursalId)
    {
        var producto = await _unitOfWork.Productos.GetProductoByIdAsync(productoId, sucursalId);

        if (producto == null)
            return null;

        return MapToDto(producto);
    }

    public async Task<ObtenerProductoDTO> RegistrarProductoAsync(RegistrarProductoDTO dto)
    {
        if (await _unitOfWork.Productos.ExisteProductoAsync(dto.Codigo))
            throw new InvalidOperationException($"Ya existe un producto con código '{dto.Codigo}'.");

        _unitOfWork.BeginTransaction();
        try
        {
            var producto = new Producto
            {
                Codigo             = dto.Codigo,
                TipoProducto       = dto.TipoProducto,
                CodigoSunat        = dto.CodigoSunat,
                NomProducto        = dto.NomProducto,
                UnidadMedida       = dto.UnidadMedida,
                TipoAfectacionIGV  = dto.TipoAfectacionIGV,
                IncluirIGV         = dto.IncluirIGV,
                CategoriaId        = dto.CategoriaId,
                Estado             = true,
                FechaCreacion      = DateTime.Now
            };

            var productoCreado = await _unitOfWork.Productos.RegistrarProductoAsync(producto);

            var sucursalProducto = new SucursalProducto
            {
                ProductoId     = productoCreado.ProductoId,
                SucursalId     = dto.SucursalId,
                PrecioUnitario = dto.PrecioUnitario,
                Stock          = dto.Stock,
                Estado         = true,
                FechaCreacion  = DateTime.Now
            };

            await _unitOfWork.Productos.RegistrarSucursalProductoAsync(sucursalProducto);

            _unitOfWork.Commit();

            var productoCompleto = await _unitOfWork.Productos.GetProductoByIdAsync(productoCreado.ProductoId, dto.SucursalId);
            return MapToDto(productoCompleto!);
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
            throw new ArgumentException("ProductoId inválido.");

        _unitOfWork.BeginTransaction();
        try
        {
            var producto = new Producto
            {
                ProductoId        = dto.ProductoId,
                Codigo            = dto.Codigo,
                TipoProducto      = dto.TipoProducto,
                CodigoSunat       = dto.CodigoSunat,
                NomProducto       = dto.NomProducto,
                UnidadMedida      = dto.UnidadMedida,
                TipoAfectacionIGV = dto.TipoAfectacionIGV,
                IncluirIGV        = dto.IncluirIGV,
                CategoriaId       = dto.CategoriaId
            };

            await _unitOfWork.Productos.EditarProductoAsync(producto);

            var sucursalProducto = new SucursalProducto
            {
                SucursalProductoId = dto.SucursalProductoId,
                PrecioUnitario     = dto.PrecioUnitario,
                Stock              = dto.Stock
            };

            await _unitOfWork.Productos.EditarSucursalProductoAsync(sucursalProducto);

            _unitOfWork.Commit();
            return true;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EliminarSucursalProductoAsync(int sucursalProductoId)
    {
        if (sucursalProductoId <= 0)
            throw new ArgumentException("SucursalProductoId inválido.");

        return await _unitOfWork.Productos.EliminarSucursalProductoAsync(sucursalProductoId);
    }

    private static ObtenerProductoDTO MapToDto(Producto p) => new ObtenerProductoDTO
    {
        ProductoId        = p.ProductoId,
        Codigo            = p.Codigo,
        TipoProducto      = p.TipoProducto,
        CodigoSunat       = p.CodigoSunat,
        NomProducto       = p.NomProducto,
        UnidadMedida      = p.UnidadMedida,
        TipoAfectacionIGV = p.TipoAfectacionIGV,
        IncluirIGV        = p.IncluirIGV,
        Estado            = p.Estado,
        FechaCreacion     = p.FechaCreacion,
        Categoria         = p.Categoria == null ? null : new ObtenerCategoriaDTO
        {
            CategoriaId    = p.Categoria.CategoriaId,
            CategoriaNombre = p.Categoria.CategoriaNombre,
        },
        SucursalProducto = p.SucursalProducto == null ? null : new ObtenerSucursalProductoDTO
        {
            SucursalProductoId = p.SucursalProducto.SucursalProductoId,
            PrecioUnitario     = p.SucursalProducto.PrecioUnitario,
            Stock              = p.SucursalProducto.Stock
        }
    };
}