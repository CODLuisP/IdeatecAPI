using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Features.CatalogoSunat.DTOs;
using IdeatecAPI.Application.Features.Direccion.DTOs;

namespace IdeatecAPI.Application.Features.Clientes.DTOs
{
    public class ObtenerClientesDTO
    {
        public int? ClienteId { get; set; }
        public string? RazonSocialNombre { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? NombreComercial { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public bool Estado { get; set; }
        public List<DireccionDTO>? Direccion { get; set; }
        public TipoDocumentoDTO? TipoDocumento { get; set; }
    }
}