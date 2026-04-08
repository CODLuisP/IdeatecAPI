namespace IdeatecAPI.Application.Features.Email.SendEmail;

// Tipos de comprobante
public enum TipoComprobante { Texto = 0, Factura = 1, Boleta = 3, GuiaRemision = 9 }

// Línea de detalle compartida para Factura y Boleta
public record LineaDetalle(string Descripcion, int Cantidad, decimal PrecioUnitario);

// Datos para Factura (tipo 1) y Boleta (tipo 3)
public record DatosComprobante(
    string SerieNumero,
    string EstadoSunat,
    List<LineaDetalle> Items,
    decimal Igv,
    decimal Total
);

// Datos para Guía de Remisión (tipo 9)
// Agrega este record
public record BienGuia(string Descripcion, int Cantidad, string Unidad);

// Cambia la tupla por el record
public record DatosGuiaRemision(
    string SerieNumero,
    string EstadoSunat,
    string MotivoTraslado,
    string FechaTraslado,
    string DireccionPartida,
    string DireccionLlegada,
    List<BienGuia> Bienes  // ← antes era List<(string, int, string)>
);