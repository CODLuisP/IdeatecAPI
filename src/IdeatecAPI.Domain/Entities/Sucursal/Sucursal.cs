using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities;
public class Sucursal
{
    public int SucursalId { get; set; }
    public string? EmpresaRuc { get; set; }
    public string? CodEstablecimiento { get; set; }
    public string? SerieFactura { get; set; }
    public int? CorrelativoFactura { get; set; }
    public string? SerieBoleta { get; set; }
    public int? CorrelativoBoleta { get; set; }
    public string? SerieNotaCredito { get; set; }
    public int? CorrelativoNotaCredito { get; set; }
    public string? SerieNotaDebito { get; set; }
    public int? CorrelativoNotaDebito { get; set; }
    public string? SerieGuiaRemision { get; set; }
    public int? CorrelativoGuiaRemision { get; set; }
    public string? SerieGuiaTransportista { get; set; }
    public int? CorrelativoGuiaTransportista { get; set; }
    public bool? Estado { get; set; }
}