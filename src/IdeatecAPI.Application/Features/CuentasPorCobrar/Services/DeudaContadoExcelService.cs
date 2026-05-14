using ClosedXML.Excel;
using IdeatecAPI.Application.Features.DeudaContado.DTOs;

namespace IdeatecAPI.Application.Features.DeudaContado.Services;

public interface IDeudaContadoExcelService
{
    byte[] GenerarReporteDeudaContado(
        IEnumerable<ReporteDeudaContadoItemDto> items,
        ReporteDeudaContadoFiltroDto filtro);
}

public class DeudaContadoExcelService : IDeudaContadoExcelService
{
    public byte[] GenerarReporteDeudaContado(
        IEnumerable<ReporteDeudaContadoItemDto> items,
        ReporteDeudaContadoFiltroDto filtro)
    {
        var lista = items.ToList();
        var tieneFiltroCliente = !string.IsNullOrWhiteSpace(filtro.ClienteNumDoc);

        // ── Título ───────────────────────────────────────────────────────────
        var titulo = !string.IsNullOrWhiteSpace(filtro.TituloReporte)
            ? filtro.TituloReporte
            : $"REPORTE DE DEUDAS POR COBRAR - RUC {filtro.EmpresaRuc}";
        var partesFiltro = new List<string> { $"RUC: {filtro.EmpresaRuc}" };
        if (!string.IsNullOrWhiteSpace(filtro.EstablecimientoAnexo))
            partesFiltro.Add($"Establecimiento: {filtro.EstablecimientoAnexo}");
        if (!string.IsNullOrWhiteSpace(filtro.ClienteNumDoc))
            partesFiltro.Add($"Cliente: {filtro.ClienteNumDoc}");
        if (filtro.FechaInicio.HasValue)
            partesFiltro.Add($"Desde: {filtro.FechaInicio:dd/MM/yyyy}");
        if (filtro.FechaFin.HasValue)
            partesFiltro.Add($"Hasta: {filtro.FechaFin:dd/MM/yyyy}");
        var subtitulo = string.Join("  |  ", partesFiltro);

        // ── Colores ───────────────────────────────────────────────────────────
        var colorEncabezado  = XLColor.FromHtml("#2C3E6B");  // Azul oscuro — encabezados
        var colorCliente     = XLColor.FromHtml("#4A5568");  // Gris azulado — fila cliente
        var colorPagado      = XLColor.FromHtml("#EAF4EA");  // Verde muy suave
        var colorParcial     = XLColor.FromHtml("#FEF9EC");  // Amarillo muy suave
        var colorPendiente   = XLColor.FromHtml("#FDEEF0");  // Rosa muy suave
        var colorPago        = XLColor.FromHtml("#F7F7F7");  // Gris muy claro — filas pago
        var colorTotal       = XLColor.FromHtml("#E8ECF2");  // Gris azulado claro — totales
        var colorBorde       = XLColor.FromHtml("#D1D5DB");

        var textoEncabezado  = XLColor.White;
        var textoCliente     = XLColor.White;
        var textoPagado      = XLColor.FromHtml("#2D6A2D");
        var textoParcial     = XLColor.FromHtml("#7A5C1E");
        var textoPendiente   = XLColor.FromHtml("#7A2D2D");
        var textoPago        = XLColor.FromHtml("#555555");
        var textoTotal       = XLColor.FromHtml("#2C3E6B");

        // ── Columnas ──────────────────────────────────────────────────────────
        var columnas = new[]
        {
            ("N° Comprobante",   22),
            ("Tipo",             10),
            ("Fecha Emisión",    15),
            ("Cliente",          30),
            ("RUC / DNI",        16),
            ("Moneda",            9),
            ("Total",            14),
            ("Pagado",           14),
            ("Saldo",            14),
            ("Estado",           12),
        };
        int numCols = columnas.Length;

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Deudas por Cobrar");

        // ── Fila 1: Título ────────────────────────────────────────────────────
        var rangeTitulo = ws.Range(1, 1, 1, numCols);
        rangeTitulo.Merge();
        rangeTitulo.Value = titulo;
        rangeTitulo.Style.Font.Bold            = true;
        rangeTitulo.Style.Font.FontSize        = 13;
        rangeTitulo.Style.Font.FontColor       = XLColor.White;
        rangeTitulo.Style.Font.FontName        = "Arial";
        rangeTitulo.Style.Fill.BackgroundColor = colorEncabezado;
        rangeTitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        rangeTitulo.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
        ws.Row(1).Height = 26;

        // ── Fila 2: Subtítulo ─────────────────────────────────────────────────
        var rangeSubtitulo = ws.Range(2, 1, 2, numCols);
        rangeSubtitulo.Merge();
        rangeSubtitulo.Value = subtitulo;
        rangeSubtitulo.Style.Font.Italic          = true;
        rangeSubtitulo.Style.Font.FontSize        = 9;
        rangeSubtitulo.Style.Font.FontColor       = XLColor.White;
        rangeSubtitulo.Style.Font.FontName        = "Arial";
        rangeSubtitulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#3D4F6B");
        rangeSubtitulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        rangeSubtitulo.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
        ws.Row(2).Height = 16;

        // ── Fila 3: Vacía ─────────────────────────────────────────────────────
        ws.Row(3).Height = 6;

        // ── Fila 4: Encabezados ───────────────────────────────────────────────
        for (int i = 0; i < columnas.Length; i++)
        {
            var (nombre, ancho) = columnas[i];
            var cell = ws.Cell(4, i + 1);
            cell.Value = nombre;
            cell.Style.Font.Bold                 = true;
            cell.Style.Font.FontSize             = 10;
            cell.Style.Font.FontColor            = textoEncabezado;
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

        int currentRow = 5;

        // ── Helper: aplicar estilo a una fila ────────────────────────────────
        void EstilarFila(int row, XLColor bg, XLColor fg, bool bold = false, int fontSize = 10)
        {
            for (int col = 1; col <= numCols; col++)
            {
                var cell = ws.Cell(row, col);
                cell.Style.Font.Bold                 = bold;
                cell.Style.Font.FontSize             = fontSize;
                cell.Style.Font.FontColor            = fg;
                cell.Style.Font.FontName             = "Arial";
                cell.Style.Fill.BackgroundColor      = bg;
                cell.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = colorBorde;
                cell.Style.Alignment.Vertical        = XLAlignmentVerticalValues.Center;
            }
            ws.Row(row).Height = 18;
        }

        // ── Helper: formato moneda ────────────────────────────────────────────
        void FormatoMoneda(int row, int col)
        {
            ws.Cell(row, col).Style.NumberFormat.Format  = "#,##0.00";
            ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }

        // ── Agrupar por cliente si hay filtro cliente ─────────────────────────
        if (tieneFiltroCliente)
        {
            var porCliente = lista
                .GroupBy(x => new { x.ClienteNumDoc, x.ClienteRznSocial })
                .ToList();

            foreach (var grupoCliente in porCliente)
            {
                // Fila cliente
                var rangeCliente = ws.Range(currentRow, 1, currentRow, numCols);
                rangeCliente.Merge();
                rangeCliente.Value = $"  {grupoCliente.Key.ClienteRznSocial}  |  {grupoCliente.Key.ClienteNumDoc}";
                rangeCliente.Style.Font.Bold            = true;
                rangeCliente.Style.Font.FontSize        = 10;
                rangeCliente.Style.Font.FontColor       = textoCliente;
                rangeCliente.Style.Font.FontName        = "Arial";
                rangeCliente.Style.Fill.BackgroundColor = colorCliente;
                rangeCliente.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
                rangeCliente.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
                rangeCliente.Style.Border.OutsideBorderColor = colorBorde;
                ws.Row(currentRow).Height = 20;
                currentRow++;

                foreach (var comp in grupoCliente)
                {
                    var colorComp = comp.Estado switch
                    {
                        "PAGADO"   => colorPagado,
                        "PARCIAL"  => colorParcial,
                        _          => colorPendiente
                    };
                    var textoComp = comp.Estado switch
                    {
                        "PAGADO"   => textoPagado,
                        "PARCIAL"  => textoParcial,
                        _          => textoPendiente
                    };

                    // Fila comprobante (indentada con espacio en col 1)
                    EstilarFila(currentRow, colorComp, textoComp, bold: true);
                    ws.Cell(currentRow, 1).Value = $"  {comp.NumeroCompleto}";
                    ws.Cell(currentRow, 2).Value = comp.TipoComprobante == "01" ? "Factura" : "Boleta";
                    ws.Cell(currentRow, 3).Value = comp.FechaEmision?.ToString("dd/MM/yyyy") ?? "-";
                    ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 4).Value = comp.ClienteRznSocial;
                    ws.Cell(currentRow, 5).Value = comp.ClienteNumDoc;
                    ws.Cell(currentRow, 6).Value = comp.TipoMoneda;
                    ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 7).Value = comp.MontoTotal;
                    ws.Cell(currentRow, 8).Value = comp.MontoPagado;
                    ws.Cell(currentRow, 9).Value = comp.Saldo;
                    ws.Cell(currentRow, 10).Value = comp.Estado;
                    ws.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    FormatoMoneda(currentRow, 7);
                    FormatoMoneda(currentRow, 8);
                    FormatoMoneda(currentRow, 9);
                    currentRow++;

                    // Filas pagos
                    if (comp.Pagos.Any())
                    {
                        foreach (var pago in comp.Pagos)
                        {
                            EstilarFila(currentRow, colorPago, textoPago);
                            ws.Cell(currentRow, 1).Value = "";
                            ws.Cell(currentRow, 2).Value = "  → Pago";
                            ws.Cell(currentRow, 3).Value = pago.FechaPago.ToString("dd/MM/yyyy");
                            ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(currentRow, 4).Value = pago.MedioPago ?? "-";
                            ws.Cell(currentRow, 5).Value = pago.EntidadFinanciera ?? "-";
                            ws.Cell(currentRow, 6).Value = "-";
                            ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            ws.Cell(currentRow, 7).Value = "";
                            ws.Cell(currentRow, 8).Value = pago.MontoPagado;
                            ws.Cell(currentRow, 9).Value = pago.NumeroOperacion ?? "-";
                            ws.Cell(currentRow, 10).Value = pago.Observaciones ?? "-";
                            FormatoMoneda(currentRow, 8);
                            currentRow++;
                        }
                    }
                    else
                    {
                        EstilarFila(currentRow, colorPago, textoPago);
                        var rangeSinPagos = ws.Range(currentRow, 2, currentRow, numCols);
                        rangeSinPagos.Merge();
                        rangeSinPagos.Value = "  Sin pagos registrados";
                        rangeSinPagos.Style.Font.Italic = true;
                        currentRow++;
                    }
                }

                // Espacio entre clientes
                ws.Row(currentRow).Height = 6;
                currentRow++;
            }
        }
        else
        {
            // Sin filtro cliente — agrupado solo por comprobante
            foreach (var comp in lista)
            {
                var colorComp = comp.Estado switch
                {
                    "PAGADO"  => colorPagado,
                    "PARCIAL" => colorParcial,
                    _         => colorPendiente
                };
                var textoComp = comp.Estado switch
                {
                    "PAGADO"  => textoPagado,
                    "PARCIAL" => textoParcial,
                    _         => textoPendiente
                };

                // Fila comprobante
                EstilarFila(currentRow, colorComp, textoComp, bold: true);
                ws.Cell(currentRow, 1).Value = comp.NumeroCompleto;
                ws.Cell(currentRow, 2).Value = comp.TipoComprobante == "01" ? "Factura" : "Boleta";
                ws.Cell(currentRow, 3).Value = comp.FechaEmision?.ToString("dd/MM/yyyy") ?? "-";
                ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(currentRow, 4).Value = comp.ClienteRznSocial;
                ws.Cell(currentRow, 5).Value = comp.ClienteNumDoc;
                ws.Cell(currentRow, 6).Value = comp.TipoMoneda;
                ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(currentRow, 7).Value = comp.MontoTotal;
                ws.Cell(currentRow, 8).Value = comp.MontoPagado;
                ws.Cell(currentRow, 9).Value = comp.Saldo;
                ws.Cell(currentRow, 10).Value = comp.Estado;
                ws.Cell(currentRow, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                FormatoMoneda(currentRow, 7);
                FormatoMoneda(currentRow, 8);
                FormatoMoneda(currentRow, 9);
                currentRow++;

                // Filas pagos
                if (comp.Pagos.Any())
                {
                    foreach (var pago in comp.Pagos)
                    {
                        EstilarFila(currentRow, colorPago, textoPago);
                        ws.Cell(currentRow, 1).Value = "";
                        ws.Cell(currentRow, 2).Value = "  → Pago";
                        ws.Cell(currentRow, 3).Value = pago.FechaPago.ToString("dd/MM/yyyy");
                        ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(currentRow, 4).Value = pago.MedioPago ?? "-";
                        ws.Cell(currentRow, 5).Value = pago.EntidadFinanciera ?? "-";
                        ws.Cell(currentRow, 6).Value = "-";
                        ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(currentRow, 7).Value = "";
                        ws.Cell(currentRow, 8).Value = pago.MontoPagado;
                        ws.Cell(currentRow, 9).Value = pago.NumeroOperacion ?? "-";
                        ws.Cell(currentRow, 10).Value = pago.Observaciones ?? "-";
                        FormatoMoneda(currentRow, 8);
                        currentRow++;
                    }
                }
                else
                {
                    EstilarFila(currentRow, colorPago, textoPago);
                    var rangeSinPagos = ws.Range(currentRow, 2, currentRow, numCols);
                    rangeSinPagos.Merge();
                    rangeSinPagos.Value = "  Sin pagos registrados";
                    rangeSinPagos.Style.Font.Italic = true;
                    currentRow++;
                }
            }
        }

        // ── Fila total general ────────────────────────────────────────────────
        var rangeTotal = ws.Range(currentRow, 1, currentRow, 6);
        rangeTotal.Merge();
        rangeTotal.Value = $"TOTAL: {lista.Count} comprobante(s)";
        rangeTotal.Style.Font.Bold                  = true;
        rangeTotal.Style.Font.FontSize              = 10;
        rangeTotal.Style.Font.FontColor             = textoTotal;
        rangeTotal.Style.Font.FontName              = "Arial";
        rangeTotal.Style.Fill.BackgroundColor       = colorTotal;
        rangeTotal.Style.Alignment.Horizontal       = XLAlignmentHorizontalValues.Left;
        rangeTotal.Style.Alignment.Vertical         = XLAlignmentVerticalValues.Center;
        rangeTotal.Style.Border.OutsideBorder       = XLBorderStyleValues.Thin;
        rangeTotal.Style.Border.OutsideBorderColor  = colorBorde;

        var cellTotalMonto = ws.Cell(currentRow, 7);
        cellTotalMonto.Value = lista.Sum(x => x.MontoTotal ?? 0);
        cellTotalMonto.Style.Font.Bold                 = true;
        cellTotalMonto.Style.Font.FontSize             = 10;
        cellTotalMonto.Style.Font.FontColor            = textoTotal;
        cellTotalMonto.Style.Font.FontName             = "Arial";
        cellTotalMonto.Style.Fill.BackgroundColor      = colorTotal;
        cellTotalMonto.Style.NumberFormat.Format       = "#,##0.00";
        cellTotalMonto.Style.Alignment.Horizontal      = XLAlignmentHorizontalValues.Right;
        cellTotalMonto.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
        cellTotalMonto.Style.Border.OutsideBorderColor = colorBorde;

        var cellTotalPagado = ws.Cell(currentRow, 8);
        cellTotalPagado.Value = lista.Sum(x => x.MontoPagado ?? 0);
        cellTotalPagado.Style.Font.Bold                 = true;
        cellTotalPagado.Style.Font.FontSize             = 10;
        cellTotalPagado.Style.Font.FontColor            = textoTotal;
        cellTotalPagado.Style.Font.FontName             = "Arial";
        cellTotalPagado.Style.Fill.BackgroundColor      = colorTotal;
        cellTotalPagado.Style.NumberFormat.Format       = "#,##0.00";
        cellTotalPagado.Style.Alignment.Horizontal      = XLAlignmentHorizontalValues.Right;
        cellTotalPagado.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
        cellTotalPagado.Style.Border.OutsideBorderColor = colorBorde;

        var cellTotalSaldo = ws.Cell(currentRow, 9);
        cellTotalSaldo.Value = lista.Sum(x => x.Saldo ?? 0);
        cellTotalSaldo.Style.Font.Bold                 = true;
        cellTotalSaldo.Style.Font.FontSize             = 10;
        cellTotalSaldo.Style.Font.FontColor            = textoTotal;
        cellTotalSaldo.Style.Font.FontName             = "Arial";
        cellTotalSaldo.Style.Fill.BackgroundColor      = colorTotal;
        cellTotalSaldo.Style.NumberFormat.Format       = "#,##0.00";
        cellTotalSaldo.Style.Alignment.Horizontal      = XLAlignmentHorizontalValues.Right;
        cellTotalSaldo.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
        cellTotalSaldo.Style.Border.OutsideBorderColor = colorBorde;

        // Celda estado vacía en total
        var cellEstadoTotal = ws.Cell(currentRow, 10);
        cellEstadoTotal.Style.Fill.BackgroundColor      = colorTotal;
        cellEstadoTotal.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
        cellEstadoTotal.Style.Border.OutsideBorderColor = colorBorde;

        ws.Row(currentRow).Height = 20;
        ws.SheetView.FreezeRows(4);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}