using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.SerieCorrelativo.DTOs;
public class AgregarSerieCorrelativoDTO
{
    public int? EmpresaId { get; set; }
    public int? TipoComprobante { get; set; }
    public string? Serie { get; set; }
    public int? correlativoActual { get; set; }
    public bool? Estado { get; set; }
    
}