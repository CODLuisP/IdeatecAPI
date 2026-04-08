using IdeatecAPI.Application.Features.Comprobante.DTOs;

namespace IdeatecAPI.Application.Features.Comprobante.Services;

/// <summary>
/// Tamaños de papel soportados para la generación del PDF del comprobante.
/// </summary>
public enum TamanoPdf
{
    /// <summary>A4 estándar (210 x 297 mm)</summary>
    A4,
    /// <summary>Carta / Letter (216 x 279 mm)</summary>
    Carta,
    /// <summary>Ticket 80 mm – impresora térmica ancha (80 x variable mm)</summary>
    Ticket80mm,
    /// <summary>Ticket 58 mm – impresora térmica pequeña (58 x variable mm)</summary>
    Ticket58mm,
    /// <summary>Media carta (216 x 140 mm) – útil para boletas compactas</summary>
    MediaCarta
}

public interface IComprobantePdfService
{
    /// <summary>
    /// Genera el PDF de un comprobante (factura o boleta) a partir de su ID.
    /// </summary>
    /// <param name="comprobanteId">ID del comprobante en base de datos.</param>
    /// <param name="tamano">Tamaño de papel deseado (por defecto A4).</param>
    /// <returns>Bytes del PDF generado.</returns>
    Task<byte[]> GenerarPdfAsync(int comprobanteId, TamanoPdf tamano = TamanoPdf.A4);
}
