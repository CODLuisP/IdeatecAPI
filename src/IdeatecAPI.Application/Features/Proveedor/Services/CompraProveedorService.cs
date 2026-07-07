using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Inventario.Services;
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
    private readonly IInventarioPepsService _inventarioPepsService;

    public CompraProveedorService(IUnitOfWork unitOfWork, IInventarioPepsService inventarioPepsService)
    {
        _unitOfWork = unitOfWork;
        _inventarioPepsService = inventarioPepsService;
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

        // Si el producto comprado es un paquete (caja, pack, etc.), la CANTIDAD se redirige al
        // producto base (cantidad x factor de conversión), pero el COSTO (ultimoPrecioCompra)
        // se registra en el paquete mismo, tal cual se pagó por él — los costos de cada producto
        // se mantienen independientes, sin mezclarse entre el paquete y su producto base.
        var producto = await _unitOfWork.Productos.GetProductoByIdAsync(dto.ProductoId.Value, dto.SucursalId.Value);

        var esPaqueteValido = producto?.EsPaquete == true
            && producto.ProductoBaseId is int productoBaseIdTmp
            && producto.FactorConversion is decimal factorTmp
            && factorTmp > 0;

        int? productoBaseId = null;
        int cantidadStockBase = 0;

        if (esPaqueteValido)
        {
            productoBaseId = producto!.ProductoBaseId!.Value;
            var factor = producto.FactorConversion!.Value;

            var baseExisteEnSucursal = await _unitOfWork.Productos.ExisteEnSucursalAsync(productoBaseId.Value, dto.SucursalId.Value);
            if (!baseExisteEnSucursal)
                throw new InvalidOperationException("El producto base de este paquete no está habilitado en esta sucursal.");

            cantidadStockBase = (int)Math.Round(dto.Cantidad.Value * factor, MidpointRounding.AwayFromZero);
        }

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

            if (productoBaseId is int baseId)
            {
                // Paquete: el stock sube en el producto base, el costo se queda en el paquete.
                await _unitOfWork.Productos.IncrementarStockSinCostoAsync(baseId, dto.SucursalId.Value, cantidadStockBase);
                await _unitOfWork.Productos.ActualizarCostoSinStockAsync(dto.ProductoId.Value, dto.SucursalId.Value, dto.PrecioCompra.Value);

                // El lote PEPS se registra en el producto base (que es quien tiene el stock real),
                // con el costo unitario convertido a la unidad base (precio del paquete / factor).
                var factorLote = producto!.FactorConversion!.Value;
                var productoBase = await _unitOfWork.Productos.GetProductoByIdAsync(baseId, dto.SucursalId.Value);
                if (productoBase?.SucursalProducto != null)
                {
                    await _inventarioPepsService.RegistrarEntradaLoteAsync(
                        productoBase.SucursalProducto.SucursalProductoId,
                        creada.CompraProveedorId,
                        "COMPRA",
                        cantidadStockBase,
                        dto.PrecioCompra.Value / factorLote,
                        compra.FechaCreacion!.Value,
                        dto.IdUsuario,
                        referenciaTipo: "COMPRAPROVEEDOR",
                        referenciaId: creada.CompraProveedorId);
                }
            }
            else
            {
                // Producto normal: stock y costo juntos, en su propia fila.
                await _unitOfWork.Productos.RegistrarCompraStockAsync(
                    dto.ProductoId.Value,
                    dto.SucursalId.Value,
                    dto.Cantidad.Value,
                    dto.PrecioCompra.Value);

                if (producto?.SucursalProducto != null)
                {
                    await _inventarioPepsService.RegistrarEntradaLoteAsync(
                        producto.SucursalProducto.SucursalProductoId,
                        creada.CompraProveedorId,
                        "COMPRA",
                        dto.Cantidad.Value,
                        dto.PrecioCompra.Value,
                        compra.FechaCreacion!.Value,
                        dto.IdUsuario,
                        referenciaTipo: "COMPRAPROVEEDOR",
                        referenciaId: creada.CompraProveedorId);
                }
            }

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
