using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdeatecAPI.Application.Features.SerieCorrelativo.DTOs;
public class ObtenerSerieCorrelativoDTO
{
    public int SerieId { get; set; }
    public int? EmpresaId { get; set; }
    public int? TipoComprobante { get; set; }
    public string? Serie { get; set; }
    public int? CorrelativoActual { get; set; }
    public bool? Estado { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}