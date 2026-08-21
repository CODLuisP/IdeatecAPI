using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

/// <summary>Datos de la sucursal necesarios para llavear la caja y filtrar comprobantes.</summary>
public record DatosSucursalCaja(string EmpresaRuc, string CodEstablecimiento, string Nombre);

/// <summary>Recaudación de un turno, agregada desde comprobante + pago.</summary>
public record ResumenVentasTurno(
    IReadOnlyList<(string MedioPago, decimal Monto)> Medios,
    decimal TotalVentas,
    int CantidadComprobantes);

/// <summary>Vendido por categoría de producto en una ventana de tiempo.</summary>
public record VentaCategoria(string Categoria, decimal Monto);

public interface ICajaRepository
{
    Task<DatosSucursalCaja?> GetDatosSucursalAsync(int sucursalId);

    // ── Caja del día ──
    Task<CajaApertura?> GetCajaAbiertaAsync(int sucursalId);
    Task<CajaApertura?> GetCajaByIdAsync(int cajaAperturaId);

    /// <summary>
    /// Última caja cerrada de la sucursal, sin importar hace cuánto. Su
    /// efectivo contado es lo que quedó físicamente en el cajón y sirve para
    /// proponer el fondo con el que se abre la siguiente.
    /// </summary>
    Task<CajaApertura?> GetUltimaCajaCerradaAsync(int sucursalId);
    Task<int> InsertCajaAsync(CajaApertura caja);
    Task<bool> CerrarCajaAsync(CajaApertura caja);

    // ── Turnos ──
    Task<CajaTurno?> GetTurnoAbiertoAsync(int cajaAperturaId);
    Task<CajaTurno?> GetTurnoByIdAsync(int cajaTurnoId);
    Task<CajaTurno?> GetUltimoTurnoCerradoAsync(int cajaAperturaId);
    Task<IEnumerable<CajaTurno>> GetTurnosByCajaAsync(int cajaAperturaId);

    /// <summary>
    /// Si el usuario ya cerró un turno en esta caja, no se le abre otro solo
    /// por volver a entrar a Nueva Venta: tiene que pedirlo explícitamente.
    /// </summary>
    Task<bool> UsuarioTieneTurnoCerradoAsync(int cajaAperturaId, int usuarioId);
    Task<int> InsertTurnoAsync(CajaTurno turno);
    Task<bool> CerrarTurnoAsync(CajaTurno turno);

    // ── Detalle por medio de pago ──
    Task<int> InsertTurnoDetalleAsync(CajaTurnoDetalle detalle);
    Task<IEnumerable<CajaTurnoDetalle>> GetDetallesByTurnoIdsAsync(IEnumerable<int> cajaTurnoIds);

    /// <summary>
    /// Suma los pagos de los comprobantes emitidos entre dos momentos. Con
    /// usuarioId nulo suma todos los usuarios (corte del día completo). Es la
    /// fuente de los montos esperados al cuadrar y del corte diario.
    /// </summary>
    Task<ResumenVentasTurno> GetResumenVentasAsync(
        string empresaRuc,
        string codEstablecimiento,
        int? usuarioId,
        DateTime desde,
        DateTime? hasta);

    /// <summary>Vendido por categoría de producto, para el corte diario.</summary>
    Task<IEnumerable<VentaCategoria>> GetVentasPorCategoriaAsync(
        string empresaRuc,
        string codEstablecimiento,
        int? usuarioId,
        DateTime desde,
        DateTime hasta);

    // ── Retiros de efectivo ──
    Task<int> InsertRetiroAsync(CajaRetiro retiro);
    Task<IEnumerable<CajaRetiro>> GetRetirosByTurnoIdsAsync(IEnumerable<int> cajaTurnoIds);

    /// <summary>
    /// Turnos de una sucursal cuyo inicio cae dentro del rango de fechas, sin
    /// importar a qué caja_apertura pertenezcan. Es la base del corte diario:
    /// agrupa por día real en vez de por caja.
    /// </summary>
    Task<IEnumerable<CajaTurno>> GetTurnosPorFechaAsync(
        int sucursalId,
        DateTime desde,
        DateTime hasta,
        int? usuarioId);

    // ── Historial (módulo Caja) ──
    Task<(IEnumerable<CajaApertura> Items, int Total)> GetHistorialAsync(
        string empresaRuc,
        int? sucursalId,
        DateTime? desde,
        DateTime? hasta,
        string? estado,
        int page,
        int pageSize);
}
