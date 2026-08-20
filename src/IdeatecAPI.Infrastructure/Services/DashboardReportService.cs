using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Reportes.DTOs;
using IdeatecAPI.Application.Features.Reportes.Services;

namespace IdeatecAPI.Infrastructure.Services;

public class DashboardReportService : IDashboardReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardReportDto> GenerarReporteAsync(DashboardReportRequestDto request)
    {
        var ruc = request.Ruc;
        var cod = request.CodEstablecimiento;
        var desde = request.FechaDesde;
        var hasta = request.FechaHasta;
        var usuario = request.UsuarioCreacion;
        var limit = request.TopLimit;

        var repo = _unitOfWork.Reportes;
        var esSoloDia = desde.HasValue && hasta.HasValue && desde.Value.Date == hasta.Value.Date;

        // ── Consultas secuenciales (comparten una sola IDbConnection) ────────
        var reporte = await repo.GetReportesPorEmpresaAsync(
            ruc, "personalizado", desde, hasta, limit, usuario);
        var totalesTrib = await repo.GetTotalesTributariosAsync(ruc, cod, desde, hasta, usuario);
        var ganancias   = await repo.GetGananciasAsync(ruc, cod, desde, hasta, usuario);
        var topComp = (await repo.GetTopComprobantesAsync(ruc, cod, desde, hasta, usuario, limit)).ToList();
        var topNV = (await repo.GetTopNotasVentaAsync(ruc, cod, desde, hasta, usuario, limit)).ToList();
        var topComision = (await repo.GetTopComisionTarjetaAsync(ruc, cod, desde, hasta, usuario, limit)).ToList();
        var productos = (await repo.GetProductosTopAsync(ruc, cod, desde, hasta, usuario, limit: limit, orderBy: "cantidad", filtroNV: "incluir")).ToList();
        var medios = (await repo.GetMediosPagoTopAsync(ruc, cod, desde, hasta, usuario)).ToList();
        var ventasPorUsuario = esSoloDia
            ? (await repo.GetVentasPorUsuarioAsync(ruc, cod, desde, hasta)).ToList()
            : new List<VentasPorUsuarioDto>();

        // ── Resolver nombres para la cabecera ────────────────────────────────
        var empresa = await _unitOfWork.Empresas.GetEmpresaByRucAsync(ruc);
        var empresaNombre = empresa?.RazonSocial ?? ruc;

        string? nombreSucursal = null;
        if (!string.IsNullOrEmpty(cod))
        {
            var sucursales = await _unitOfWork.Sucursal.GetByRucSucursalAsync(ruc);
            nombreSucursal = sucursales.FirstOrDefault(s => s.CodEstablecimiento == cod)?.Nombre;
        }

        string? nombreUsuario = null;
        if (usuario.HasValue)
        {
            var usr = await _unitOfWork.Usuarios.GetByIdAsync(usuario.Value);
            nombreUsuario = usr?.Username;
        }

        var ventasTributarias = reporte.Kpi.TotalVentas;
        var totalNV  = reporte.Distribucion.TotalNotasVenta;
        var totalNC  = totalesTrib.TotalNC;
        var totalND  = totalesTrib.TotalND;
        // Ventas totales = facturas + boletas + NV - NC + ND + Comisión POS
        var ventasTotal = ventasTributarias + totalNV - totalNC + totalND + reporte.Kpi.TotalComisionTarjeta;

        var utilidad   = ganancias.IngresoVentas - ganancias.CostoVentas;
        var porcentaje = ganancias.IngresoVentas > 0
            ? Math.Round(utilidad / ganancias.IngresoVentas * 100, 2)
            : 0m;

        return new DashboardReportDto
        {
            EmpresaRazonSocial = empresaNombre,
            EmpresaRuc = ruc,
            NombreSucursal = nombreSucursal,
            NombreUsuario = nombreUsuario,
            FechaDesde = desde ?? DateTime.Today,
            FechaHasta = hasta ?? DateTime.Today,
            FechaGeneracion = DateTime.Now,

            VentasTributarias = ventasTributarias,
            VentasTotal = ventasTotal,
            TotalIGV = reporte.Kpi.TotalIGV,
            TotalDocumentos = reporte.Kpi.TotalDocumentos,
            TotalNotasVenta = totalNV,
            CantidadNotasVenta = reporte.Distribucion.NotasVenta,
            TotalComisionPOS = reporte.Kpi.TotalComisionTarjeta,

            Ganancias = utilidad,
            PorcentajeGanancias = porcentaje,

            VentasTributariasAnterior = reporte.Kpi.TotalVentasAnterior,
            TotalIGVAnterior = reporte.Kpi.TotalIGVAnterior,
            TotalDocumentosAnterior = reporte.Kpi.TotalDocumentosAnterior,

            TotalesTributarios = totalesTrib,

            CantidadNC = reporte.Distribucion.NotasCredito,
            CantidadND = reporte.Distribucion.NotasDebito,
            TotalNC = totalNC,
            TotalND = totalND,

            Distribucion = reporte.Distribucion,
            Grafico = reporte.Grafico,

            TopComprobantes = topComp,
            TopNotasVenta = topNV,
            TopComisionTarjeta = topComision,
            TopClientes = reporte.TopClientes,
            TopProductos = productos,
            MediosPago = medios,
            VentasPorUsuario = ventasPorUsuario,
        };
    }
}
