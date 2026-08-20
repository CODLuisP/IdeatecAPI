using ClosedXML.Excel;
using IdeatecAPI.Application.Features.Reportes.DTOs;

namespace IdeatecAPI.Application.Features.Reportes.Services;

public interface IDashboardReportExcelService
{
    Task<byte[]> ExportarAsync(DashboardReportDto data);
}

// Replica el mismo contenido y orden del DashboardReportPdfService, pero con una paleta
// propia del Excel: azul marino sobrio y de baja saturación (a diferencia del PDF, que usa
// un color distinto por sección). Solo se pintan encabezados; las filas de datos quedan
// en blanco/gris zebra para que la hoja se lea como una tabla, no como un mosaico de colores.
public class DashboardReportExcelService : IDashboardReportExcelService
{
    private const string NavyHex     = "#1E3A5F";   // franjas de título de sección + cabecera KPI
    private const string NavyTextHex = "#1E3A5F";   // texto de los th
    private const string ThBgHex     = "#E2E8F0";   // fondo de los encabezados de columna
    private const string GrisHex     = "#F8FAFC";   // zebra de filas de datos

    public Task<byte[]> ExportarAsync(DashboardReportDto d)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Dashboard");

        var esSoloDia   = d.FechaDesde.Date == d.FechaHasta.Date;
        var labelVentas = esSoloDia ? "Ventas del Día" : "Ventas del Período";
        var periodoStr = esSoloDia
            ? d.FechaDesde.ToString("dd/MM/yyyy")
            : $"{d.FechaDesde:dd/MM/yyyy} al {d.FechaHasta:dd/MM/yyyy}";

        int row = 1;
        const int maxCol = 7;

        // ── Cabecera ─────────────────────────────────────────────────────────
        ws.Cell(row, 1).Value = d.EmpresaRazonSocial;
        ws.Range(row, 1, row, maxCol).Merge().Style
            .Font.SetBold(true).Font.SetFontSize(14)
            .Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(NavyHex))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Row(row).Height = 28;
        row++;

        ws.Cell(row, 1).Value = $"RUC: {d.EmpresaRuc}   |   Período: {periodoStr}" +
            (!string.IsNullOrEmpty(d.NombreSucursal) ? $"   |   Sucursal: {d.NombreSucursal}" : "") +
            (!string.IsNullOrEmpty(d.NombreUsuario) ? $"   |   Usuario: {d.NombreUsuario}" : "");
        ws.Range(row, 1, row, maxCol).Merge().Style
            .Font.SetItalic(true).Font.SetFontSize(9)
            .Font.SetFontColor(XLColor.Gray)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        row++;

        ws.Cell(row, 1).Value = $"Generado: {d.FechaGeneracion:dd/MM/yyyy HH:mm}";
        ws.Range(row, 1, row, maxCol).Merge().Style
            .Font.SetFontSize(8).Font.SetFontColor(XLColor.Gray)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        row += 2;

        // ── Helpers ──────────────────────────────────────────────────────────
        void SeccionTitulo(ref int r, string titulo, int cols)
        {
            ws.Cell(r, 1).Value = titulo;
            ws.Range(r, 1, r, cols).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml(NavyHex));
            ws.Row(r).Height = 22;
            r++;
        }

        void HeaderRow(ref int r, params string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(r, i + 1).Value = headers[i];
                ws.Cell(r, i + 1).Style
                    .Font.SetBold(true).Font.SetFontSize(9)
                    .Font.SetFontColor(XLColor.FromHtml(NavyTextHex))
                    .Fill.SetBackgroundColor(XLColor.FromHtml(ThBgHex));
            }
            r++;
        }

        void Zebra(int r, int cols)
        {
            if (r % 2 == 0)
                ws.Range(r, 1, r, cols).Style.Fill.SetBackgroundColor(XLColor.FromHtml(GrisHex));
        }

        // ── KPIs principales (tabla simple, sin filas pintadas de color) ─────
        SeccionTitulo(ref row, "Indicadores Clave (KPIs)", 2);
        HeaderRow(ref row, "Indicador", "Valor");

        void KpiRow(ref int r, string label, string valor)
        {
            ws.Cell(r, 1).Value = label;
            ws.Cell(r, 2).Value = valor;
            ws.Cell(r, 2).Style.Font.SetBold(true).Font.SetFontColor(XLColor.FromHtml(NavyHex));
            Zebra(r, 2);
            r++;
        }

        KpiRow(ref row, labelVentas,      $"S/ {d.VentasTotal:N2}");
        KpiRow(ref row, "Total IGV",      $"S/ {d.TotalIGV:N2}");
        KpiRow(ref row, "Documentos",     d.TotalDocumentos.ToString("N0"));
        KpiRow(ref row, "Ganancias",      $"S/ {d.Ganancias:N2}");
        KpiRow(ref row, "% de Ganancias", $"{d.PorcentajeGanancias:N1}%");
        KpiRow(ref row, "Comisión POS",   $"S/ {d.TotalComisionPOS:N2}");
        row++;

        // ── Distribución de Documentos (orden de mayor a menor, igual al donut) ─
        SeccionTitulo(ref row, "Distribución de Documentos", 3);
        HeaderRow(ref row, "Tipo", "Cantidad", "Porcentaje");

        var dist = d.Distribucion;
        var segs = new (string L, int C)[]
        {
            ("Facturas", dist.Facturas),
            ("Boletas", dist.Boletas),
            ("N. Crédito", dist.NotasCredito),
            ("N. Débito", dist.NotasDebito),
            ("N. Venta", dist.NotasVenta),
        }
        .Where(s => s.C > 0)
        .OrderByDescending(s => s.C)
        .ToArray();
        var totalDist = segs.Sum(s => s.C);

        foreach (var s in segs)
        {
            ws.Cell(row, 1).Value = s.L;
            ws.Cell(row, 2).Value = s.C;
            ws.Cell(row, 3).Value = totalDist > 0 ? (decimal)s.C / totalDist : 0;
            ws.Cell(row, 3).Style.NumberFormat.Format = "0.0%";
            Zebra(row, 3);
            row++;
        }

        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 2).Value = totalDist;
        ws.Range(row, 1, row, 3).Style
            .Font.SetBold(true).Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(NavyHex));
        row += 2;

        // ── Ventas por Período (día único ⇒ Ventas por Usuario) ─────────────
        if (esSoloDia)
        {
            if (d.VentasPorUsuario.Any())
            {
                SeccionTitulo(ref row, "Ventas por Usuario", 4);
                HeaderRow(ref row, "#", "Usuario", "Documentos", "Total Ventas");

                int i = 0;
                foreach (var u in d.VentasPorUsuario)
                {
                    i++;
                    ws.Cell(row, 1).Value = i;
                    ws.Cell(row, 2).Value = u.NombreUsuario;
                    ws.Cell(row, 3).Value = u.TotalDocumentos;
                    ws.Cell(row, 4).Value = u.TotalVentas;
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                    Zebra(row, 4);
                    row++;
                }
                row++;
            }
        }
        else if (d.Grafico.Any())
        {
            SeccionTitulo(ref row, "Ventas por Período", 4);
            HeaderRow(ref row, "Fecha", "Ventas", "IGV", "Notas Venta");

            foreach (var g in d.Grafico)
            {
                ws.Cell(row, 1).Value = g.Etiqueta;
                ws.Cell(row, 2).Value = g.Ventas;
                ws.Cell(row, 3).Value = g.Igv;
                ws.Cell(row, 4).Value = g.VentasNV;
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                Zebra(row, 4);
                row++;
            }
            row++;
        }

        // ── Top Comprobantes (facturas + boletas + notas de venta unificado) ─
        var todosComp = d.TopComprobantes.Concat(d.TopNotasVenta)
            .OrderByDescending(c => c.ImporteTotal)
            .Take(5)
            .ToList();
        if (todosComp.Any())
        {
            SeccionTitulo(ref row, "Top Comprobantes", 5);
            HeaderRow(ref row, "#", "Comprobante", "Cliente", "Fecha", "Importe");

            int i = 0;
            foreach (var c in todosComp)
            {
                i++;
                ws.Cell(row, 1).Value = i;
                ws.Cell(row, 2).Value = c.NumeroCompleto;
                ws.Cell(row, 3).Value = c.ClienteRznSocial;
                ws.Cell(row, 4).Value = c.FechaEmision.ToString("dd/MM/yyyy");
                ws.Cell(row, 5).Value = c.ImporteTotal;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                Zebra(row, 5);
                row++;
            }
            row++;
        }

        // ── Medios de Pago ───────────────────────────────────────────────────
        var medios = d.MediosPago.OrderByDescending(m => m.MontoTotal).Take(5).ToList();
        if (medios.Any())
        {
            SeccionTitulo(ref row, "Medios de Pago", 4);
            HeaderRow(ref row, "#", "Medio de Pago", "Usos", "Monto");

            int i = 0;
            foreach (var mp in medios)
            {
                i++;
                ws.Cell(row, 1).Value = i;
                ws.Cell(row, 2).Value = mp.MedioPago ?? "Sin especificar";
                ws.Cell(row, 3).Value = mp.VecesUsado;
                ws.Cell(row, 4).Value = mp.MontoTotal;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                Zebra(row, 4);
                row++;
            }
            row++;
        }

        // ── Clientes con más Ventas ──────────────────────────────────────────
        var clientes = d.TopClientes.Take(5).ToList();
        if (clientes.Any())
        {
            SeccionTitulo(ref row, "Clientes con más Ventas", 5);
            HeaderRow(ref row, "#", "Cliente", "Doc.", "Cant.", "Total");

            int i = 0;
            foreach (var cl in clientes)
            {
                i++;
                ws.Cell(row, 1).Value = i;
                ws.Cell(row, 2).Value = cl.ClienteRznSocial;
                ws.Cell(row, 3).Value = cl.ClienteNumDoc;
                ws.Cell(row, 4).Value = cl.NumDocs;
                ws.Cell(row, 5).Value = cl.Total;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                Zebra(row, 5);
                row++;
            }
            row++;
        }

        // ── Comisión Tarjeta ─────────────────────────────────────────────────
        var comision = d.TopComisionTarjeta.Take(5).ToList();
        if (comision.Any())
        {
            SeccionTitulo(ref row, "Comisión Tarjeta", 3);
            HeaderRow(ref row, "#", "Cliente", "Comisión");

            int i = 0;
            foreach (var ct in comision)
            {
                i++;
                ws.Cell(row, 1).Value = i;
                ws.Cell(row, 2).Value = ct.ClienteRznSocial;
                ws.Cell(row, 3).Value = ct.ComisionTarjeta;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                Zebra(row, 3);
                row++;
            }
            row++;
        }

        // ── Productos más Vendidos (ordenados por cantidad, igual al PDF) ────
        var productos = d.TopProductos.Take(10).ToList();
        if (productos.Any())
        {
            SeccionTitulo(ref row, "Productos más Vendidos", 4);
            HeaderRow(ref row, "#", "Producto", "Cantidad", "Monto");

            int i = 0;
            foreach (var p in productos)
            {
                i++;
                ws.Cell(row, 1).Value = i;
                ws.Cell(row, 2).Value = p.Descripcion ?? "—";
                ws.Cell(row, 3).Value = p.TotalCantidad;
                ws.Cell(row, 4).Value = p.TotalMonto;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                Zebra(row, 4);
                row++;
            }
            row++;
        }

        // ── Nota de comisión (solo si hubo comisión de tarjeta en el período) ─
        if (d.TotalComisionPOS > 0)
        {
            ws.Cell(row, 1).Value = "* Algunos comprobantes incluyen su comisión de tarjeta en el monto mostrado; esta comisión no es declarada ante SUNAT.";
            ws.Range(row, 1, row, maxCol).Merge().Style
                .Font.SetItalic(true).Font.SetFontSize(8)
                .Font.SetFontColor(XLColor.Gray);
            row++;
        }

        // ── Autoajustar columnas (con ancho mínimo para nombres largos) ──────
        // Col. B: Cliente/Producto en la mayoría de tablas. Col. C: Cliente en Top Comprobantes.
        ws.Columns().AdjustToContents();
        if (ws.Column(2).Width < 30) ws.Column(2).Width = 30;
        if (ws.Column(3).Width < 24) ws.Column(3).Width = 24;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }
}
