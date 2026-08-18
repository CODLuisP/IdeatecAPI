using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Inventario.DTOs;
using IdeatecAPI.Application.Features.Inventario.Services;
using IdeatecAPI.Application.Features.Productos.DTO;
using IdeatecAPI.Domain.Entities;
using ClosedXML.Excel;

namespace IdeatecAPI.Application.Features.Productos.Services;

public interface IProductoService
{
    Task<IEnumerable<ObtenerProductoDTO>> GetAllProductosAsync(int sucursalId); //Producto completo por sucursal
    Task<IEnumerable<ObtenerProductoBaseDTO>> GetAllProductosBaseRucAsync(string empresaRuc); //Proucto base por empresa
    Task<IEnumerable<ObtenerProductoDTO>> GetAllProductosRucAsync(string empresaRuc); // Producto completo por empresa
    Task<IEnumerable<ObtenerProductoBaseDTO>> GetProductosRucDisponiblesAsync(int sucursalId); // Productos base disponibles de la misma empresa por agregar a la sede
    Task<IEnumerable<ObtenerProductoBaseDTO>> SearchProductosRucDisponiblesAsync(int sucursalId, string palabra); // topa 10 segun palaba de Productos base disponibles de la misma empresa por agregar a la sede
    Task<ObtenerProductoDTO?> GetProductoByIdAsync(int productoId, int sucursalId); //Producto especifico por sucursal
    Task<IEnumerable<ObtenerProductoDTO>> SearchBySucursalAsync(int sucursalId, string palabra);
    Task<IEnumerable<ObtenerProductoDTO>> SearchByRucAsync(string empresaRuc, string palabra);
    Task<ObtenerProductoDTO> RegistrarProductoAsync(RegistrarProductoDTO dto);
    Task<bool> EditarProductoAsync(EditarProductoDTO dto);
    Task<bool> ActualizarStockAsync(IEnumerable<ActualizarStockDTO> dtos, string tipoMovimiento = "SALIDA_VENTA");
    // Descuenta stock reutilizando una transacción YA abierta por el llamador
    // (no hace Begin/Commit/Rollback). Permite que la creación de la venta y el
    // descuento de stock ocurran de forma atómica: si no hay stock, el llamador
    // hace rollback y la venta completa no se registra.
    Task DescontarStockEnTransaccionAsync(IEnumerable<ActualizarStockDTO> dtos, string tipoMovimiento = "SALIDA_VENTA");
    Task<bool> DevolverStockAsync(IEnumerable<DevolverStockDTO> dtos);
    // Revierte TODO el stock que se descontó para una referencia (ej. un comprobante anulado),
    // reutilizando una transacción YA abierta por el llamador (no hace Begin/Commit/Rollback).
    // A diferencia de DevolverStockAsync (que requiere que el llamador ya sepa qué productos
    // devolver), esta consulta el Kardex por referencia y revierte exactamente lo que salió,
    // sin importar si hubo redirección a producto base por paquete.
    Task RevertirStockPorReferenciaEnTransaccionAsync(string referenciaTipo, int referenciaId);
    Task<bool> EliminarSucursalProductoAsync(int sucursalProductoId);
    Task<byte[]> GenerarReporteExcelAsync(ReporteProductoFiltroDTO filtro);
}

public class ProductoService : IProductoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductoExcelService _excelService;
    private readonly IInventarioPepsService _inventarioPepsService;

    public ProductoService(IUnitOfWork unitOfWork, IProductoExcelService excelService, IInventarioPepsService inventarioPepsService)
    {
        _unitOfWork = unitOfWork;
        _excelService = excelService;
        _inventarioPepsService = inventarioPepsService;
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
        // Si no se exige, el stock inicial termina generando un lote PEPS con costo 0 (ver más abajo),
        // que después queda invisible para el backfill de saldo-inicial y arrastra costo 0 en
        // kardex/valorizado/rentabilidad hasta que alguien lo corrija a mano.
        if (dto.Stock is decimal stockInicialValidar && stockInicialValidar > 0
            && (dto.CostoUnitario is null || dto.CostoUnitario <= 0))
        {
            throw new ArgumentException("Debes registrar el costo de compra cuando informas stock inicial del producto.");
        }

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
                // Valida unicidad de código de barras antes de crear
                if (!string.IsNullOrWhiteSpace(dto.CodigoBarras))
                {
                    var barrasOcupado = await _unitOfWork.Productos.ExisteCodigoBarrasAsync(dto.CodigoBarras, dto.SucursalId);
                    if (barrasOcupado)
                        throw new InvalidOperationException($"El código de barras '{dto.CodigoBarras}' ya está asignado a otro producto.");
                }

                // El producto no existe, lo crea
                var producto = new Producto
                {
                    Codigo = dto.Codigo,
                    TipoProducto = dto.TipoProducto,
                    CodigoSunat = dto.CodigoSunat,
                    NomProducto = dto.NomProducto,
                    UnidadMedida = dto.UnidadMedida,
                    TipoAfectacionIGV = dto.TipoAfectacionIGV,
                    IncluirIGV = dto.IncluirIGV,
                    CategoriaId = dto.CategoriaId,
                    UrlImagenProducto = dto.UrlImagenProducto,
                    CodigoBarras = dto.CodigoBarras,
                    EsPaquete = dto.EsPaquete,
                    ProductoBaseId = dto.ProductoBaseId,
                    FactorConversion = dto.FactorConversion
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
                ProductoId = productoId,
                SucursalId = dto.SucursalId,
                PrecioUnitario = dto.PrecioUnitario,
                Stock = dto.Stock,
                PrecioMayorista = dto.PrecioMayorista,
                CantidadMinimaMayorista = dto.CantidadMinimaMayorista,
                EnPromocion = dto.EnPromocion ?? false,
                PorcentajeDescuento = dto.PorcentajeDescuento,
                UsuarioId = dto.UsuarioId,
                UbicacionTienda = dto.UbicacionTienda,
                AlertaVencimientoActiva = dto.AlertaVencimientoActiva,
                AlertaStockBajoActiva = dto.AlertaStockBajoActiva,
                StockMinimoAlerta = dto.StockMinimoAlerta,
                Estado = true,
                FechaCreacion = DateTime.Now
            };

            await _unitOfWork.Productos.RegistrarSucursalProductoAsync(sucursalProducto);

            // Si se informó stock inicial, se registra también como lote PEPS (origen SALDO_INICIAL)
            // para que kardex, stock valorizado y rentabilidad lo reconozcan desde la creación.
            // RegistrarSaldoInicialAsync omite duplicados por SucursalProductoId, así que es seguro
            // si luego alguien vuelve a correr el backfill manual de saldo inicial.
            if (dto.Stock is decimal stockInicial && stockInicial > 0)
            {
                await _inventarioPepsService.RegistrarEntradaLoteAsync(
                    sucursalProducto.SucursalProductoId,
                    compraProveedorId: null,
                    origen: "SALDO_INICIAL",
                    cantidad: stockInicial,
                    costoUnitario: dto.CostoUnitario ?? 0m,
                    fecha: DateTime.Now,
                    idUsuario: dto.UsuarioId,
                    fechaVencimiento: dto.FechaVencimiento);
            }

            // El "último precio de compra" se persiste también en la creación, igual que en
            // la edición, para que quede visible/editable de forma independiente al lote PEPS.
            if (dto.CostoUnitario is decimal costoUnitario)
            {
                await _unitOfWork.Productos.ActualizarCostoSinStockAsync(productoId, dto.SucursalId, costoUnitario);
            }

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

    public async Task<IEnumerable<ObtenerProductoDTO>> SearchBySucursalAsync(int sucursalId, string palabra)
    {
        if (string.IsNullOrWhiteSpace(palabra))
            throw new ArgumentException("La palabra de búsqueda no puede estar vacía.");

        var productos = await _unitOfWork.Productos.SearchBySucursalAsync(sucursalId, palabra);
        return productos.Select(MapToDto);
    }

    public async Task<IEnumerable<ObtenerProductoDTO>> SearchByRucAsync(string empresaRuc, string palabra)
    {
        if (string.IsNullOrWhiteSpace(palabra))
            throw new ArgumentException("La palabra de búsqueda no puede estar vacía.");

        var productos = await _unitOfWork.Productos.SearchByRucAsync(empresaRuc, palabra);
        return productos.Select(MapToDto);
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
                ProductoId = dto.ProductoId,
                Codigo = dto.Codigo,
                TipoProducto = dto.TipoProducto,
                CodigoSunat = dto.CodigoSunat,
                NomProducto = dto.NomProducto,
                UnidadMedida = dto.UnidadMedida,
                TipoAfectacionIGV = dto.TipoAfectacionIGV,
                IncluirIGV = dto.IncluirIGV,
                CategoriaId = dto.CategoriaId,
                UrlImagenProducto = dto.UrlImagenProducto,
                CodigoBarras = dto.CodigoBarras,
                EsPaquete = dto.EsPaquete,
                ProductoBaseId = dto.ProductoBaseId,
                FactorConversion = dto.FactorConversion
            };

            await _unitOfWork.Productos.EditarProductoAsync(producto);

            // Se resuelve el SucursalId una sola vez si hace falta (para el ajuste de stock
            // y/o para persistir el costo), evitando una segunda consulta redundante.
            int? sucursalId = null;
            if (dto.Stock is decimal || dto.CostoUnitario is decimal)
            {
                var info = await _unitOfWork.Productos.GetInfoConversionBySucursalProductoIdAsync(dto.SucursalProductoId);
                sucursalId = info?.SucursalProducto?.SucursalId;

                // Si el stock cambió, la diferencia se registra como movimiento PEPS (ajuste)
                // para que kardex/valorizado/rentabilidad reflejen el nuevo stock, en vez de
                // solo sobrescribir el número en sucursalproducto.
                if (dto.Stock is decimal stockNuevo)
                {
                    // Si el producto editado es un paquete (caja, pack, etc.), el valor de Stock
                    // representa la cantidad de cajas deseada (siempre entera), y el ajuste real
                    // de inventario/PEPS se redirige al producto base (cantidad x factor de
                    // conversión), igual que ya hacen las ventas, devoluciones y compras. Las
                    // unidades sueltas que ya tuviera el base (resto de la división) se preservan.
                    var esPaqueteValido = info?.EsPaquete == true
                        && info.ProductoBaseId is int
                        && info.FactorConversion is decimal factorTmp
                        && factorTmp > 0;

                    if (esPaqueteValido && sucursalId is int sucIdPaquete)
                    {
                        if (stockNuevo < 0 || stockNuevo != Math.Floor(stockNuevo))
                            throw new ArgumentException("La cantidad de cajas/paquetes debe ser un número entero mayor o igual a 0.");

                        var productoBaseId = info!.ProductoBaseId!.Value;
                        var factor = info.FactorConversion!.Value;

                        var productoBase = await _unitOfWork.Productos.GetProductoByIdAsync(productoBaseId, sucIdPaquete);
                        if (productoBase?.SucursalProducto == null)
                            throw new InvalidOperationException("El producto base de este paquete no está registrado en la sucursal.");

                        var baseSucursalProductoId = productoBase.SucursalProducto.SucursalProductoId;
                        var stockBaseActual = productoBase.SucursalProducto.Stock ?? 0m;
                        var sueltas = stockBaseActual % factor;
                        var nuevoStockBase = stockNuevo * factor + sueltas;
                        var deltaBase = nuevoStockBase - stockBaseActual;

                        if (deltaBase != 0 && (dto.CostoUnitario is null || dto.CostoUnitario <= 0))
                            throw new ArgumentException("Debes registrar el costo de compra para modificar el stock del producto.");

                        if (deltaBase > 0)
                        {
                            // RegistrarEntradaLoteAsync solo maneja lotes/kardex (no toca la
                            // columna stock): hay que incrementarla aparte, igual que en compras.
                            await _unitOfWork.Productos.IncrementarStockSinCostoAsync(productoBaseId, sucIdPaquete, deltaBase);
                            await _inventarioPepsService.RegistrarEntradaLoteAsync(
                                baseSucursalProductoId,
                                compraProveedorId: null,
                                origen: "AJUSTE_INVENTARIO",
                                cantidad: deltaBase,
                                costoUnitario: (dto.CostoUnitario ?? 0m) / factor,
                                fecha: DateTime.Now,
                                idUsuario: dto.UsuarioId);
                        }
                        else if (deltaBase < 0)
                        {
                            var descontado = await _unitOfWork.Productos.DescontarStockBaseAsync(productoBaseId, sucIdPaquete, Math.Abs(deltaBase));
                            if (!descontado)
                                throw new InvalidOperationException("Stock insuficiente en el producto base para reducir el stock del paquete.");

                            await _inventarioPepsService.ConsumirFifoAsync(
                                baseSucursalProductoId,
                                Math.Abs(deltaBase),
                                "SALIDA_AJUSTE",
                                referenciaTipo: null,
                                referenciaId: null,
                                idUsuario: dto.UsuarioId);
                        }
                    }
                    else
                    {
                        var stockActual = info?.SucursalProducto?.Stock ?? 0m;
                        var delta = stockNuevo - stockActual;

                        // Cualquier cambio de stock (subir o bajar) exige tener costo registrado:
                        // subir sin costo generaría un lote PEPS a costo 0, y bajar sin costo indica
                        // que el producto todavía no tiene su costo corregido (arrastraría movimientos
                        // de Kardex a costo 0 desde los lotes existentes).
                        if (delta != 0 && (dto.CostoUnitario is null || dto.CostoUnitario <= 0))
                            throw new ArgumentException("Debes registrar el costo de compra para modificar el stock del producto.");

                        if (delta > 0)
                        {
                            await _inventarioPepsService.RegistrarEntradaLoteAsync(
                                dto.SucursalProductoId,
                                compraProveedorId: null,
                                origen: "AJUSTE_INVENTARIO",
                                cantidad: delta,
                                costoUnitario: dto.CostoUnitario ?? 0m,
                                fecha: DateTime.Now,
                                idUsuario: dto.UsuarioId);
                        }
                        else if (delta < 0)
                        {
                            await _inventarioPepsService.ConsumirFifoAsync(
                                dto.SucursalProductoId,
                                Math.Abs(delta),
                                "SALIDA_AJUSTE",
                                referenciaTipo: null,
                                referenciaId: null,
                                idUsuario: dto.UsuarioId);
                        }
                    }
                }
            }

            var sucursalProducto = new SucursalProducto
            {
                SucursalProductoId = dto.SucursalProductoId,
                PrecioUnitario = dto.PrecioUnitario,
                Stock = dto.Stock,
                PrecioMayorista = dto.PrecioMayorista,
                CantidadMinimaMayorista = dto.CantidadMinimaMayorista,
                EnPromocion = dto.EnPromocion ?? false,
                PorcentajeDescuento = dto.PorcentajeDescuento,
                UsuarioId = dto.UsuarioId,
                UbicacionTienda = dto.UbicacionTienda,
                AlertaVencimientoActiva = dto.AlertaVencimientoActiva,
                AlertaStockBajoActiva = dto.AlertaStockBajoActiva,
                StockMinimoAlerta = dto.StockMinimoAlerta
            };

            await _unitOfWork.Productos.EditarSucursalProductoAsync(sucursalProducto);

            // El "último precio de compra" se persiste siempre que el usuario lo informe en el
            // modal, aunque el stock no suba: así el campo queda visible/editable de forma
            // independiente al lote PEPS (que solo se genera cuando efectivamente entra stock).
            if (dto.CostoUnitario is decimal costoUnitario && sucursalId is int sucId)
            {
                await _unitOfWork.Productos.ActualizarCostoSinStockAsync(dto.ProductoId, sucId, costoUnitario);
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

    public async Task<bool> ActualizarStockAsync(IEnumerable<ActualizarStockDTO> dtos, string tipoMovimiento = "SALIDA_VENTA")
    {
        _unitOfWork.BeginTransaction();
        try
        {
            await DescontarStockEnTransaccionAsync(dtos, tipoMovimiento);
            _unitOfWork.Commit();
            return true;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // Igual que ActualizarStockAsync pero SIN abrir/confirmar transacción: corre
    // dentro de la que ya abrió el llamador (creación de nota de venta / comprobante),
    // para que la venta y el descuento de stock sean atómicos.
    public async Task DescontarStockEnTransaccionAsync(IEnumerable<ActualizarStockDTO> dtos, string tipoMovimiento = "SALIDA_VENTA")
    {
        var items = dtos.ToList();

        if (items.Count == 0)
            throw new ArgumentException("La lista no puede estar vacía.");

        if (items.Any(d => d.Cantidad <= 0))
            throw new ArgumentException("Todas las cantidades deben ser mayores a 0.");

        // Una sola consulta resuelve, para todos los productos de la venta, si son paquete y
        // cual es el sucursalProducto base al que hay que redirigir el descuento.
        var infoConversion = (await _unitOfWork.Productos.GetInfoConversionBySucursalProductoIdsAsync(
                items.Select(i => i.SucursalProductoId)))
            .ToDictionary(i => i.SucursalProductoId);

        var consumos = new List<ConsumoPepsRequestDTO>(items.Count);
        var descuentos = new Dictionary<int, decimal>();
        // Destino -> sucursalProductoID del paquete que lo origino, solo para poder dar el
        // mismo mensaje de error de antes cuando falla el stock del producto base.
        var paquetesPorDestino = new Dictionary<int, int>();

        foreach (var dto in items)
        {
            infoConversion.TryGetValue(dto.SucursalProductoId, out var info);

            // Si el producto vendido es un paquete (caja, pack, etc.), el descuento de stock
            // se redirige al producto base (cantidad x factor de conversión), igual que en compras.
            var esPaqueteValido = info?.EsPaquete == true
                && info.ProductoBaseId is int
                && info.FactorConversion is decimal factorTmp
                && factorTmp > 0;

            int sucursalProductoDestino;
            decimal cantidadDestino;

            if (esPaqueteValido)
            {
                var factor = info!.FactorConversion!.Value;
                cantidadDestino = (int)Math.Round(dto.Cantidad * factor, MidpointRounding.AwayFromZero);

                sucursalProductoDestino = info.BaseSucursalProductoId
                    ?? throw new InvalidOperationException(
                        $"Stock insuficiente en el producto base para cubrir la venta del paquete (SucursalProductoId {dto.SucursalProductoId}).");

                paquetesPorDestino.TryAdd(sucursalProductoDestino, dto.SucursalProductoId);
            }
            else
            {
                sucursalProductoDestino = dto.SucursalProductoId;
                cantidadDestino = dto.Cantidad;
            }

            // Si el mismo producto aparece en varias lineas, se descuenta una sola vez por el total.
            descuentos[sucursalProductoDestino] = descuentos.GetValueOrDefault(sucursalProductoDestino) + cantidadDestino;

            // El consumo PEPS ocurre sobre los lotes del destino, que en el caso del paquete
            // es el producto BASE (quien tiene costo y lotes reales).
            consumos.Add(new ConsumoPepsRequestDTO
            {
                SucursalProductoId = sucursalProductoDestino,
                Cantidad = cantidadDestino,
                TipoMovimiento = tipoMovimiento,
                ReferenciaTipo = dto.ReferenciaTipo,
                ReferenciaId = dto.ReferenciaId
            });
        }

        await DescontarStockDeSucursalAsync(descuentos, paquetesPorDestino);

        await _inventarioPepsService.ConsumirFifoBatchAsync(consumos, idUsuario: null);
    }

    // Descuenta el stock de toda la venta en dos viajes en vez de un UPDATE por linea: primero
    // lee y bloquea las filas implicadas, valida la disponibilidad en memoria (para poder decir
    // exactamente que producto falta) y luego aplica todas las restas de una sola vez.
    private async Task DescontarStockDeSucursalAsync(
        Dictionary<int, decimal> descuentos,
        Dictionary<int, int> paquetesPorDestino)
    {
        var stockActual = (await _unitOfWork.Productos.GetStockParaDescontarAsync(descuentos.Keys))
            .ToDictionary(s => s.SucursalProductoId, s => s.Stock);

        foreach (var (sucursalProductoId, cantidad) in descuentos)
        {
            var disponible = stockActual.TryGetValue(sucursalProductoId, out var stock) ? stock : null;
            if (disponible is not null && disponible >= cantidad)
                continue;

            throw paquetesPorDestino.TryGetValue(sucursalProductoId, out var sucursalProductoPaquete)
                ? new InvalidOperationException($"Stock insuficiente en el producto base para cubrir la venta del paquete (SucursalProductoId {sucursalProductoPaquete}).")
                : new InvalidOperationException($"Stock insuficiente o producto no encontrado para SucursalProductoId {sucursalProductoId}.");
        }

        // La guardia del UPDATE es redundante con la validacion de arriba, pero se conserva
        // como ultima linea de defensa: si por lo que sea alguna fila no la cumple, el conteo
        // no cuadra y la transaccion se revierte antes de registrar la venta.
        var filas = await _unitOfWork.Productos.DescontarStockBatchAsync(descuentos);
        if (filas != descuentos.Count)
            throw new InvalidOperationException(
                $"Se esperaba descontar stock de {descuentos.Count} productos y solo {filas} tenian stock suficiente.");
    }

    public async Task<bool> DevolverStockAsync(IEnumerable<DevolverStockDTO> dtos)
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
                // Si el producto devuelto es un paquete, la devolución se redirige al producto
                // base (cantidad x factor de conversión), simétrico a la venta y a la compra.
                var producto = await _unitOfWork.Productos.GetProductoByIdAsync(dto.ProductoId, dto.SucursalId);

                var esPaqueteValido = producto?.EsPaquete == true
                    && producto.ProductoBaseId is int
                    && producto.FactorConversion is decimal factorTmp
                    && factorTmp > 0;

                if (esPaqueteValido)
                {
                    var productoBaseId = producto!.ProductoBaseId!.Value;
                    var factor = producto.FactorConversion!.Value;
                    var cantidadStockBase = (int)Math.Round(dto.Cantidad * factor, MidpointRounding.AwayFromZero);

                    var resultadoBase = await _unitOfWork.Productos.IncrementarStockSinCostoAsync(productoBaseId, dto.SucursalId, cantidadStockBase);
                    if (!resultadoBase)
                        throw new InvalidOperationException($"Producto base no encontrado para la devolución del paquete (ProductoId {dto.ProductoId}) en SucursalId {dto.SucursalId}.");

                    // El lote de devolución PEPS se reingresa en el producto BASE (donde viven los lotes reales).
                    var productoBase = await _unitOfWork.Productos.GetProductoByIdAsync(productoBaseId, dto.SucursalId);
                    if (productoBase?.SucursalProducto != null)
                    {
                        await _inventarioPepsService.DevolverAFifoAsync(
                            productoBase.SucursalProducto.SucursalProductoId,
                            cantidadStockBase,
                            productoBase.SucursalProducto.UltimoPrecioCompra,
                            dto.ReferenciaTipo,
                            dto.ReferenciaId,
                            idUsuario: null);
                    }
                }
                else
                {
                    var resultado = await _unitOfWork.Productos.DevolverStockAsync(dto.ProductoId, dto.SucursalId, dto.Cantidad);
                    if (!resultado)
                        throw new InvalidOperationException($"Producto no encontrado para ProductoId {dto.ProductoId} en SucursalId {dto.SucursalId}.");

                    if (producto?.SucursalProducto != null)
                    {
                        await _inventarioPepsService.DevolverAFifoAsync(
                            producto.SucursalProducto.SucursalProductoId,
                            dto.Cantidad,
                            producto.SucursalProducto.UltimoPrecioCompra,
                            dto.ReferenciaTipo,
                            dto.ReferenciaId,
                            idUsuario: null);
                    }
                }
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

    public async Task RevertirStockPorReferenciaEnTransaccionAsync(string referenciaTipo, int referenciaId)
    {
        var movimientos = await _unitOfWork.InventarioLotes.GetMovimientosPorReferenciaAsync(referenciaTipo, referenciaId);

        foreach (var mov in movimientos.Where(m => m.TipoMovimiento == "SALIDA_VENTA"))
        {
            var producto = await _unitOfWork.Productos.GetInfoConversionBySucursalProductoIdAsync(mov.SucursalProductoId);
            if (producto?.SucursalProducto == null)
                throw new InvalidOperationException($"No se encontró el producto asociado al movimiento de stock {mov.KardexMovimientoId} para revertir.");

            var sucursalId = producto.SucursalProducto.SucursalId;

            var resultado = await _unitOfWork.Productos.DevolverStockAsync(producto.ProductoId, sucursalId, mov.Cantidad);
            if (!resultado)
                throw new InvalidOperationException($"No se pudo revertir el stock del producto {producto.ProductoId} en la sucursal {sucursalId}.");

            await _inventarioPepsService.DevolverAFifoAsync(
                mov.SucursalProductoId,
                mov.Cantidad,
                producto.SucursalProducto.UltimoPrecioCompra,
                referenciaTipo,
                referenciaId,
                idUsuario: null);
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
        ProductoId = p.ProductoId,
        Codigo = p.Codigo,
        TipoProducto = p.TipoProducto,
        CodigoSunat = p.CodigoSunat,
        NomProducto = p.NomProducto,
        UnidadMedida = p.UnidadMedida,
        TipoAfectacionIGV = p.TipoAfectacionIGV,
        IncluirIGV = p.IncluirIGV,
        UrlImagenProducto = p.UrlImagenProducto,
        Estado = p.Estado,
        FechaCreacion = p.FechaCreacion,
        CodigoBarras = p.CodigoBarras,
        EsPaquete = p.EsPaquete,
        ProductoBaseId = p.ProductoBaseId,
        FactorConversion = p.FactorConversion,
        Categoria = p.Categoria == null ? null : new ObtenerCategoriaDTO
        {
            CategoriaId = p.Categoria.CategoriaId,
            CategoriaNombre = p.Categoria.CategoriaNombre,
        },
        SucursalProducto = p.SucursalProducto == null ? null : new ObtenerSucursalProductoDTO
        {
            SucursalProductoId = p.SucursalProducto.SucursalProductoId,
            NomSucursal = p.SucursalProducto.NomSucursal,
            PrecioUnitario = p.SucursalProducto.PrecioUnitario,
            Stock = p.SucursalProducto.Stock,
            UltimoPrecioCompra = p.SucursalProducto.UltimoPrecioCompra,
            FechaUltimaCompra = p.SucursalProducto.FechaUltimaCompra,
            PrecioMayorista = p.SucursalProducto.PrecioMayorista,
            CantidadMinimaMayorista = p.SucursalProducto.CantidadMinimaMayorista,
            EnPromocion = p.SucursalProducto.EnPromocion,
            PorcentajeDescuento = p.SucursalProducto.PorcentajeDescuento,
            UsuarioId = p.SucursalProducto.UsuarioId,
            UbicacionTienda = p.SucursalProducto.UbicacionTienda,
            ProximoVencimiento = p.SucursalProducto.ProximoVencimiento,
            AlertaVencimientoActiva = p.SucursalProducto.AlertaVencimientoActiva,
            AlertaStockBajoActiva = p.SucursalProducto.AlertaStockBajoActiva,
            StockMinimoAlerta = p.SucursalProducto.StockMinimoAlerta
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

    public async Task<IEnumerable<ObtenerProductoBaseDTO>> SearchProductosRucDisponiblesAsync(int sucursalId, string palabra)
    {
        if (string.IsNullOrWhiteSpace(palabra))
            throw new ArgumentException("La palabra de búsqueda no puede estar vacía.");

        var productos = await _unitOfWork.Productos.SearchProductosRucDisponiblesAsync(sucursalId, palabra);
        return productos.Select(MapToBaseDto);
    }

    private static ObtenerProductoBaseDTO MapToBaseDto(Producto p) => new ObtenerProductoBaseDTO
    {
        ProductoId = p.ProductoId,
        Codigo = p.Codigo,
        TipoProducto = p.TipoProducto,
        CodigoSunat = p.CodigoSunat,
        NomProducto = p.NomProducto,
        UnidadMedida = p.UnidadMedida,
        TipoAfectacionIGV = p.TipoAfectacionIGV,
        IncluirIGV = p.IncluirIGV,
        UrlImagenProducto = p.UrlImagenProducto,
        Estado = p.Estado,
        FechaCreacion = p.FechaCreacion,
        CodigoBarras = p.CodigoBarras,
        EsPaquete = p.EsPaquete,
        ProductoBaseId = p.ProductoBaseId,
        FactorConversion = p.FactorConversion,
        Categoria = p.Categoria == null ? null : new ObtenerCategoriaDTO
        {
            CategoriaId = p.Categoria.CategoriaId,
            CategoriaNombre = p.Categoria.CategoriaNombre,
        }
    };

    public async Task<byte[]> GenerarReporteExcelAsync(ReporteProductoFiltroDTO filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro.EmpresaRuc))
            throw new ArgumentException("El RUC de la empresa es obligatorio.");

        var items = await _unitOfWork.Productos.GetReporteProductosAsync(filtro);
        return _excelService.GenerarReporteProductos(items, filtro);
    }

}