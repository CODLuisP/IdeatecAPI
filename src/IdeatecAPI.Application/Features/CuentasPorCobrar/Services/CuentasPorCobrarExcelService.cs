using ClosedXML.Excel;
using IdeatecAPI.Application.Features.CuentasPorCobrar.DTOs;

namespace IdeatecAPI.Application.Features.CuentasPorCobrar.Services;

public interface ICuentasPorCobrarExcelService
{
    byte[] GenerarReporteCuentasPorCobrar(
        IEnumerable<ReporteCuentasPorCobrarItemDto> items,
        ReporteCuentasPorCobrarFiltroDto filtro);
}

public class CuentasPorCobrarExcelService : ICuentasPorCobrarExcelService
{
    public byte[] GenerarReporteCuentasPorCobrar(
        IEnumerable<ReporteCuentasPorCobrarItemDto> items,
        ReporteCuentasPorCobrarFiltroDto filtro)
    {
        var lista = items.ToList();
        var tieneFiltroCliente = !string.IsNullOrWhiteSpace(filtro.ClienteNumDoc);

        // ── Título ────────────────────────────────────────────────────────────
        var titulo = !string.IsNullOrWhiteSpace(filtro.TituloReporte)
            ? filtro.TituloReporte
            : $"REPORTE DE CUENTAS POR COBRAR - RUC {filtro.EmpresaRuc}";

        var partesFiltro = new List<string> { $"RUC: {filtro.EmpresaRuc}" };
        if (!string.IsNullOrWhiteSpace(filtro.EstablecimientoAnexo))
            partesFiltro.Add($"Establecimiento: {filtro.EstablecimientoAnexo}");
        if (!string.IsNullOrWhiteSpace(filtro.ClienteNumDoc))
            partesFiltro.Add($"Cliente: {filtro.ClienteNumDoc}");
        if (filtro.FechaInicio.HasValue)
            partesFiltro.Add($"Desde: {filtro.FechaInicio:dd/MM/yyyy}");
        if (filtro.FechaFin.HasValue)
            partesFiltro.Add($"Hasta: {filtro.FechaFin:dd/MM/yyyy}");
        if (!string.IsNullOrWhiteSpace(filtro.Estado))
            partesFiltro.Add($"Estado: {filtro.Estado}");
        var subtitulo = string.Join("  |  ", partesFiltro);

        // ── Colores ───────────────────────────────────────────────────────────
        var colorEncabezado = XLColor.FromHtml("#2C3E6B");
        var colorCliente    = XLColor.FromHtml("#4A5568");
        var colorPagado     = XLColor.FromHtml("#EAF4EA");
        var colorPendiente  = XLColor.FromHtml("#FDEEF0");
        var colorCuota      = XLColor.FromHtml("#F7F7F7");
        var colorTotal      = XLColor.FromHtml("#E8ECF2");
        var colorBorde      = XLColor.FromHtml("#D1D5DB");

        var textoEncabezado = XLColor.White;
        var textoCliente    = XLColor.White;
        var textoPagado     = XLColor.FromHtml("#2D6A2D");
        var textoPendiente  = XLColor.FromHtml("#7A2D2D");
        var textoCuota      = XLColor.FromHtml("#555555");
        var textoTotal      = XLColor.FromHtml("#2C3E6B");

        // ── Columnas ──────────────────────────────────────────────────────────
        var columnas = new[]
        {
            ("N° Comprobante",   22),
            ("Tipo",             10),
            ("Fecha Emisión",    15),
            ("Cliente",          30),
            ("RUC / DNI",        16),
            ("Moneda",            9),
            ("Importe Total",    15),
            ("Monto Crédito",    15),
            ("Pagado",           14),
            ("Saldo",            14),
            ("Estado",           12),
        };
        int numCols = columnas.Length;

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Cuentas por Cobrar");

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

        // ── Helpers ───────────────────────────────────────────────────────────
        void EstilarFila(int row, XLColor bg, XLColor fg, bool bold = false)
        {
            for (int col = 1; col <= numCols; col++)
            {
                var cell = ws.Cell(row, col);
                cell.Style.Font.Bold                 = bold;
                cell.Style.Font.FontSize             = 10;
                cell.Style.Font.FontColor            = fg;
                cell.Style.Font.FontName             = "Arial";
                cell.Style.Fill.BackgroundColor      = bg;
                cell.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = colorBorde;
                cell.Style.Alignment.Vertical        = XLAlignmentVerticalValues.Center;
            }
            ws.Row(row).Height = 18;
        }

        void FormatoMoneda(int row, int col)
        {
            ws.Cell(row, col).Style.NumberFormat.Format  = "#,##0.00";
            ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }

        void EscribirComprobante(ReporteCuentasPorCobrarItemDto comp)
        {
            var colorComp = comp.Estado == "PAGADO" ? colorPagado : colorPendiente;
            var textoComp = comp.Estado == "PAGADO" ? textoPagado : textoPendiente;

            var montoPagado = comp.Cuotas.Sum(c => c.MontoPagado ?? 0);
            var saldo       = comp.Cuotas.Sum(c => c.Saldo ?? 0);

            EstilarFila(currentRow, colorComp, textoComp, bold: true);
            ws.Cell(currentRow, 1).Value  = comp.NumeroCompleto;
            ws.Cell(currentRow, 2).Value  = comp.TipoComprobante == "01" ? "Factura" : "Boleta";
            ws.Cell(currentRow, 3).Value  = comp.FechaEmision?.ToString("dd/MM/yyyy") ?? "-";
            ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(currentRow, 4).Value  = comp.ClienteRznSocial;
            ws.Cell(currentRow, 5).Value  = comp.ClienteNumDoc;
            ws.Cell(currentRow, 6).Value  = comp.TipoMoneda;
            ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(currentRow, 7).Value  = comp.ImporteTotal;
            ws.Cell(currentRow, 8).Value  = comp.MontoCredito ?? comp.ImporteTotal;
            ws.Cell(currentRow, 9).Value  = montoPagado;
            ws.Cell(currentRow, 10).Value = saldo;
            ws.Cell(currentRow, 11).Value = comp.Estado;
            ws.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            FormatoMoneda(currentRow, 7);
            FormatoMoneda(currentRow, 8);
            FormatoMoneda(currentRow, 9);
            FormatoMoneda(currentRow, 10);
            currentRow++;

            // Cuotas
            if (comp.Cuotas.Any())
            {
                foreach (var cuota in comp.Cuotas)
                {
                    EstilarFila(currentRow, colorCuota, textoCuota);
                    ws.Cell(currentRow, 1).Value  = "";
                    ws.Cell(currentRow, 2).Value  = $"  → {cuota.NumeroCuota}";
                    ws.Cell(currentRow, 3).Value  = cuota.FechaVencimiento.ToString("dd/MM/yyyy");
                    ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRow, 4).Value  = cuota.FechaPago.HasValue ? $"Pago: {cuota.FechaPago:dd/MM/yyyy}" : "Sin pago";
                    ws.Cell(currentRow, 5).Value  = "";
                    ws.Cell(currentRow, 6).Value  = "";
                    ws.Cell(currentRow, 7).Value  = cuota.Monto;
                    ws.Cell(currentRow, 8).Value  = "";
                    ws.Cell(currentRow, 9).Value  = cuota.MontoPagado ?? 0;
                    ws.Cell(currentRow, 10).Value = cuota.Saldo ?? 0;
                    ws.Cell(currentRow, 11).Value = cuota.Estado ?? "-";
                    ws.Cell(currentRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    FormatoMoneda(currentRow, 7);
                    FormatoMoneda(currentRow, 9);
                    FormatoMoneda(currentRow, 10);
                    currentRow++;
                }
            }
            else
            {
                EstilarFila(currentRow, colorCuota, textoCuota);
                var rangeSinCuotas = ws.Range(currentRow, 2, currentRow, numCols);
                rangeSinCuotas.Merge();
                rangeSinCuotas.Value = "  Sin cuotas registradas";
                rangeSinCuotas.Style.Font.Italic = true;
                currentRow++;
            }
        }

        // ── Datos ─────────────────────────────────────────────────────────────
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
                    EscribirComprobante(comp);

                // Espacio entre clientes
                ws.Row(currentRow).Height = 6;
                currentRow++;
            }
        }
        else
        {
            foreach (var comp in lista)
                EscribirComprobante(comp);
        }

        // ── Fila total ────────────────────────────────────────────────────────
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

        void CeldaTotalMoneda(int col, decimal valor)
        {
            var cell = ws.Cell(currentRow, col);
            cell.Value = valor;
            cell.Style.Font.Bold                 = true;
            cell.Style.Font.FontSize             = 10;
            cell.Style.Font.FontColor            = textoTotal;
            cell.Style.Font.FontName             = "Arial";
            cell.Style.Fill.BackgroundColor      = colorTotal;
            cell.Style.NumberFormat.Format       = "#,##0.00";
            cell.Style.Alignment.Horizontal      = XLAlignmentHorizontalValues.Right;
            cell.Style.Border.OutsideBorder      = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = colorBorde;
        }

        CeldaTotalMoneda(7, lista.Sum(x => x.ImporteTotal ?? 0));
        CeldaTotalMoneda(8, lista.Sum(x => x.MontoCredito ?? x.ImporteTotal ?? 0));
        CeldaTotalMoneda(9, lista.Sum(x => x.Cuotas.Sum(c => c.MontoPagado ?? 0)));
        CeldaTotalMoneda(10, lista.Sum(x => x.Cuotas.Sum(c => c.Saldo ?? 0)));

        var cellEstadoTotal = ws.Cell(currentRow, 11);
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