using ClosedXML.Excel;
using IdeatecAPI.Application.Features.Productos.DTO;

namespace IdeatecAPI.Application.Features.Productos.Services;

public class ProductoExcelService : IProductoExcelService
{
    public byte[] GenerarReporteProductos(
        IEnumerable<ReporteProductoItemDTO> items,
        ReporteProductoFiltroDTO filtro)
    {
        // --- Título automático si no se envía uno ---
        var titulo = !string.IsNullOrWhiteSpace(filtro.TituloReporte)
            ? filtro.TituloReporte
            : $"REPORTE DE PRODUCTOS - RUC {filtro.EmpresaRuc}";

        // --- Subtítulo con filtros aplicados ---
        var partesFiltro = new List<string> { $"RUC: {filtro.EmpresaRuc}" };
        if (filtro.SucursalId.HasValue)
            partesFiltro.Add($"Sucursal ID: {filtro.SucursalId}");
        if (filtro.CategoriaId.HasValue)
            partesFiltro.Add($"Categoría ID: {filtro.CategoriaId}");
        if (!string.IsNullOrWhiteSpace(filtro.IgvTipo))
        {
            var igvDesc = filtro.IgvTipo switch
            {
                "10" => "Gravado",
                "20" => "Exonerado",
                "30" => "Inafecto",
                _    => filtro.IgvTipo
            };
            partesFiltro.Add($"IGV: {igvDesc}");
        }
        if (!string.IsNullOrWhiteSpace(filtro.TipoProducto))
            partesFiltro.Add($"Tipo: {filtro.TipoProducto}");

        var stockDesc = filtro.StockFiltro?.ToLower() switch
        {
            "sin_stock" => "Sin stock",
            "con_stock" => "Con stock",
            "menor_a"   => $"Stock menor a {filtro.StockValor}",
            _           => null
        };
        if (stockDesc != null)
            partesFiltro.Add($"Stock: {stockDesc}");

        var subtitulo = string.Join("  |  ", partesFiltro);

        // --- Colores ---
        var colorTitulo     = XLColor.FromHtml("#1F3864");
        var colorSubtitulo  = XLColor.FromHtml("#2E75B6");
        var colorEncabezado = XLColor.FromHtml("#1F3864");
        var colorFilaPar    = XLColor.FromHtml("#EBF3FB");
        var colorTotal      = XLColor.FromHtml("#D6E4F0");
        var colorBorde      = XLColor.FromHtml("#B8CCE4");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Reporte Productos");

        var columnas = new[]
        {
            ("Código",              14),
            ("Código de Barras",    20),
            ("Nombre Producto",     34),
            ("Categoría",           18),
            ("Tipo Producto",       14),
            ("Unid. Medida",        13),
            ("Tipo IGV",            13),
            ("Inc. IGV",            10),
            ("Sucursal",            22),
            ("Precio Compra (S/)",  18),
            ("Precio Venta (S/)",   18),
            ("Stock",               10),
            ("Imagen (URL)",        50),
        };

        // Índices (0-based). La imagen va al final a propósito: así las columnas
        // numéricas conservan sus letras (J/K/L) en las fórmulas de la fila TOTAL.
        const int colPrecioCompra = 9;   // J
        const int colPrecioVenta  = 10;  // K
        const int colStock        = 11;  // L
        const int colUrlImagen    = 12;  // M

        int numCols = columnas.Length;

        // --- FILA 1: Título ---
        var rangeTitulo = ws.Range(1, 1, 1, numCols);
        rangeTitulo.Merge();
        rangeTitulo.Value = titulo;
        rangeTitulo.Style.Font.Bold             = true;
        rangeTitulo.Style.Font.FontSize         = 14;
        rangeTitulo.Style.Font.FontColor        = XLColor.White;
        rangeTitulo.Style.Font.FontName         = "Arial";
        rangeTitulo.Style.Fill.BackgroundColor  = colorTitulo;
        rangeTitulo.Style.Alignment.Horizontal  = XLAlignmentHorizontalValues.Center;
        rangeTitulo.Style.Alignment.Vertical    = XLAlignmentVerticalValues.Center;
        ws.Row(1).Height = 28;

        // --- FILA 2: Subtítulo ---
        var rangeSubtitulo = ws.Range(2, 1, 2, numCols);
        rangeSubtitulo.Merge();
        rangeSubtitulo.Value = subtitulo;
        rangeSubtitulo.Style.Font.Italic          = true;
        rangeSubtitulo.Style.Font.FontSize        = 10;
        rangeSubtitulo.Style.Font.FontColor       = XLColor.White;
        rangeSubtitulo.Style.Font.FontName        = "Arial";
        rangeSubtitulo.Style.Fill.BackgroundColor = colorSubtitulo;
        rangeSubtitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        rangeSubtitulo.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
        ws.Row(2).Height = 18;

        // --- FILA 3: Vacía ---
        ws.Row(3).Height = 8;

        // --- FILA 4: Encabezados ---
        for (int i = 0; i < columnas.Length; i++)
        {
            var (nombre, ancho) = columnas[i];
            var cell = ws.Cell(4, i + 1);
            cell.Value = nombre;
            cell.Style.Font.Bold                 = true;
            cell.Style.Font.FontSize             = 10;
            cell.Style.Font.FontColor            = XLColor.White;
            cell.Style.Font.FontName             = "Arial";
            cell.Style.Fill.BackgroundColor      = colorEncabezado;
            cell.Style.Alignment.Horizontal      = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical        = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText        = true;
            cell.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = colorBorde;
            ws.Column(i + 1).Width = ancho;
        }
        ws.Row(4).Height = 22;

        // --- DATOS ---
        var lista = items.ToList();
        int dataStartRow = 5;

        for (int i = 0; i < lista.Count; i++)
        {
            var item    = lista[i];
            int row     = dataStartRow + i;
            bool esPar  = i % 2 == 0;
            var bgColor = esPar ? colorFilaPar : XLColor.White;

            var igvLabel = item.TipoAfectacionIGV switch
            {
                "10" => "Gravado",
                "20" => "Exonerado",
                "30" => "Inafecto",
                _    => item.TipoAfectacionIGV ?? ""
            };

            // Los servicios no manejan costo ni stock: se marcan con "-".
            bool esServicio = item.TipoProducto == "SERVICIO";

            var valores = new object?[]
            {
                item.Codigo,
                item.CodigoBarras,
                item.NomProducto,
                item.CategoriaNombre ?? "Sin categoría",
                item.TipoProducto,
                item.UnidadMedida,
                igvLabel,
                item.IncluirIGV == true ? "Sí" : "No",
                item.NomSucursal,
                esServicio ? (object?)"-" : item.PrecioCompra,
                item.PrecioUnitario,
                esServicio ? (object?)"-" : item.Stock,
                item.UrlImagenProducto
            };

            for (int col = 0; col < valores.Length; col++)
            {
                var cell = ws.Cell(row, col + 1);
                cell.Value = valores[col] is null
                    ? XLCellValue.FromObject("")
                    : XLCellValue.FromObject(valores[col]!);
                cell.Style.Font.FontSize             = 10;
                cell.Style.Font.FontName             = "Arial";
                cell.Style.Fill.BackgroundColor      = bgColor;
                cell.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = colorBorde;

                if (col == colPrecioCompra || col == colPrecioVenta)
                {
                    cell.Style.NumberFormat.Format  = "#,##0.00";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }
                else if (col == colStock)
                {
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                else if (col == colUrlImagen)
                {
                    // Sin WrapText: la URL debe quedar en una sola línea para poder
                    // copiarla o reimportarla tal cual.
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    cell.Style.Font.FontSize        = 9;
                }
                else
                {
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    cell.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                }
            }
        }

        // --- FILA TOTAL ---
        int totalRow = dataStartRow + lista.Count;
        if (lista.Count > 0)
        {
            int lastDataRow = totalRow - 1;

            var rangeTotalLabel = ws.Range(totalRow, 1, totalRow, colPrecioCompra);
            rangeTotalLabel.Merge();
            rangeTotalLabel.Value = $"TOTAL: {lista.Count} producto(s)";
            rangeTotalLabel.Style.Font.Bold                  = true;
            rangeTotalLabel.Style.Font.FontSize              = 10;
            rangeTotalLabel.Style.Font.FontColor             = colorTitulo;
            rangeTotalLabel.Style.Font.FontName              = "Arial";
            rangeTotalLabel.Style.Fill.BackgroundColor       = colorTotal;
            rangeTotalLabel.Style.Alignment.Horizontal       = XLAlignmentHorizontalValues.Left;
            rangeTotalLabel.Style.Alignment.Vertical         = XLAlignmentVerticalValues.Center;
            rangeTotalLabel.Style.Border.OutsideBorder       = XLBorderStyleValues.Thin;
            rangeTotalLabel.Style.Border.OutsideBorderColor  = colorBorde;

            // Valorización del inventario: costo total (compra x stock) y venta total (venta x stock).
            // SUMPRODUCT ignora los "-" de los servicios porque los trata como cero.
            var cellCostoTotal = ws.Cell(totalRow, colPrecioCompra + 1);
            cellCostoTotal.FormulaA1                        = $"=SUMPRODUCT(J{dataStartRow}:J{lastDataRow},L{dataStartRow}:L{lastDataRow})";
            cellCostoTotal.Style.Font.Bold                  = true;
            cellCostoTotal.Style.Font.FontSize              = 10;
            cellCostoTotal.Style.Font.FontColor             = colorTitulo;
            cellCostoTotal.Style.Font.FontName              = "Arial";
            cellCostoTotal.Style.Fill.BackgroundColor       = colorTotal;
            cellCostoTotal.Style.NumberFormat.Format        = "#,##0.00";
            cellCostoTotal.Style.Alignment.Horizontal       = XLAlignmentHorizontalValues.Right;
            cellCostoTotal.Style.Border.OutsideBorder       = XLBorderStyleValues.Thin;
            cellCostoTotal.Style.Border.OutsideBorderColor  = colorBorde;

            var cellValorTotal = ws.Cell(totalRow, colPrecioVenta + 1);
            cellValorTotal.FormulaA1                        = $"=SUMPRODUCT(K{dataStartRow}:K{lastDataRow},L{dataStartRow}:L{lastDataRow})";
            cellValorTotal.Style.Font.Bold                  = true;
            cellValorTotal.Style.Font.FontSize              = 10;
            cellValorTotal.Style.Font.FontColor             = colorTitulo;
            cellValorTotal.Style.Font.FontName              = "Arial";
            cellValorTotal.Style.Fill.BackgroundColor       = colorTotal;
            cellValorTotal.Style.NumberFormat.Format        = "#,##0.00";
            cellValorTotal.Style.Alignment.Horizontal       = XLAlignmentHorizontalValues.Right;
            cellValorTotal.Style.Border.OutsideBorder       = XLBorderStyleValues.Thin;
            cellValorTotal.Style.Border.OutsideBorderColor  = colorBorde;

            var cellStockTotal = ws.Cell(totalRow, colStock + 1);
            cellStockTotal.FormulaA1                        = $"=SUM(L{dataStartRow}:L{lastDataRow})";
            cellStockTotal.Style.Font.Bold                  = true;
            cellStockTotal.Style.Font.FontSize              = 10;
            cellStockTotal.Style.Font.FontColor             = colorTitulo;
            cellStockTotal.Style.Font.FontName              = "Arial";
            cellStockTotal.Style.Fill.BackgroundColor       = colorTotal;
            cellStockTotal.Style.Alignment.Horizontal       = XLAlignmentHorizontalValues.Center;
            cellStockTotal.Style.Border.OutsideBorder       = XLBorderStyleValues.Thin;
            cellStockTotal.Style.Border.OutsideBorderColor  = colorBorde;

            // Celda vacía para que la banda del total cubra también la columna de imagen.
            var cellRelleno = ws.Cell(totalRow, colUrlImagen + 1);
            cellRelleno.Style.Fill.BackgroundColor      = colorTotal;
            cellRelleno.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
            cellRelleno.Style.Border.OutsideBorderColor = colorBorde;

            ws.Row(totalRow).Height = 20;
        }

        ws.SheetView.FreezeRows(4);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}