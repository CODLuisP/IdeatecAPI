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

    // ── Separar en grupos ─────────────────────────────────────────────────────
    var numerosEnPeriodo = lista
        .Where(x => x.TipoComprobante != "07" && x.TipoComprobante != "08")
        .Select(x => x.NumeroCompleto?.Trim().ToUpper())
        .Where(x => !string.IsNullOrEmpty(x))
        .ToHashSet();

    var listaRechazados = lista
        .Where(x => x.EstadoSunat == "RECHAZADO")
        .ToList();

    var listaValidos = lista
        .Where(x => x.EstadoSunat != "RECHAZADO")
        .ToList();

    var listaPeriodo = listaValidos.Where(x =>
        (x.TipoComprobante != "07" && x.TipoComprobante != "08")
        || (
            (x.TipoComprobante == "07" || x.TipoComprobante == "08")
            && !string.IsNullOrEmpty(x.NumDocAfectado)
            && numerosEnPeriodo.Contains(x.NumDocAfectado?.Trim().ToUpper())
        )
    ).ToList();

    var listaFueraPeriodo = listaValidos.Where(x =>
        (x.TipoComprobante == "07" || x.TipoComprobante == "08")
        && (
            string.IsNullOrEmpty(x.NumDocAfectado)
            || !numerosEnPeriodo.Contains(x.NumDocAfectado?.Trim().ToUpper())
        )
    ).ToList();

    using var wb = new XLWorkbook();
    var ws = wb.Worksheets.Add("Comprobantes");

    // ── Título ────────────────────────────────────────────────────────────────
    ws.Cell(1, 1).Value = titulo;
    ws.Range(1, 1, 1, 12).Merge();
    ws.Cell(1, 1).Style
        .Font.SetBold(true)
        .Font.SetFontSize(14)
        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
        .Fill.SetBackgroundColor(XLColor.FromHtml("#2E75B6"))
        .Font.SetFontColor(XLColor.White);
    ws.Row(1).Height = 25;

    // ── Subtítulo filtros ─────────────────────────────────────────────────────
    ws.Cell(2, 1).Value = BuildFiltros(ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
    ws.Range(2, 1, 2, 12).Merge();
    ws.Cell(2, 1).Style
        .Font.SetItalic(true)
        .Font.SetFontSize(9)
        .Font.SetFontColor(XLColor.Gray)
        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

    var headers = new[]
    {
        "N° Comprobante", "Tipo", "F. Emisión", "Cliente", "Doc. Cliente",
        "Val. Venta", "IGV", "Importe Total", "Moneda", "Estado SUNAT",
        "Doc. Afectado", "Tipo Pago"
    };

    int filaActual = 4;

    // ═════════════════════════════════════════════════════════════════════════
    // SECCIÓN 1 — Ventas del período (aceptados + anulados)
    // ═════════════════════════════════════════════════════════════════════════
    ws.Cell(filaActual, 1).Value = "VENTAS DEL PERÍODO";
    ws.Range(filaActual, 1, filaActual, 12).Merge();
    ws.Cell(filaActual, 1).Style
        .Font.SetBold(true)
        .Font.SetFontSize(10)
        .Font.SetFontColor(XLColor.White)
        .Fill.SetBackgroundColor(XLColor.FromHtml("#2E75B6"))
        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
    filaActual++;

    SetHeaders(ws, filaActual, headers);
    filaActual++;

    int primeraFilaSeccion1 = filaActual;

    foreach (var item in listaPeriodo)
    {
        EscribirFila(ws, filaActual, item, listaPeriodo.IndexOf(item));
        filaActual++;
    }

    int ultimaFilaSeccion1 = filaActual - 1;

    // Total sección 1
    ws.Cell(filaActual, 1).Value = "TOTAL NETO DEL PERÍODO";
    ws.Cell(filaActual, 6).FormulaA1 = $"=SUM(F{primeraFilaSeccion1}:F{ultimaFilaSeccion1})";
    ws.Cell(filaActual, 7).FormulaA1 = $"=SUM(G{primeraFilaSeccion1}:G{ultimaFilaSeccion1})";
    ws.Cell(filaActual, 8).FormulaA1 = $"=SUM(H{primeraFilaSeccion1}:H{ultimaFilaSeccion1})";
    ws.Range(filaActual, 1, filaActual, 12).Style
        .Font.SetBold(true)
        .Fill.SetBackgroundColor(XLColor.FromHtml("#BDD7EE"))
        .NumberFormat.SetFormat("#,##0.00")
        .Border.SetOutsideBorder(XLBorderStyleValues.Medium);
    filaActual++;

    // Leyenda sección 1
    ws.Cell(filaActual, 1).Value = "N. Credito (resta al total)";
    ws.Range(filaActual, 1, filaActual, 6).Merge();
    ws.Cell(filaActual, 1).Style
        .Font.SetItalic(true)
        .Font.SetFontSize(8)
        .Font.SetFontColor(XLColor.Red)
        .Fill.SetBackgroundColor(XLColor.FromHtml("#FFF2CC"));

    ws.Cell(filaActual, 7).Value = "N. Debito (suma al total)";
    ws.Range(filaActual, 7, filaActual, 12).Merge();
    ws.Cell(filaActual, 7).Style
        .Font.SetItalic(true)
        .Font.SetFontSize(8)
        .Font.SetFontColor(XLColor.FromHtml("#375623"))
        .Fill.SetBackgroundColor(XLColor.FromHtml("#E2EFDA"));
    filaActual += 2;

    // ═════════════════════════════════════════════════════════════════════════
    // SECCIÓN 2 — Ajustes fuera del período
    // ═════════════════════════════════════════════════════════════════════════
    if (listaFueraPeriodo.Any())
    {
        ws.Cell(filaActual, 1).Value = "AJUSTES DE OTROS PERÍODOS (no afectan el total anterior)";
        ws.Range(filaActual, 1, filaActual, 12).Merge();
        ws.Cell(filaActual, 1).Style
            .Font.SetBold(true)
            .Font.SetFontSize(10)
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#7030A0"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
        filaActual++;

        SetHeaders(ws, filaActual, headers);
        filaActual++;

        int primeraFilaSeccion2 = filaActual;

        foreach (var item in listaFueraPeriodo)
        {
            EscribirFila(ws, filaActual, item, listaFueraPeriodo.IndexOf(item));
            filaActual++;
        }

        int ultimaFilaSeccion2 = filaActual - 1;

        ws.Cell(filaActual, 1).Value = "TOTAL AJUSTES";
        ws.Cell(filaActual, 6).FormulaA1 = $"=SUM(F{primeraFilaSeccion2}:F{ultimaFilaSeccion2})";
        ws.Cell(filaActual, 7).FormulaA1 = $"=SUM(G{primeraFilaSeccion2}:G{ultimaFilaSeccion2})";
        ws.Cell(filaActual, 8).FormulaA1 = $"=SUM(H{primeraFilaSeccion2}:H{ultimaFilaSeccion2})";
        ws.Range(filaActual, 1, filaActual, 12).Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#E2CFED"))
            .NumberFormat.SetFormat("#,##0.00")
            .Border.SetOutsideBorder(XLBorderStyleValues.Medium);
        filaActual++;

        ws.Cell(filaActual, 1).Value = "Estas notas afectan comprobantes emitidos en otros períodos y no se incluyen en el total del período.";
        ws.Range(filaActual, 1, filaActual, 12).Merge();
        ws.Cell(filaActual, 1).Style
            .Font.SetItalic(true)
            .Font.SetFontSize(8)
            .Font.SetFontColor(XLColor.FromHtml("#7030A0"));
        filaActual += 2;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SECCIÓN 3 — Rechazados (solo informativo)
    // ═════════════════════════════════════════════════════════════════════════
    if (listaRechazados.Any())
    {
        ws.Cell(filaActual, 1).Value = "RECHAZADOS (solo informativo, no afectan totales)";
        ws.Range(filaActual, 1, filaActual, 12).Merge();
        ws.Cell(filaActual, 1).Style
            .Font.SetBold(true)
            .Font.SetFontSize(10)
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#808080"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
        filaActual++;

        SetHeaders(ws, filaActual, headers);
        filaActual++;

        foreach (var item in listaRechazados)
        {
            EscribirFila(ws, filaActual, item, listaRechazados.IndexOf(item));
            filaActual++;
        }

        ws.Cell(filaActual, 1).Value = "Estos comprobantes fueron rechazados por SUNAT y no tienen efecto contable.";
        ws.Range(filaActual, 1, filaActual, 12).Merge();
        ws.Cell(filaActual, 1).Style
            .Font.SetItalic(true)
            .Font.SetFontSize(8)
            .Font.SetFontColor(XLColor.Gray);
    }

    // ── Ancho columnas ────────────────────────────────────────────────────────
    ws.Column(1).Width = 20; ws.Column(2).Width = 12;
    ws.Column(3).Width = 14; ws.Column(4).Width = 35;
    ws.Column(5).Width = 16; ws.Column(6).Width = 14;
    ws.Column(7).Width = 14; ws.Column(8).Width = 14;
    ws.Column(9).Width = 10; ws.Column(10).Width = 20;
    ws.Column(11).Width = 20; ws.Column(12).Width = 12;

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
            
        // ── Nota informativa ──────────────────────────────────────────────────────────
        var filaNota = lista.Count + 7;
        ws.Cell(filaNota, 1).Value = "Nota: Los PENDIENTES, ANULADOS y RECHAZADOS no afectan este reporte.";        
        ws.Range(filaNota, 1, filaNota, 4).Merge();
        ws.Cell(filaNota, 1).Style
            .Font.SetItalic(true)
            .Font.SetFontSize(8)
            .Font.SetFontColor(XLColor.FromHtml("#7F6000"))
            .Fill.SetBackgroundColor(XLColor.FromHtml("#FFF2CC"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
            .Alignment.SetWrapText(true);
        ws.Row(filaNota).Height = 25;

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

    public Task<byte[]> ExportarControlCajaAsync(
        string titulo,
        IEnumerable<ListarComprobanteDTO> datos,
        string ruc,
        string? codEstablecimiento = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        int? usuarioCreacion = null,
        string? clienteNumDoc = null)
    {
        var movimientos = datos.ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Control de Caja");

        // ── Título ────────────────────────────────────────────────────────────────
        ws.Cell(1, 1).Value = titulo;
        ws.Range(1, 1, 1, 13).Merge();
        ws.Cell(1, 1).Style
            .Font.SetBold(true)
            .Font.SetFontSize(14)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#1F7A4D"))
            .Font.SetFontColor(XLColor.White);
        ws.Row(1).Height = 25;

        // ── Subtítulo filtros ─────────────────────────────────────────────────────
        ws.Cell(2, 1).Value = BuildFiltros(ruc, codEstablecimiento, fechaDesde, fechaHasta, usuarioCreacion, clienteNumDoc);
        ws.Range(2, 1, 2, 13).Merge();
        ws.Cell(2, 1).Style
            .Font.SetItalic(true)
            .Font.SetFontSize(9)
            .Font.SetFontColor(XLColor.Gray)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        var headers = new[]
        {
            "N° Comprobante", "Tipo", "F. Emisión", "Cliente", "Doc. Cliente",
            "Val. Venta", "IGV", "Importe Total", "Moneda", "Estado SUNAT",
            "Doc. Afectado", "Tipo Pago", "Monto Crédito"
        };

        int filaActual = 4;

        // ═════════════════════════════════════════════════════════════════════════
        // SECCIÓN — Movimientos del período
        // ═════════════════════════════════════════════════════════════════════════
        ws.Cell(filaActual, 1).Value = "MOVIMIENTOS DEL PERÍODO";
        ws.Range(filaActual, 1, filaActual, 13).Merge();
        ws.Cell(filaActual, 1).Style
            .Font.SetBold(true)
            .Font.SetFontSize(10)
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#1F7A4D"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
        filaActual++;

        SetHeaders(ws, filaActual, headers);
        filaActual++;

        int primeraFila = filaActual;

        foreach (var item in movimientos)
        {
            EscribirFilaControlCaja(ws, filaActual, item, movimientos.IndexOf(item));
            filaActual++;
        }

        int ultimaFila = filaActual - 1;

        // ── Total del período ─────────────────────────────────────────────────────
        ws.Cell(filaActual, 1).Value = "TOTAL";
        ws.Cell(filaActual, 6).FormulaA1 = $"=SUM(F{primeraFila}:F{ultimaFila})";
        ws.Cell(filaActual, 7).FormulaA1 = $"=SUM(G{primeraFila}:G{ultimaFila})";
        ws.Cell(filaActual, 8).FormulaA1 = $"=SUM(H{primeraFila}:H{ultimaFila})";
        ws.Range(filaActual, 1, filaActual, 13).Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#C6EFCE"))
            .NumberFormat.SetFormat("#,##0.00")
            .Border.SetOutsideBorder(XLBorderStyleValues.Medium);

        // ── Aviso saldo negativo (solo considera movimientos del período) ─────────
        var totalImporte = movimientos
            .Where(x => x.TipoComprobante != "07")
            .Sum(x => x.ImporteTotal)
            - movimientos
            .Where(x => x.TipoComprobante == "07")
            .Sum(x => x.ImporteTotal);

        if (totalImporte < 0)
        {
            filaActual += 2;
            ws.Cell(filaActual, 1).Value = "⚠ SALDO NEGATIVO — Revisar notas de crédito del período";
            ws.Range(filaActual, 1, filaActual, 13).Merge();
            ws.Cell(filaActual, 1).Style
                .Font.SetBold(true)
                .Font.SetFontSize(10)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.Red)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        // ── Ancho columnas ────────────────────────────────────────────────────────
        ws.Column(1).Width = 20; ws.Column(2).Width = 12;
        ws.Column(3).Width = 14; ws.Column(4).Width = 35;
        ws.Column(5).Width = 16; ws.Column(6).Width = 14;
        ws.Column(7).Width = 14; ws.Column(8).Width = 14;
        ws.Column(9).Width = 10; ws.Column(10).Width = 20;
        ws.Column(11).Width = 20; ws.Column(12).Width = 12;
        ws.Column(13).Width = 14;

        return Task.FromResult(ToBytes(wb));
    }

    private static void EscribirFilaControlCaja(IXLWorksheet ws, int fila, ListarComprobanteDTO item, int index)
    {
        var bgColor = item.TipoComprobante switch
        {
            "07" => XLColor.FromHtml("#FFF2CC"),
            "08" => XLColor.FromHtml("#E2EFDA"),
            _    => index % 2 == 0 ? XLColor.White : XLColor.FromHtml("#EBF3FB")
        };

        // Estados con color especial
        var esRechazado2 = item.EstadoSunat == "RECHAZADO";
        if (esRechazado2)
            bgColor = XLColor.FromHtml("#F2F2F2");
        else if (item.EstadoSunat == "ANULADO")
            bgColor = XLColor.FromHtml("#D9D9D9");
        else if (item.EstadoSunat == "PENDIENTE")
            bgColor = XLColor.FromHtml("#FFF9C4");

        var tc2 = (item.TipoMoneda == "USD" && item.TipoCambio > 0) ? item.TipoCambio : 1m;
        bool esUsd2 = tc2 != 1m;
        static decimal Conv2(decimal v, decimal t, bool usd) =>
            Math.Round(v * t, 2);
        var valorVenta = Conv2((item.TipoComprobante == "07" ? -item.ValorVenta   : item.ValorVenta),   tc2, esUsd2);
        var igv        = Conv2((item.TipoComprobante == "07" ? -item.TotalIGV     : item.TotalIGV),     tc2, esUsd2);
        var importe    = Conv2((item.TipoComprobante == "07" ? -item.ImporteTotal : item.ImporteTotal), tc2, esUsd2);

        ws.Cell(fila, 1).Value  = item.NumeroCompleto;
        ws.Cell(fila, 2).Value  = item.TipoComprobante switch
        {
            "01" => "Factura",
            "03" => "Boleta",
            "07" => "N. Credito",
            "08" => "N. Debito",
            "NV" => "Nota de Venta",
            _    => item.TipoComprobante
        };
        ws.Cell(fila, 3).Value  = item.FechaEmision.ToString("dd/MM/yyyy");
        ws.Cell(fila, 4).Value  = item.Cliente?.RazonSocial;
        ws.Cell(fila, 5).Value  = item.Cliente?.NumeroDocumento;
        ws.Cell(fila, 6).Value  = valorVenta;
        ws.Cell(fila, 7).Value  = igv;
        ws.Cell(fila, 8).Value  = importe;
        ws.Cell(fila, 9).Value  = (item.TipoMoneda == "USD" && item.TipoCambio > 0)
            ? $"USD ({item.TipoCambio:F3})"
            : item.TipoMoneda;
        ws.Cell(fila, 10).Value = item.EstadoSunat;
        ws.Cell(fila, 11).Value = !string.IsNullOrEmpty(item.NumDocAfectado) ? item.NumDocAfectado : "-";
        ws.Cell(fila, 12).Value = item.TipoPago switch
        {
            "Contado"           => "Contado",
            "Credito"           => "Crédito",
            "Credito con Inicial" => "Créd. con Inicial",
            null or ""          => "-",
            _                   => item.TipoPago
        };
        if (item.MontoCredito > 0)
        {
            ws.Cell(fila, 13).Value = item.MontoCredito;
            ws.Cell(fila, 13).Style.NumberFormat.Format = "#,##0.00";
        }
        else
        {
            ws.Cell(fila, 13).Value = "-";
        }

        ws.Cell(fila, 6).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(fila, 7).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(fila, 8).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(fila, 13).Style.NumberFormat.Format = "#,##0.00";

        if (item.TipoComprobante == "07")
        {
            ws.Cell(fila, 6).Style.Font.SetFontColor(XLColor.Red);
            ws.Cell(fila, 7).Style.Font.SetFontColor(XLColor.Red);
            ws.Cell(fila, 8).Style.Font.SetFontColor(XLColor.Red);
        }

        if (esRechazado2)
            ws.Range(fila, 1, fila, 13).Style.Font.SetFontColor(XLColor.Gray);

        ws.Range(fila, 1, fila, 13).Style
            .Fill.SetBackgroundColor(bgColor)
            .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorder(XLBorderStyleValues.Hair);
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

private static void EscribirFila(IXLWorksheet ws, int fila, ListarComprobanteDTO item, int index)
{
    var esRechazado = item.EstadoSunat == "RECHAZADO";

    var bgColor = esRechazado
        ? XLColor.FromHtml("#F2F2F2")
        : item.EstadoSunat == "ANULADO"
            ? XLColor.FromHtml("#D9D9D9")
            : item.TipoComprobante switch
            {
                "07" => XLColor.FromHtml("#FFF2CC"),
                "08" => XLColor.FromHtml("#E2EFDA"),
                _    => index % 2 == 0 ? XLColor.White : XLColor.FromHtml("#EBF3FB")
            };

    var tc = (item.TipoMoneda == "USD" && item.TipoCambio > 0) ? item.TipoCambio : 1m;
    bool esUsd = tc != 1m;
    static decimal Conv(decimal v, decimal t, bool usd) =>
        Math.Round(v * t, 2);
    var valorVenta = Conv((!esRechazado && item.TipoComprobante == "07") ? -item.ValorVenta   : item.ValorVenta,   tc, esUsd);
    var igv        = Conv((!esRechazado && item.TipoComprobante == "07") ? -item.TotalIGV     : item.TotalIGV,     tc, esUsd);
    var importe    = Conv((!esRechazado && item.TipoComprobante == "07") ? -item.ImporteTotal : item.ImporteTotal, tc, esUsd);

    ws.Cell(fila, 1).Value  = item.NumeroCompleto;
    ws.Cell(fila, 2).Value  = item.TipoComprobante switch
    {
        "01" => "Factura",
        "03" => "Boleta",
        "07" => "N. Credito",
        "08" => "N. Debito",
        "NV" => "Nota de Venta",
        _    => item.TipoComprobante
    };
    ws.Cell(fila, 3).Value  = item.FechaEmision.ToString("dd/MM/yyyy");
    ws.Cell(fila, 4).Value  = item.Cliente?.RazonSocial;
    ws.Cell(fila, 5).Value  = item.Cliente?.NumeroDocumento;
    ws.Cell(fila, 6).Value  = valorVenta;
    ws.Cell(fila, 7).Value  = igv;
    ws.Cell(fila, 8).Value  = importe;
    ws.Cell(fila, 9).Value  = (item.TipoMoneda == "USD" && item.TipoCambio > 0)
        ? $"USD ({item.TipoCambio:F3})"
        : item.TipoMoneda;
    ws.Cell(fila, 10).Value = item.EstadoSunat;
    ws.Cell(fila, 11).Value = !string.IsNullOrEmpty(item.NumDocAfectado) ? item.NumDocAfectado : "-";
    ws.Cell(fila, 12).Value = item.TipoPago switch
    {
        "Contado" => "Contado",
        "Credito" => "Crédito",
        null or "" => "-",
        _ => item.TipoPago
    };

    ws.Cell(fila, 6).Style.NumberFormat.Format = "#,##0.00";
    ws.Cell(fila, 7).Style.NumberFormat.Format = "#,##0.00";
    ws.Cell(fila, 8).Style.NumberFormat.Format = "#,##0.00";

    if (!esRechazado && item.TipoComprobante == "07")
    {
        ws.Cell(fila, 6).Style.Font.SetFontColor(XLColor.Red);
        ws.Cell(fila, 7).Style.Font.SetFontColor(XLColor.Red);
        ws.Cell(fila, 8).Style.Font.SetFontColor(XLColor.Red);
    }

    if (esRechazado)
        ws.Range(fila, 1, fila, 12).Style.Font.SetFontColor(XLColor.Gray);

    ws.Range(fila, 1, fila, 12).Style
        .Fill.SetBackgroundColor(bgColor)
        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
        .Border.SetInsideBorder(XLBorderStyleValues.Hair);
}
    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}