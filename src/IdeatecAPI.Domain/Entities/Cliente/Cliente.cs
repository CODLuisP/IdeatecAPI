using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Domain.Entities.CatalogosSunat;

namespace IdeatecAPI.Domain.Entities.Cliente
{
    public class Cliente
    {
        public int ClienteId { get; set; }
        public string? TipoDocumentoId { get; set; } //Catalogo 06
        public TipoDocumento? TipoDocumentoCliente { get; set; } //Catalogo 06
        public string? NumeroDocumento { get; set; }
        public string? RazonSocialNombre { get; set; }
        public string? NombreComercial { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public bool Estado { get; set; }

        public ICollection<Direccion> Direcciones { get; set; } = new List<Direccion>();
    }
}