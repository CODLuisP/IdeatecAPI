using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities;

public class Pago
{
    public int PagoId { get; set; }
    public int? ComprobanteId { get; set; }
    public string? MedioPago { get; set; }
    public decimal? Monto { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? NumeroOperacion { get; set; }
    public string? EntidadFinanciera { get; set; }
    public string? Observaciones { get; set; }
}
