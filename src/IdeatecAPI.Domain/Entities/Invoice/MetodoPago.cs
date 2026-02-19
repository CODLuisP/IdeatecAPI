using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities.Invoice
{
    public class MetodoPago
    {
        public int IdMetodoPago { get; set; }
        public string? TipoMetodo { get; set; } //efectivo-yape-transferencia
        public int? UltimosDigitosTarjeta { get; set; }
        public int? NumeroOperacion { get; set; }
        public string? Banco { get; set; }
        public string? Estado { get; set; }
    }
}