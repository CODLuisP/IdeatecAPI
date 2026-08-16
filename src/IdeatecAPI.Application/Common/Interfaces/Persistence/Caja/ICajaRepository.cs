using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

/// <summary>Datos de la sucursal necesarios para llavear la caja y filtrar comprobantes.</summary>
public record DatosSucursalCaja(string EmpresaRuc, string CodEstablecimiento, string Nombre);

/// <summary>Recaudación de un turno, agregada desde comprobante + pago.</summary>
public record ResumenVentasTurno(
    IReadOnlyList<(string MedioPago, decimal Monto)> Medios,
    decimal TotalVentas,
    int CantidadComprobantes);

public interface ICajaRepository
{
    Task<DatosSucursalCaja?> GetDatosSucursalAsync(int sucursalId);

    // ── Caja del día ──
    Task<CajaApertura?> GetCajaAbiertaAsync(int sucursalId);
    Task<CajaApertura?> GetCajaByIdAsync(int cajaAperturaId);
    Task<int> InsertCajaAsync(CajaApertura caja);
    Task<bool> CerrarCajaAsync(CajaApertura caja);

    // ── Turnos ──
    Task<CajaTurno?> GetTurnoAbiertoAsync(int cajaAperturaId);
    Task<CajaTurno?> GetTurnoByIdAsync(int cajaTurnoId);
    Task<CajaTurno?> GetUltimoTurnoCerradoAsync(int cajaAperturaId);
    Task<IEnumerable<CajaTurno>> GetTurnosByCajaAsync(int cajaAperturaId);
    Task<int> InsertTurnoAsync(CajaTurno turno);
    Task<bool> CerrarTurnoAsync(CajaTurno turno);

    // ── Detalle por medio de pago ──
    Task<int> InsertTurnoDetalleAsync(CajaTurnoDetalle detalle);
    Task<IEnumerable<CajaTurnoDetalle>> GetDetallesByTurnoIdsAsync(IEnumerable<int> cajaTurnoIds);

    /// <summary>
    /// Suma los pagos de los comprobantes emitidos por un usuario entre dos
    /// momentos. Es la fuente de los montos esperados al cuadrar.
    /// </summary>
    Task<ResumenVentasTurno> GetResumenVentasAsync(
        string empresaRuc,
        string codEstablecimiento,
        int usuarioId,
        DateTime desde,
        DateTime? hasta);

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
