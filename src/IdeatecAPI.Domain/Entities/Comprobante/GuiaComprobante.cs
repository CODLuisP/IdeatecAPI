using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities;
public class GuiaComprobante
{
    public int GuiaComprobanteId { get; set; }
    public int ComprobanteId { get; set; }
    public string? GuiaNumeroCompleto { get; set; }
    public string? GuiaTipoDoc { get; set; }
    public bool? Estado { get; set; }
}
