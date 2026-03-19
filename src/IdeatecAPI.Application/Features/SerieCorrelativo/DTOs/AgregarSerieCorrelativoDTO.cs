using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.SerieCorrelativo.DTOs;
public class AgregarSerieCorrelativoDTO
{
    public string? EmpresaRuc { get; set; }
    public string? TipoComprobante { get; set; }
    public string? Serie { get; set; }
    public int? correlativoActual { get; set; }
    public bool? Estado { get; set; }
    
}