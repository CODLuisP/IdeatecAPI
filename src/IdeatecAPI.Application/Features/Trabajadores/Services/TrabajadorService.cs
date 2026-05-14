using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Trabajadores.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Trabajadores.Services;

public interface ITrabajadorService
{
    Task<IEnumerable<ObtenerTrabajadorDTO>> GetAllBySucursalAsync(int sucursalId);
    Task<ObtenerTrabajadorDTO?> GetByIdAsync(int id);
    Task<ObtenerTrabajadorDTO> RegistrarAsync(RegistrarTrabajadorDTO dto);
    Task<bool> EditarAsync(EditarTrabajadorDTO dto);
    Task<bool> EliminarAsync(int id);
    Task<IEnumerable<ObtenerTrabajadorDTO>> SearchAsync(int sucursalId, string palabra);
    Task<ReporteTrabajadorDTO?> GetReporteByTrabajadorAsync(int trabajadorId, DateTime? fechaDesde, DateTime? fechaHasta);
    Task<IEnumerable<RankingTrabajadorDTO>> GetRankingBySucursalAsync(
    int sucursalId,
    DateTime? fechaDesde,
    DateTime? fechaHasta);

    Task<IEnumerable<ServicioTopDTO>> GetServiciosTopBySucursalAsync(
        int sucursalId,
        DateTime? fechaDesde,
        DateTime? fechaHasta);

    Task<IEnumerable<ReporteClienteDTO>> GetReporteByClienteAsync(
    int sucursalId,
    string palabra,
    DateTime? fechaDesde,
    DateTime? fechaHasta);

    Task<IEnumerable<ReporteServicioRawDTO>> GetDetalleByServicioAsync(
    int sucursalId,
    string descripcion,
    DateTime? fechaDesde,
    DateTime? fechaHasta);
}

public class TrabajadorService : ITrabajadorService
{
    private readonly IUnitOfWork _unitOfWork;

    public TrabajadorService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ObtenerTrabajadorDTO>> GetAllBySucursalAsync(int sucursalId)
    {
        var trabajadores = await _unitOfWork.Trabajadores.GetAllBySucursalAsync(sucursalId);
        return trabajadores.Select(MapToDTO);
    }

    public async Task<ObtenerTrabajadorDTO?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Id inválido.");

        var trabajador = await _unitOfWork.Trabajadores.GetByIdAsync(id);
        return trabajador == null ? null : MapToDTO(trabajador);
    }

    public async Task<ObtenerTrabajadorDTO> RegistrarAsync(RegistrarTrabajadorDTO dto)
    {
        // Validar DNI duplicado dentro de la misma sucursal
        var existente = await _unitOfWork.Trabajadores.GetByDniEnSucursalAsync(dto.Dni!, dto.SucursalId);
        if (existente != null)
            throw new InvalidOperationException($"Ya existe un trabajador con el DNI '{dto.Dni}' en esta sucursal.");

        _unitOfWork.BeginTransaction();
        try
        {
            var trabajador = new Trabajador
            {
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                Dni = dto.Dni,
                Celular = dto.Celular,
                Email = dto.Email,
                SucursalId = dto.SucursalId,
                Estado = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var creado = await _unitOfWork.Trabajadores.RegistrarAsync(trabajador);
            _unitOfWork.Commit();

            var completo = await _unitOfWork.Trabajadores.GetByIdAsync(creado.Id);
            return MapToDTO(completo!);
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EditarAsync(EditarTrabajadorDTO dto)
    {
        if (dto.Id == null || dto.Id <= 0)
            throw new ArgumentException("Id es obligatorio.");

        _unitOfWork.BeginTransaction();
        try
        {
            var trabajador = new Trabajador
            {
                Id = dto.Id.Value,
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                Dni = dto.Dni,
                Celular = dto.Celular,
                Email = dto.Email,
                SucursalId = dto.SucursalId ?? 0,
                UpdatedAt = DateTime.Now
            };

            var resultado = await _unitOfWork.Trabajadores.EditarAsync(trabajador);
            _unitOfWork.Commit();
            return resultado;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> EliminarAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Id inválido.");

        _unitOfWork.BeginTransaction();
        try
        {
            var resultado = await _unitOfWork.Trabajadores.EliminarAsync(id);
            _unitOfWork.Commit();
            return resultado;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<ObtenerTrabajadorDTO>> SearchAsync(int sucursalId, string palabra)
    {
        var trabajadores = await _unitOfWork.Trabajadores.SearchBySucursalAsync(sucursalId, palabra);
        return trabajadores.Select(MapToDTO);
    }

    private static ObtenerTrabajadorDTO MapToDTO(Trabajador t) => new()
    {
        Id = t.Id,
        Nombres = t.Nombres,
        Apellidos = t.Apellidos,
        Dni = t.Dni,
        Celular = t.Celular,
        Email = t.Email,
        Estado = t.Estado,
        SucursalId = t.SucursalId,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };

    public async Task<ReporteTrabajadorDTO?> GetReporteByTrabajadorAsync(
    int trabajadorId,
    DateTime? fechaDesde,
    DateTime? fechaHasta)
    {
        if (trabajadorId <= 0)
            throw new ArgumentException("Id inválido.");

        var filas = await _unitOfWork.Trabajadores.GetServiciosByTrabajadorAsync(
            trabajadorId, fechaDesde, fechaHasta);

        if (!filas.Any())
            return null;

        var primera = filas.First();

        var reporte = new ReporteTrabajadorDTO
        {
            TrabajadorId = primera.TrabajadorId,
            Nombres = primera.Nombres,
            Apellidos = primera.Apellidos,
            Dni = primera.Dni,

            Servicios = filas.Select(f => new ServicioTrabajadorDTO
            {
                ComprobanteId = f.ComprobanteId,
                NumeroCompleto = f.NumeroCompleto,
                TipoComprobante = f.TipoComprobante,
                FechaEmision = f.FechaEmision,
                TipoMoneda = f.TipoMoneda,
                EstadoSunat = f.EstadoSunat,
                ClienteNumDoc = f.ClienteNumDoc,
                ClienteRazonSocial = f.ClienteRazonSocial,
                DetalleId = f.DetalleId,
                Codigo = f.Codigo,
                Descripcion = f.Descripcion,
                Cantidad = f.Cantidad,
                UnidadMedida = f.UnidadMedida,
                PrecioUnitario = f.PrecioUnitario,
                TotalVentaItem = f.TotalVentaItem
            }).ToList()
        };

        // Totales calculados en memoria
        reporte.TotalServicios = reporte.Servicios.Count;
        reporte.TotalComprobantes = reporte.Servicios.Select(s => s.ComprobanteId).Distinct().Count();
        reporte.TotalMonto = reporte.Servicios.Sum(s => s.TotalVentaItem);

        return reporte;
    }

    public async Task<IEnumerable<RankingTrabajadorDTO>> GetRankingBySucursalAsync(
    int sucursalId,
    DateTime? fechaDesde,
    DateTime? fechaHasta)
    {
        if (sucursalId <= 0)
            throw new ArgumentException("SucursalId inválido.");

        return await _unitOfWork.Trabajadores.GetRankingBySucursalAsync(
            sucursalId, fechaDesde, fechaHasta);
    }

    public async Task<IEnumerable<ServicioTopDTO>> GetServiciosTopBySucursalAsync(
        int sucursalId,
        DateTime? fechaDesde,
        DateTime? fechaHasta)
    {
        if (sucursalId <= 0)
            throw new ArgumentException("SucursalId inválido.");

        return await _unitOfWork.Trabajadores.GetServiciosTopBySucursalAsync(
            sucursalId, fechaDesde, fechaHasta);
    }

    public async Task<IEnumerable<ReporteClienteDTO>> GetReporteByClienteAsync(
    int sucursalId,
    string palabra,
    DateTime? fechaDesde,
    DateTime? fechaHasta)
    {
        if (string.IsNullOrWhiteSpace(palabra))
            throw new ArgumentException("Debe ingresar una palabra de búsqueda.");

        var filas = await _unitOfWork.Trabajadores.GetServiciosByClienteAsync(
            sucursalId, palabra, fechaDesde, fechaHasta);

        if (!filas.Any()) return [];

        // Agrupar por cliente
        return filas
            .GroupBy(f => f.ClienteNumDoc)
            .Select(gCliente =>
            {
                var filasCliente = gCliente.ToList();
                var primera = filasCliente.First();

                return new ReporteClienteDTO
                {
                    ClienteNumDoc = primera.ClienteNumDoc,
                    ClienteRazonSocial = primera.ClienteRazonSocial,
                    TotalServicios = filasCliente.Count,
                    TotalComprobantes = filasCliente.Select(f => f.ComprobanteId).Distinct().Count(),
                    TotalMonto = filasCliente.Sum(f => f.TotalVentaItem),

                    // Agrupar por trabajador dentro del cliente
                    Trabajadores = filasCliente
                        .GroupBy(f => f.TrabajadorId)
                        .Select(gTrabajador =>
                        {
                            var filasT = gTrabajador.ToList();
                            var primeraT = filasT.First();

                            return new ReporteTrabajadorDTO
                            {
                                TrabajadorId = primeraT.TrabajadorId,
                                Nombres = primeraT.Nombres,
                                Apellidos = primeraT.Apellidos,
                                Dni = primeraT.Dni,
                                TotalServicios = filasT.Count,
                                TotalComprobantes = filasT.Select(f => f.ComprobanteId).Distinct().Count(),
                                TotalMonto = filasT.Sum(f => f.TotalVentaItem),
                                Servicios = filasT.Select(f => new ServicioTrabajadorDTO
                                {
                                    ComprobanteId = f.ComprobanteId,
                                    NumeroCompleto = f.NumeroCompleto,
                                    TipoComprobante = f.TipoComprobante,
                                    FechaEmision = f.FechaEmision,
                                    TipoMoneda = f.TipoMoneda,
                                    EstadoSunat = f.EstadoSunat,
                                    ClienteNumDoc = f.ClienteNumDoc,
                                    ClienteRazonSocial = f.ClienteRazonSocial,
                                    DetalleId = f.DetalleId,
                                    Codigo = f.Codigo,
                                    Descripcion = f.Descripcion,
                                    Cantidad = f.Cantidad,
                                    UnidadMedida = f.UnidadMedida,
                                    PrecioUnitario = f.PrecioUnitario,
                                    TotalVentaItem = f.TotalVentaItem
                                }).ToList()
                            };
                        }).ToList()
                };
            }).ToList();
    }

    public async Task<IEnumerable<ReporteServicioRawDTO>> GetDetalleByServicioAsync(
        int sucursalId,
        string descripcion,
        DateTime? fechaDesde,
        DateTime? fechaHasta)
    {
        return await _unitOfWork.Trabajadores.GetDetalleByServicioAsync(
            sucursalId, descripcion, fechaDesde, fechaHasta);
    }
}