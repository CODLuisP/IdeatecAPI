using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities;

public class Cuota
{
    public int CuotaId { get; set; }
    public int? ComprobanteId { get; set; }
    public string? NumeroCuota { get; set; }
    public decimal? Monto { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string? MontoPagado { get; set; }
    public DateTime? FechaPago { get; set; } 
    public string? Estado { get; set; }
}
