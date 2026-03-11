using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.ResumenComprobante.DTO;
public class ObtenerResumenComprobanteDTO
{
    public int ResumenComprobanteId { get; set; }
    public int EmpresaId { get; set; }
    public string? EmpresaRuc { get; set; }
    public string? EmpresaRazonSocial { get; set; }
    public int NumeroEnvio { get; set; }  // falta bd         
    public DateTime FechaEmisionDocumentos { get; set; }   // fechaEmisionDocumentos date
    public DateTime FechaGeneracion { get; set; }          // fechaGeneracion date
    public string Identificador { get; set; } = string.Empty;     // identificador varchar(20)
    public string EstadoSunat { get; set; } = string.Empty;       // estadoSunat varchar(20)
    public string Ticket { get; set; } = string.Empty;            // ticket varchar(50)
    public string CodigoRespuesta { get; set; } = string.Empty;   // codigoRespuesta varchar(10)
    public string MensajeRespuesta { get; set; } = string.Empty;  // mensajeRespuesta text
    public string XmlGenerado { get; set; } = string.Empty;       // xmlGenerado longtext
    public DateTime? FechaEnvio { get; set; }    // fechaEnvio datetime (nullable por si aún no se envía)
    public ICollection<ObtenerResumenDetalleDTO> DetallesResumen { get; set; } = [];
}

public class ObtenerResumenDetalleDTO
{
    public int ResumenComprobanteDetalleId { get; set; }
    public int LineID { get; set; }
    public int ComprobanteId { get; set; }
    public int? ResumenComprobanteId { get; set; }
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