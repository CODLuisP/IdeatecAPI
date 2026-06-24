using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Proveedor.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Proveedor.Services;

public interface ICompraProveedorService
{
    Task<IEnumerable<ObtenerCompraProveedorDTO>> GetAllBySucursalAsync(int sucursalId);
    Task<IEnumerable<ObtenerCompraProveedorDTO>> GetAllByProveedorAsync(int proveedorId);
    Task<IEnumerable<ObtenerCompraProveedorDTO>> GetByDocReferenciaAsync(string docReferencia, int sucursalId);
    Task<ObtenerCompraProveedorDTO> RegistrarAsync(RegistrarCompraProveedorDTO dto);
    Task<bool> EliminarAsync(int compraProveedorId);
}

public class CompraProveedorService : ICompraProveedorService
{
    private readonly IUnitOfWork _unitOfWork;

    public CompraProveedorService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ObtenerCompraProveedorDTO>> GetAllBySucursalAsync(int sucursalId)
    {
        var compras = await _unitOfWork.ComprasProveedor.GetAllBySucursalAsync(sucursalId);
        return compras.Select(MapToDTO);
    }

    public async Task<IEnumerable<ObtenerCompraProveedorDTO>> GetAllByProveedorAsync(int proveedorId)
    {
        var compras = await _unitOfWork.ComprasProveedor.GetAllByProveedorAsync(proveedorId);
        return compras.Select(MapToDTO);
    }

    public async Task<IEnumerable<ObtenerCompraProveedorDTO>> GetByDocReferenciaAsync(string docReferencia, int sucursalId)
    {
        var compras = await _unitOfWork.ComprasProveedor.GetByDocReferenciaAsync(docReferencia, sucursalId);
        return compras.Select(MapToDTO);
    }

    public async Task<ObtenerCompraProveedorDTO> RegistrarAsync(RegistrarCompraProveedorDTO dto)
    {
        if (dto.ProveedorId == null || dto.ProveedorId <= 0)
            throw new ArgumentException("ProveedorId es obligatorio");
        if (dto.SucursalId == null || dto.SucursalId <= 0)
            throw new ArgumentException("SucursalId es obligatorio");
        if (dto.ProductoId == null || dto.ProductoId <= 0)
            throw new ArgumentException("ProductoId es obligatorio");
        if (dto.Cantidad == null || dto.Cantidad <= 0)
            throw new ArgumentException("Cantidad debe ser mayor a 0");
        if (dto.PrecioCompra == null || dto.PrecioCompra < 0)
            throw new ArgumentException("PrecioCompra inválido");

        var existeEnSucursal = await _unitOfWork.Productos.ExisteEnSucursalAsync(dto.ProductoId.Value, dto.SucursalId.Value);
        if (!existeEnSucursal)
            throw new InvalidOperationException("El producto no está habilitado en esta sucursal.");

        _unitOfWork.BeginTransaction();
        try
        {
            var compra = new CompraProveedor
            {
                ProveedorId = dto.ProveedorId.Value,
                SucursalId = dto.SucursalId.Value,
                ProductoId = dto.ProductoId.Value,
                PrecioCompra = dto.PrecioCompra,
                Cantidad = dto.Cantidad,
                UnidadMedida = dto.UnidadMedida,
                DocReferencia = dto.DocReferencia,
                IdUsuario = dto.IdUsuario,
                FechaCreacion = DateTime.Now
            };

            var creada = await _unitOfWork.ComprasProveedor.RegistrarAsync(compra);

            await _unitOfWork.Productos.RegistrarCompraStockAsync(
                dto.ProductoId.Value,
                dto.SucursalId.Value,
                dto.Cantidad.Value,
                dto.PrecioCompra.Value);

            _unitOfWork.Commit();

            var creadaCompleta = await _unitOfWork.ComprasProveedor.GetByIdAsync(creada.CompraProveedorId);
            return MapToDTO(creadaCompleta!);
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EliminarAsync(int compraProveedorId)
    {
        if (compraProveedorId <= 0)
            throw new ArgumentException("CompraProveedorId inválido");

        _unitOfWork.BeginTransaction();
        try
        {
            var result = await _unitOfWork.ComprasProveedor.EliminarAsync(compraProveedorId);

            _unitOfWork.Commit();
            return result;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    private ObtenerCompraProveedorDTO MapToDTO(CompraProveedor c)
    {
        return new ObtenerCompraProveedorDTO
        {
            CompraProveedorId = c.CompraProveedorId,
            ProveedorId = c.ProveedorId,
            RazonSocialProveedor = c.RazonSocialProveedor,
            SucursalId = c.SucursalId,
            NomSucursal = c.NomSucursal,
            ProductoId = c.ProductoId,
            NomProducto = c.NomProducto,
            PrecioCompra = c.PrecioCompra,
            Cantidad = c.Cantidad,
            UnidadMedida = c.UnidadMedida,
            DocReferencia = c.DocReferencia,
            FechaCreacion = c.FechaCreacion
        };
    }
}
