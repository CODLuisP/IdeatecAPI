using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities.Invoice
{
    public class Pagos
    {
        public int IdPago { get; set; }
        public string? ComprobanteId { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaPago { get; set; }
        public int IdMetodoPago { get; set; }
        public int? IdCuota { get; set; }
        public string? Observacion { get; set; }
    
    }
}