using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities.Invoice
{
    public class Cuotas
    {
        public int IdCuota { get; set; }
        public decimal Monto { get; set; }
        public int Numero { get; set; }  
        public DateTime FechaVencimiento { get; set; }
        public string? Estado { get; set; }
        public string? ComprobanteId { get; set; }
    }
}