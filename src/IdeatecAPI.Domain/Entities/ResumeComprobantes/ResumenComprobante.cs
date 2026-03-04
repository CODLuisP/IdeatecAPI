using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities;

public class ResumenComprobante
{
    public int ResumenComprobanteId { get; set; }
        
    // Empresa (aplanado)
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
    public bool? Estado { get; set; }
    public ICollection<ResumenComprobanteDetalle> DetallesResumen { get; set; } = [];
}