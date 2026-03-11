using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.ResumenComprobante.DTO;
public class AgregarResumenDetalleDTO
{
    public int LineID { get; set; }
    public int ComprobanteId { get; set; }
    public string TipoComprobante { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Correlativo { get; set; } = string.Empty;
    public string CodigoCondicion { get; set; } = "1";
    public string Moneda { get; set; } = "PEN";
    public decimal MontoTotalVenta { get; set; }
    public decimal TotalGravado { get; set; }
    public decimal TotalExonerado { get; set; }
    public decimal TotalInafecto { get; set; }
    public decimal TotalGratuito { get; set; }
    public decimal TotalIGV { get; set; }
    public decimal IGVReferencial { get; set; }
}