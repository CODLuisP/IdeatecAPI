using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Application.Features.Direccion.DTOs;

namespace IdeatecAPI.Application.Features.Clientes.DTOs
{
    public class RegistrarClienteDTO
    {
        public string? NumeroDocumento { get; set; }
        public string? RazonSocialNombre { get; set; }
        public string? NombreComercial { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? TipoDocumentoId { get; set; } //Catalogo 06
        public RegistrarDireccionDTO? Direccion { get; set; }

    }
}