using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.Detraccion.DTOs;
public class DetraccionDTO
{
    public int ComprobanteID { get; set; }
    public string? CodigoBienDetraccion { get; set; }
    public string? CodigoMedioPago { get; set; }
    public string? CuentaBancoDetraccion { get; set; }
    public decimal? PorcentajeDetraccion { get; set; }
    public decimal? MontoDetraccion { get; set; }
    public string? Observacion { get; set; }
}