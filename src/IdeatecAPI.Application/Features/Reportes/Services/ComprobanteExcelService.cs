using ClosedXML.Excel;
using IdeatecAPI.Application.Features.Comprobante.DTOs;
using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Application.Features.Comprobante.Services;

public class ComprobanteExcelService : IComprobanteExcelService
{
    public Task<byte[]> ExportarListadoReportesAsync(
        string titulo,
        IEnumerable<ListarComprobanteDTO> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null)
    {
        var lista = datos.ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Comprobantes");

        // ── Título ────────────────────────────────────────────────────────────
        ws.Cell(1, 1).Value = titulo;
        ws.Range(1, 1, 1, 10).Merge();
        ws.Cell(1, 1).Style
            .Font.SetBold(true)
            .Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#2E75B6"))
            .Font.SetFontColor(XLColor.White);
        ws.Row(1).Height = 25;

        // ── Subtítulo filtros ─────────────────────────────────────────────────
        ws.Cell(2, 1).Value = BuildFiltros(ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
        ws.Range(2, 1, 2, 10).Merge();
        ws.Cell(2, 1).Style
            .Font.SetItalic(true)
            .Font.SetFontSize(9)
            .Font.SetFontColor(XLColor.Gray)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // ── Encabezados ───────────────────────────────────────────────────────
        var headers = new[]
        {
            "N° Comprobante", "Tipo", "F. Emisión", "Cliente", "Doc. Cliente",
            "Val. Venta", "IGV", "Importe Total", "Moneda", "Estado SUNAT"
        };
        SetHeaders(ws, 4, headers);

        // ── Datos ─────────────────────────────────────────────────────────────
        for (int i = 0; i < lista.Count; i++)
        {
            var fila = i + 5;
            var item = lista[i];
            var bgColor = i % 2 == 0 ? XLColor.White : XLColor.FromHtml("#EBF3FB");

            ws.Cell(fila, 1).Value  = item.NumeroCompleto;
            ws.Cell(fila, 2).Value  = item.TipoComprobante;
            ws.Cell(fila, 3).Value  = item.FechaEmision.ToString("dd/MM/yyyy");
            ws.Cell(fila, 4).Value  = item.Cliente?.RazonSocial;
            ws.Cell(fila, 5).Value  = item.Cliente?.NumeroDocumento;
            ws.Cell(fila, 6).Value  = item.ValorVenta;
            ws.Cell(fila, 7).Value  = item.TotalIGV;
            ws.Cell(fila, 8).Value  = item.ImporteTotal;
            ws.Cell(fila, 9).Value  = item.TipoMoneda;
            ws.Cell(fila, 10).Value = item.EstadoSunat;

            ws.Cell(fila, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(fila, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(fila, 8).Style.NumberFormat.Format = "#,##0.00";

            ws.Range(fila, 1, fila, 10).Style
                .Fill.SetBackgroundColor(bgColor)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Hair);
        }

        // ── Totales ───────────────────────────────────────────────────────────
        var filaTotal = lista.Count + 5;
        ws.Cell(filaTotal, 1).Value = "TOTAL";
        ws.Cell(filaTotal, 6).FormulaA1 = $"=SUM(F5:F{filaTotal - 1})";
        ws.Cell(filaTotal, 7).FormulaA1 = $"=SUM(G5:G{filaTotal - 1})";
        ws.Cell(filaTotal, 8).FormulaA1 = $"=SUM(H5:H{filaTotal - 1})";
        ws.Range(filaTotal, 1, filaTotal, 10).Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#BDD7EE"))
            .NumberFormat.SetFormat("#,##0.00")
            .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

        // ── Ancho columnas ────────────────────────────────────────────────────
        ws.Column(1).Width = 20; ws.Column(2).Width = 10;
        ws.Column(3).Width = 14; ws.Column(4).Width = 35;
        ws.Column(5).Width = 16; ws.Column(6).Width = 14;
        ws.Column(7).Width = 14; ws.Column(8).Width = 14;
        ws.Column(9).Width = 10; ws.Column(10).Width = 20;

        return Task.FromResult(ToBytes(wb));
    }

    public Task<byte[]> ExportarMediosPagoTopAsync(
        string titulo,
        IEnumerable<MedioPagoTopDTO> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null)
    {
        var lista = datos.ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Medios de Pago");

        // ── Título ────────────────────────────────────────────────────────────────
        ws.Cell(1, 1).Value = titulo;
        ws.Range(1, 1, 1, 4).Merge();
        ws.Cell(1, 1).Style
            .Font.SetBold(true)
            .Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#2E75B6"))
            .Font.SetFontColor(XLColor.White);
        ws.Row(1).Height = 25;

        // ── Subtítulo filtros ─────────────────────────────────────────────────────
        ws.Cell(2, 1).Value = BuildFiltros(ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
        ws.Range(2, 1, 2, 4).Merge();
        ws.Cell(2, 1).Style
            .Font.SetItalic(true)
            .Font.SetFontSize(9)
            .Font.SetFontColor(XLColor.Gray)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // ── Encabezados ───────────────────────────────────────────────────────────
        var headers = new[] { "Medio de Pago", "Veces Usado", "Monto Total", "Promedio Monto" };
        SetHeaders(ws, 4, headers);

        // ── Datos ─────────────────────────────────────────────────────────────────
        for (int i = 0; i < lista.Count; i++)
        {
            var fila = i + 5;
            var item = lista[i];
            var bgColor = i % 2 == 0 ? XLColor.White : XLColor.FromHtml("#EBF3FB");

            ws.Cell(fila, 1).Value = item.MedioPago;
            ws.Cell(fila, 2).Value = item.VecesUsado;
            ws.Cell(fila, 3).Value = item.MontoTotal;
            ws.Cell(fila, 4).Value = item.PromedioMonto;

            ws.Cell(fila, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(fila, 4).Style.NumberFormat.Format = "#,##0.00";

            ws.Range(fila, 1, fila, 4).Style
                .Fill.SetBackgroundColor(bgColor)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Hair);
        }

        // ── Totales ───────────────────────────────────────────────────────────────
        var filaTotal = lista.Count + 5;
        ws.Cell(filaTotal, 1).Value = "TOTAL";
        ws.Cell(filaTotal, 2).FormulaA1 = $"=SUM(B5:B{filaTotal - 1})";
        ws.Cell(filaTotal, 3).FormulaA1 = $"=SUM(C5:C{filaTotal - 1})";
        ws.Cell(filaTotal, 4).Value = "";

        ws.Range(filaTotal, 1, filaTotal, 4).Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#BDD7EE"))
            .NumberFormat.SetFormat("#,##0.00")
            .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

        // ── Ancho columnas ────────────────────────────────────────────────────────
        ws.Column(1).Width = 25;
        ws.Column(2).Width = 15;
        ws.Column(3).Width = 18;
        ws.Column(4).Width = 18;

        return Task.FromResult(ToBytes(wb));
    }

    public Task<byte[]> ExportarProductosTopAsync(
        string titulo,
        IEnumerable<ProductoTopDTO> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null)
    {
        var lista = datos.ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Productos");

        // ── Título ────────────────────────────────────────────────────────────
        ws.Cell(1, 1).Value = titulo;
        ws.Range(1, 1, 1, 7).Merge();
        ws.Cell(1, 1).Style
            .Font.SetBold(true)
            .Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#2E75B6"))
            .Font.SetFontColor(XLColor.White);
        ws.Row(1).Height = 25;

        // ── Subtítulo filtros ─────────────────────────────────────────────────
        ws.Cell(2, 1).Value = BuildFiltros(ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
        ws.Range(2, 1, 2, 7).Merge();
        ws.Cell(2, 1).Style
            .Font.SetItalic(true)
            .Font.SetFontSize(9)
            .Font.SetFontColor(XLColor.Gray)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // ── Encabezados ───────────────────────────────────────────────────────
        var headers = new[]
        {
            "Código", "Descripción", "Cant. Total", "Monto Total",
            "IGV Total", "Veces Vendido", "Precio Promedio"
        };
        SetHeaders(ws, 4, headers);

        // ── Datos ─────────────────────────────────────────────────────────────
        for (int i = 0; i < lista.Count; i++)
        {
            var fila = i + 5;
            var item = lista[i];
            var bgColor = i % 2 == 0 ? XLColor.White : XLColor.FromHtml("#EBF3FB");

            ws.Cell(fila, 1).Value = item.Codigo;
            ws.Cell(fila, 2).Value = item.Descripcion;
            ws.Cell(fila, 3).Value = item.TotalCantidad;
            ws.Cell(fila, 4).Value = item.TotalMonto;
            ws.Cell(fila, 5).Value = item.TotalIGV;
            ws.Cell(fila, 6).Value = item.VecesVendido;
            ws.Cell(fila, 7).Value = item.PrecioPromedio;

            ws.Cell(fila, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(fila, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(fila, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(fila, 7).Style.NumberFormat.Format = "#,##0.00";

            ws.Range(fila, 1, fila, 7).Style
                .Fill.SetBackgroundColor(bgColor)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Hair);
        }

        // ── Totales ───────────────────────────────────────────────────────────
        var filaTotal = lista.Count + 5;
        ws.Cell(filaTotal, 1).Value = "TOTAL";
        ws.Cell(filaTotal, 3).FormulaA1 = $"=SUM(C5:C{filaTotal - 1})";
        ws.Cell(filaTotal, 4).FormulaA1 = $"=SUM(D5:D{filaTotal - 1})";
        ws.Cell(filaTotal, 5).FormulaA1 = $"=SUM(E5:E{filaTotal - 1})";
        ws.Cell(filaTotal, 6).FormulaA1 = $"=SUM(F5:F{filaTotal - 1})";
        ws.Range(filaTotal, 1, filaTotal, 7).Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#BDD7EE"))
            .NumberFormat.SetFormat("#,##0.00")
            .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

        // ── Ancho columnas ────────────────────────────────────────────────────
        ws.Column(1).Width = 15; ws.Column(2).Width = 40;
        ws.Column(3).Width = 14; ws.Column(4).Width = 14;
        ws.Column(5).Width = 14; ws.Column(6).Width = 16;
        ws.Column(7).Width = 16;

        return Task.FromResult(ToBytes(wb));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static void SetHeaders(IXLWorksheet ws, int fila, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(fila, i + 1).Value = headers[i];
            ws.Cell(fila, i + 1).Style
                .Font.SetBold(true)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#2E75B6"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }
        ws.Row(fila).Height = 18;
    }

    private static string BuildFiltros(string ruc, string? codEstablecimiento,
        DateTime? fechaDesde, DateTime? fechaHasta, int? usuarioCreacion, string? clienteNumDoc)
    {
        var filtros = $"RUC: {ruc}";
        if (!string.IsNullOrEmpty(codEstablecimiento)) filtros += $" | Sucursal: {codEstablecimiento}";
        if (fechaDesde.HasValue) filtros += $" | Desde: {fechaDesde.Value:dd/MM/yyyy}";
        if (fechaHasta.HasValue) filtros += $" | Hasta: {fechaHasta.Value:dd/MM/yyyy}";
        if (usuarioCreacion.HasValue) filtros += $" | Usuario: {usuarioCreacion}";
        if (!string.IsNullOrEmpty(clienteNumDoc)) filtros += $" | Cliente: {clienteNumDoc}";
        return filtros;
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}