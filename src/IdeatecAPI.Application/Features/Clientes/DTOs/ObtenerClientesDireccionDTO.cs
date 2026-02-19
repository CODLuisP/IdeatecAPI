using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.Clientes.DTOs
{
    public class ObtenerClientesDireccionDTO
    {
        public int? DireccionId { get; set; }
        public string? Ubigeo { get; set; }
        public string? Departamento { get; set; }
        public string? Provincia { get; set; }
        public string? Distrito { get; set; }
        public string? TipoDireccion { get; set; } //fiscal, almacen.

    }
}