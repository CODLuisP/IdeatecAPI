using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities.Cliente
{
    public class Direccion
    {
        public int? DireccionId { get; set; }
        public string? DireccionLineal { get; set; }
        public string? Ubigeo { get; set; }
        public string? Departamento { get; set; }
        public string? Provincia { get; set; }
        public string? Distrito { get; set; }
        public string? TipoDireccion { get; set; } //fiscal, almacen.
        public bool Estado { get; set; }
        public int? ClienteId { get; set; }
    }
}