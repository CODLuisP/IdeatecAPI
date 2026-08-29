namespace IdeatecAPI.Application.Features.Sire.DTOs;

public class SirePeriodoDto
{
    public string? Periodo { get; set; }
    public string? Estado { get; set; }
    public string? Descripcion { get; set; }
}

public class SireEjercicioDto
{
    public string? Anio { get; set; }
    public List<SirePeriodoDto> Periodos { get; set; } = new();
}

public class SirePeriodosResponse
{
    public bool Success { get; set; }
    public string? Mensaje { get; set; }
    public List<SireEjercicioDto> Ejercicios { get; set; } = new();
    public string? RespuestaCruda { get; set; }
}

public class SireAceptarPropuestaResponse
{
    public bool Success { get; set; }
    public string? NumTicket { get; set; }
    public string? Mensaje { get; set; }
    public string? RespuestaCruda { get; set; }
}

public class SireRegistrarPreliminarResponse
{
    public bool Success { get; set; }
    public string? Mensaje { get; set; }
    public string? RespuestaCruda { get; set; }
}

public class SireComprobanteDto
{
    public string? RucEmisor { get; set; }
    public string? RazonSocialEmisor { get; set; }
    public string? Periodo { get; set; }
    public string? CarSunat { get; set; }
    public string? FechaEmision { get; set; }
    public string? FechaVctoPago { get; set; }
    public string? TipoComprobante { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }
    public string? TipoDocCliente { get; set; }
    public string? NumDocCliente { get; set; }
    public string? RazonSocialCliente { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal Igv { get; set; }
    public decimal MtoExonerado { get; set; }
    public decimal MtoInafecto { get; set; }
    public decimal ImporteTotal { get; set; }
    public bool Activo { get; set; }
    public decimal? TipoCambio { get; set; }
    public string? CodMoneda { get; set; }
    public string? Inconsistencias { get; set; }
    public string? FechaEmisionDocModificado { get; set; }
}

public class SireDescargarPropuestaResponse
{
    public bool Success { get; set; }
    public string? Mensaje { get; set; }
    public string? NumTicket { get; set; }
    public List<SireComprobanteDto> Comprobantes { get; set; } = new();
}

// Manual v30 §5.13/§5.14: identifica el comprobante a eliminar (mismos datos que ya trae SireComprobanteDto)
public class SireComprobanteEliminarDto
{
    public string NumSerieCDP { get; set; } = string.Empty;
    public string NumCDP { get; set; } = string.Empty;
    public string CodCar { get; set; } = string.Empty;
    public string CodTipoCDP { get; set; } = string.Empty;
}

public class SireEliminarComprobanteResponse
{
    public bool Success { get; set; }
    public string? Mensaje { get; set; }
}

// RS 112-2021/SUNAT, Anexo N°2: datos mínimos de un comprobante nuevo a agregar a la propuesta/preliminar.
// Los campos 29-32 (documento modificado) solo aplican si TipoComprobante es Nota de Crédito/Débito (07/08/87/88).
public class SireComprobanteNuevoDto
{
    public string FechaEmision { get; set; } = string.Empty;   // dd/mm/aaaa
    public string? FechaVctoPago { get; set; }                 // solo obligatorio si TipoComprobante = '14' (recibo de servicios)
    public string TipoComprobante { get; set; } = string.Empty; // tabla 3 del Anexo N°1
    public string Serie { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? TipoDocCliente { get; set; }
    public string? NumDocCliente { get; set; }
    public string? RazonSocialCliente { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal Igv { get; set; }
    public decimal ImporteTotal { get; set; }
    public string CodMoneda { get; set; } = "PEN";
    public decimal? TipoCambio { get; set; }
    public decimal ValorFacturadoExportacion { get; set; }
    public decimal MtoExonerado { get; set; }
    public decimal MtoInafecto { get; set; }
    public decimal Isc { get; set; }
    public decimal Icbper { get; set; }
    public string? FechaEmisionDocModificado { get; set; }
    public string? TipoCPModificado { get; set; }
    public string? SerieCPModificado { get; set; }
    public string? NroCPModificado { get; set; }
}

public class SireImportarComprobantesResponse
{
    public bool Success { get; set; }
    public string? Mensaje { get; set; }
    public string? NumTicket { get; set; }
}

// Manual v30 §5.12: edita el tipo de cambio de un comprobante en la propuesta (antes de aceptar)
public class SireEditarTipoCambioDto
{
    public string CodCar { get; set; } = string.Empty;
    public string CodMoneda { get; set; } = string.Empty;
    public decimal MtoTipoCambio { get; set; }
    public decimal? MtoCambioMonedaExt { get; set; }
}

public class SireEditarTipoCambioResponse
{
    public bool Success { get; set; }
    public string? Mensaje { get; set; }
}
