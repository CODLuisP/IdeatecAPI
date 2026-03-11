using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Domain.Entities;

public class Detraccion
{
    public int DetraccionID { get; set; }
    public int ComprobanteID { get; set; }
    public string? CodigoBienDetraccion { get; set; }
    public string? CodigoMedioPago { get; set; }
    public string? CuentaBancoDetraccion { get; set; }
    public decimal? PorcentajeDetraccion { get; set; }
    public decimal? MontoDetraccion { get; set; }
    public string? Observacion { get; set; }
    public bool Estado { get; set; }
}
