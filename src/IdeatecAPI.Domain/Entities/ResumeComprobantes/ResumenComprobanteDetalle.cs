using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities;

public class ResumenComprobanteDetalle
{
    public int ResumenComprobanteDetalleId { get; set; }
   // Identificación de línea
    public int LineID { get; set; }
    public int ComprobanteId { get; set; }
    public int? ResumenComprobanteId { get; set; }

    // 03 = Boleta | 07 = Nota Crédito Boleta | 08 = Nota Débito Boleta
    public string TipoComprobante { get; set; } = string.Empty;

    // Serie y número del comprobante
    public string Serie { get; set; } = string.Empty;
    public string Correlativo { get; set; } = string.Empty;

    // 1 = Alta | 3 = Anulación
    public string CodigoCondicion { get; set; } = "1";

    // Moneda
    public string Moneda { get; set; } = "PEN";

    // Total del comprobante
    public decimal MontoTotalVenta { get; set; }

    // Totales por tipo de operación
    public decimal TotalGravado { get; set; }      // 01
    public decimal TotalExonerado { get; set; }    // 02
    public decimal TotalInafecto { get; set; }     // 03
    public decimal TotalGratuito { get; set; }     // 04 (valor referencial)

    // Impuestos
    public decimal TotalIGV { get; set; }          // IGV real
    public decimal IGVReferencial { get; set; }    // Solo si es gratuito 
}