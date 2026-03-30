using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Productos.DTO;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Productos.Services;

public interface IProductoService
{
    Task<IEnumerable<ObtenerProductoDTO>> GetAllProductosAsync(int sucursalId); //Producto completo por sucursal
    Task<IEnumerable<ObtenerProductoBaseDTO>> GetAllProductosBaseRucAsync(string empresaRuc); //Proucto base por empresa
    Task<IEnumerable<ObtenerProductoDTO>> GetAllProductosRucAsync(string empresaRuc); // Producto completo por empresa
    Task<IEnumerable<ObtenerProductoBaseDTO>> GetProductosRucDisponiblesAsync(int sucursalId); // Productos base disponibles de la misma empresa por agregar a la sede
    Task<ObtenerProductoDTO?> GetProductoByIdAsync(int productoId, int sucursalId); //Producto especifico por sucursal
    Task<ObtenerProductoDTO> RegistrarProductoAsync(RegistrarProductoDTO dto);
    Task<bool> EditarProductoAsync(EditarProductoDTO dto);
    Task<bool> ActualizarStockAsync(IEnumerable<ActualizarStockDTO> dtos);

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
        _unitOfWork.BeginTransaction();
        try
        {
            int productoId;

            var productoExistente = await _unitOfWork.Productos.ObtenerProductoPorCodigoAsync(dto.Codigo);

            if (productoExistente != null)
            {
                // El producto ya existe, solo agrega a la sucursal
                productoId = productoExistente.ProductoId;
            }
            else
            {
                // El producto no existe, lo crea
                var producto = new Producto
                {
                    Codigo            = dto.Codigo,
                    TipoProducto      = dto.TipoProducto,
                    CodigoSunat       = dto.CodigoSunat,
                    NomProducto       = dto.NomProducto,
                    UnidadMedida      = dto.UnidadMedida,
                    TipoAfectacionIGV = dto.TipoAfectacionIGV,
                    IncluirIGV        = dto.IncluirIGV,
                    CategoriaId       = dto.CategoriaId
                };

                var productoCreado = await _unitOfWork.Productos.RegistrarProductoAsync(producto);
                productoId = productoCreado.ProductoId;
            }

            // Verifica que el producto no esté ya en esa sucursal
            var existeEnSucursal = await _unitOfWork.Productos.ExisteEnSucursalAsync(productoId, dto.SucursalId);
            if (existeEnSucursal)
                throw new InvalidOperationException($"El producto con código '{dto.Codigo}' ya existe en esta sucursal.");

            var sucursalProducto = new SucursalProducto
            {
                ProductoId     = productoId,
                SucursalId     = dto.SucursalId,
                PrecioUnitario = dto.PrecioUnitario,
                Stock          = dto.Stock,
                Estado         = true,
                FechaCreacion  = DateTime.Now
            };

            await _unitOfWork.Productos.RegistrarSucursalProductoAsync(sucursalProducto);

            _unitOfWork.Commit();

            var productoCompleto = await _unitOfWork.Productos.GetProductoByIdAsync(productoId, dto.SucursalId);
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

    public async Task<bool> ActualizarStockAsync(IEnumerable<ActualizarStockDTO> dtos)
    {
        if (!dtos.Any())
            throw new ArgumentException("La lista no puede estar vacía.");

        if (dtos.Any(d => d.Cantidad <= 0))
            throw new ArgumentException("Todas las cantidades deben ser mayores a 0.");

        _unitOfWork.BeginTransaction();
        try
        {
            foreach (var dto in dtos)
            {
                var resultado = await _unitOfWork.Productos.ActualizarStockAsync(dto.SucursalProductoId, dto.Cantidad);
                if (!resultado)
                    throw new InvalidOperationException($"Stock insuficiente o producto no encontrado para SucursalProductoId {dto.SucursalProductoId}.");
            }

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

    public async Task<IEnumerable<ObtenerProductoBaseDTO>> GetAllProductosBaseRucAsync(string empresaRuc)
    {
        var productos = await _unitOfWork.Productos.GetAllProductosBaseRucAsync(empresaRuc);
        return productos.Select(MapToBaseDto);
    }
    public async Task<IEnumerable<ObtenerProductoDTO>> GetAllProductosRucAsync(string empresaRuc)
    {
        var productos = await _unitOfWork.Productos.GetAllProductosRucAsync(empresaRuc);
        return productos.Select(MapToDto);
    }


    public async Task<IEnumerable<ObtenerProductoBaseDTO>> GetProductosRucDisponiblesAsync(int sucursalId)
    {
        var productos = await _unitOfWork.Productos.GetProductosRucDisponiblesAsync(sucursalId);
        return productos.Select(MapToBaseDto);
    }

    private static ObtenerProductoBaseDTO MapToBaseDto(Producto p) => new ObtenerProductoBaseDTO
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
            CategoriaId     = p.Categoria.CategoriaId,
            CategoriaNombre = p.Categoria.CategoriaNombre,
        }
    };
}