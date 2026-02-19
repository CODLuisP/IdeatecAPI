using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.Clientes.DTOs
{
    public class ObtenerClientesTipoDocumentoDTO
    {
        public string? TipoDocumentoId { get; set; }
        public string? TipoDocumentoNombre { get; set; }
    }
}