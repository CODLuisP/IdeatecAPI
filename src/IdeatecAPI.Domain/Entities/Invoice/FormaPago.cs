using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities;

    public class FormaPago
    {
        public int IdFormaPago { get; set; }
        public string? Tipo { get; set; } //contado-credito
        public decimal? MontoInicial { get; set; }
        public decimal? SaldoInicial { get; set; }
        public int? NumeroCuotas { get; set; }
        public string? ComprobanteId { get; set; }
    }
