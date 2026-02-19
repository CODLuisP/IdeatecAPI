using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.Clientes.DTOs
{
    public class ObtenerClientesDTO
    {
        public string? RazonSocialNombre { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? NombreComercial { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public bool Estado { get; set; }
        public List<ObtenerClientesDireccionDTO>? Direccion { get; set; }
        public ObtenerClientesTipoDocumentoDTO? TipoDocumento { get; set; }
    }
}