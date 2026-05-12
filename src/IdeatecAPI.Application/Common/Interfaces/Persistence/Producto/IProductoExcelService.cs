using IdeatecAPI.Application.Features.Productos.DTO;

namespace IdeatecAPI.Application.Features.Productos.Services;

public interface IProductoExcelService
{
    byte[] GenerarReporteProductos(
        IEnumerable<ReporteProductoItemDTO> items,
        ReporteProductoFiltroDTO filtro);
}