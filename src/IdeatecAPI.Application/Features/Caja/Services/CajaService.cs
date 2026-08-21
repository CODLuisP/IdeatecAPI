using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Caja.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Features.Caja.Services;

public interface ICajaService
{
    Task<CajaEstadoDto> GetEstadoAsync(int sucursalId, int usuarioId);
    Task<CajaEstadoDto> AbrirCajaAsync(AbrirCajaDto dto, int usuarioId, string nombreUsuario);
    Task<CajaEstadoDto> IniciarTurnoAsync(int sucursalId, int usuarioId, string nombreUsuario);
    Task<CuadreTurnoDto> GetCuadreAsync(int cajaTurnoId);
    Task<CajaTurnoDto> CuadrarAsync(CuadrarCajaDto dto, int usuarioId, string nombreUsuario);
    Task<CajaHistorialDto> GetHistorialAsync(string empresaRuc, int? sucursalId, DateTime? desde,
                                             DateTime? hasta, string? estado, int page, int pageSize);
    Task<CajaDetalleDto?> GetDetalleAsync(int cajaAperturaId);
    Task<CajaEstadoDto> RegistrarRetiroAsync(RegistrarRetiroDto dto, int usuarioId, string nombreUsuario);
    Task<CorteDiarioDto> GetCorteDiarioAsync(int sucursalId, DateTime fecha, int? usuarioId);
}

public class CajaService : ICajaService
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Medios que el cuadre siempre muestra, aunque no hayan recaudado nada, para
    /// que el cajero vea la misma grilla todos los días. El orden es el del POS.
    /// </summary>
    private static readonly string[] MediosCanonicos =
        { "Efectivo", "Tarjeta", "Yape", "Plin", "Transferencia", "Otro" };

    private const string MedioEfectivo = "Efectivo";

    public CajaService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ─────────────────────────────── Estado ───────────────────────────────

    public async Task<CajaEstadoDto> GetEstadoAsync(int sucursalId, int usuarioId)
    {
        var caja = await _unitOfWork.Caja.GetCajaAbiertaAsync(sucursalId);
        if (caja == null)
        {
            // Sin caja abierta, se propone arrancar con el efectivo que quedó
            // en el cajón al cerrar la anterior, sea de ayer o de hace un mes.
            var ultimaCerrada = await _unitOfWork.Caja.GetUltimaCajaCerradaAsync(sucursalId);
            return new CajaEstadoDto
            {
                CajaAbierta = false,
                SugerenciaMontoInicial = ultimaCerrada?.EfectivoContado
            };
        }

        var sucursal = await _unitOfWork.Caja.GetDatosSucursalAsync(sucursalId);
        var turnoAbierto = await _unitOfWork.Caja.GetTurnoAbiertoAsync(caja.CajaAperturaId);

        var estado = new CajaEstadoDto
        {
            CajaAbierta = true,
            Caja = MapCaja(caja, sucursal?.Nombre),
            SaldoEfectivo = await CalcularSaldoEfectivoAsync(caja, turnoAbierto, sucursal),
            UsuarioYaCuadro = await _unitOfWork.Caja.UsuarioTieneTurnoCerradoAsync(caja.CajaAperturaId, usuarioId),
            // Se compara solo el día calendario, no cuántas horas pasaron: una
            // caja abierta a las 11 pm y consultada a la 1 am ya es de ayer.
            CajaDeDiaAnterior = caja.FechaApertura.Date < DateTime.Now.Date
        };

        if (turnoAbierto == null)
            return estado;

        var turnoDto = MapTurno(turnoAbierto);
        if (turnoAbierto.UsuarioId == usuarioId)
            estado.TurnoActual = turnoDto;
        else
            estado.TurnoDeOtroUsuario = turnoDto;

        return estado;
    }

    /// <summary>
    /// Efectivo que debería haber en el cajón ahora mismo. Con turno abierto es
    /// lo que recibió al empezar más lo que vendió en efectivo; sin turno
    /// abierto es lo que contó físicamente el último que cuadró.
    /// </summary>
    private async Task<decimal> CalcularSaldoEfectivoAsync(
        CajaApertura caja, CajaTurno? turnoAbierto, DatosSucursalCaja? sucursal)
    {
        if (turnoAbierto == null)
        {
            var ultimoCerrado = await _unitOfWork.Caja.GetUltimoTurnoCerradoAsync(caja.CajaAperturaId);
            return ultimoCerrado?.EfectivoContado ?? caja.MontoInicial;
        }

        if (sucursal == null)
            return turnoAbierto.SaldoInicial;

        var resumen = await _unitOfWork.Caja.GetResumenVentasAsync(
            sucursal.EmpresaRuc, sucursal.CodEstablecimiento,
            turnoAbierto.UsuarioId, turnoAbierto.FechaInicio, null);

        var retiros = await _unitOfWork.Caja.GetRetirosByTurnoIdsAsync(new[] { turnoAbierto.CajaTurnoId });

        return turnoAbierto.SaldoInicial + MontoDe(resumen, MedioEfectivo) - retiros.Sum(r => r.Monto);
    }

    // ─────────────────────────────── Apertura ───────────────────────────────

    public async Task<CajaEstadoDto> AbrirCajaAsync(AbrirCajaDto dto, int usuarioId, string nombreUsuario)
    {
        if (dto.MontoInicial < 0)
            throw new ArgumentException("El monto inicial no puede ser negativo.");

        var sucursal = await _unitOfWork.Caja.GetDatosSucursalAsync(dto.SucursalId)
            ?? throw new KeyNotFoundException($"Sucursal {dto.SucursalId} no encontrada.");

        var cajaAbierta = await _unitOfWork.Caja.GetCajaAbiertaAsync(dto.SucursalId);
        if (cajaAbierta != null)
            throw new InvalidOperationException("Ya existe una caja abierta para esta sucursal.");

        _unitOfWork.BeginTransaction();
        try
        {
            var ahora = DateTime.Now;

            var caja = new CajaApertura
            {
                EmpresaRuc = sucursal.EmpresaRuc,
                SucursalId = dto.SucursalId,
                CodEstablecimiento = sucursal.CodEstablecimiento,
                MontoInicial = dto.MontoInicial,
                FechaApertura = ahora,
                UsuarioApertura = usuarioId,
                NombreUsuarioApertura = nombreUsuario,
                Estado = CajaApertura.EstadoAbierta,
                Observaciones = dto.Observaciones
            };
            caja.CajaAperturaId = await _unitOfWork.Caja.InsertCajaAsync(caja);

            // Quien abre la caja arranca de inmediato su propio turno con el
            // fondo que acaba de declarar.
            var turno = new CajaTurno
            {
                CajaAperturaId = caja.CajaAperturaId,
                UsuarioId = usuarioId,
                NombreUsuario = nombreUsuario,
                FechaInicio = ahora,
                SaldoInicial = dto.MontoInicial,
                Estado = CajaTurno.EstadoAbierto
            };
            turno.CajaTurnoId = await _unitOfWork.Caja.InsertTurnoAsync(turno);

            _unitOfWork.Commit();

            return await GetEstadoAsync(dto.SucursalId, usuarioId);
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // ──────────────────────────────── Turnos ────────────────────────────────

    public async Task<CajaEstadoDto> IniciarTurnoAsync(int sucursalId, int usuarioId, string nombreUsuario)
    {
        var caja = await _unitOfWork.Caja.GetCajaAbiertaAsync(sucursalId)
            ?? throw new InvalidOperationException("No hay una caja abierta en esta sucursal.");

        var sucursal = await _unitOfWork.Caja.GetDatosSucursalAsync(sucursalId);
        var turnoAbierto = await _unitOfWork.Caja.GetTurnoAbiertoAsync(caja.CajaAperturaId);

        if (turnoAbierto != null)
        {
            // El turno propio ya existe: iniciar es idempotente. El ajeno hay que
            // cuadrarlo primero, no se pisa en silencio.
            if (turnoAbierto.UsuarioId == usuarioId)
                return await GetEstadoAsync(sucursalId, usuarioId);

            throw new InvalidOperationException(
                $"Hay un turno abierto de {turnoAbierto.NombreUsuario}. Debe cuadrarse antes de iniciar otro.");
        }

        var saldoInicial = await CalcularSaldoEfectivoAsync(caja, null, sucursal);

        _unitOfWork.BeginTransaction();
        try
        {
            var turno = new CajaTurno
            {
                CajaAperturaId = caja.CajaAperturaId,
                UsuarioId = usuarioId,
                NombreUsuario = nombreUsuario,
                FechaInicio = DateTime.Now,
                SaldoInicial = saldoInicial,
                Estado = CajaTurno.EstadoAbierto
            };
            turno.CajaTurnoId = await _unitOfWork.Caja.InsertTurnoAsync(turno);

            _unitOfWork.Commit();

            return await GetEstadoAsync(sucursalId, usuarioId);
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // ──────────────────────────────── Cuadre ────────────────────────────────

    public async Task<CuadreTurnoDto> GetCuadreAsync(int cajaTurnoId)
    {
        var turno = await _unitOfWork.Caja.GetTurnoByIdAsync(cajaTurnoId)
            ?? throw new KeyNotFoundException($"Turno {cajaTurnoId} no encontrado.");

        var caja = await _unitOfWork.Caja.GetCajaByIdAsync(turno.CajaAperturaId)
            ?? throw new KeyNotFoundException($"Caja {turno.CajaAperturaId} no encontrada.");

        var resumen = await _unitOfWork.Caja.GetResumenVentasAsync(
            caja.EmpresaRuc!, caja.CodEstablecimiento!, turno.UsuarioId, turno.FechaInicio, turno.FechaFin);

        var ventasEfectivo = MontoDe(resumen, MedioEfectivo);

        var retiros = (await _unitOfWork.Caja.GetRetirosByTurnoIdsAsync(new[] { turno.CajaTurnoId })).ToList();
        var totalRetiros = retiros.Sum(r => r.Monto);

        return new CuadreTurnoDto
        {
            CajaTurnoId = turno.CajaTurnoId,
            CajaAperturaId = turno.CajaAperturaId,
            NombreUsuario = turno.NombreUsuario,
            FechaInicio = turno.FechaInicio,
            SaldoInicial = turno.SaldoInicial,
            VentasEfectivo = ventasEfectivo,
            TotalRetiros = totalRetiros,
            EfectivoEsperado = turno.SaldoInicial + ventasEfectivo - totalRetiros,
            TotalVentas = resumen.TotalVentas,
            CantidadComprobantes = resumen.CantidadComprobantes,
            Medios = ArmarMedios(resumen),
            Retiros = retiros.Select(MapRetiro).ToList()
        };
    }

    public async Task<CajaTurnoDto> CuadrarAsync(CuadrarCajaDto dto, int usuarioId, string nombreUsuario)
    {
        if (dto.EfectivoContado < 0)
            throw new ArgumentException("El efectivo contado no puede ser negativo.");

        var turno = await _unitOfWork.Caja.GetTurnoByIdAsync(dto.CajaTurnoId)
            ?? throw new KeyNotFoundException($"Turno {dto.CajaTurnoId} no encontrado.");

        if (turno.Estado != CajaTurno.EstadoAbierto)
            throw new InvalidOperationException("Este turno ya fue cuadrado.");

        var caja = await _unitOfWork.Caja.GetCajaByIdAsync(turno.CajaAperturaId)
            ?? throw new KeyNotFoundException($"Caja {turno.CajaAperturaId} no encontrada.");

        if (caja.Estado != CajaApertura.EstadoAbierta)
            throw new InvalidOperationException("La caja de este turno ya está cerrada.");

        var ahora = DateTime.Now;
        var resumen = await _unitOfWork.Caja.GetResumenVentasAsync(
            caja.EmpresaRuc!, caja.CodEstablecimiento!, turno.UsuarioId, turno.FechaInicio, ahora);

        var totalRetiros = (await _unitOfWork.Caja.GetRetirosByTurnoIdsAsync(new[] { turno.CajaTurnoId }))
            .Sum(r => r.Monto);
        var efectivoEsperado = turno.SaldoInicial + MontoDe(resumen, MedioEfectivo) - totalRetiros;
        var diferencia = dto.EfectivoContado - efectivoEsperado;

        _unitOfWork.BeginTransaction();
        try
        {
            turno.FechaFin = ahora;
            turno.EfectivoEsperado = efectivoEsperado;
            turno.EfectivoContado = dto.EfectivoContado;
            turno.Diferencia = diferencia;
            turno.TotalVentas = resumen.TotalVentas;
            turno.CantidadComprobantes = resumen.CantidadComprobantes;
            turno.CerradoPorUsuarioId = usuarioId;
            turno.EsCierreCaja = dto.CerrarCaja;
            turno.Observaciones = dto.Observaciones;
            turno.Estado = CajaTurno.EstadoCerrado;

            if (!await _unitOfWork.Caja.CerrarTurnoAsync(turno))
                throw new InvalidOperationException("El turno ya fue cuadrado por otra sesión.");

            // Congela los totales por medio de pago: el histórico no debe moverse
            // si mañana se anula un comprobante de este turno.
            var medios = ArmarMedios(resumen);
            foreach (var medio in medios)
            {
                var esEfectivo = string.Equals(medio.MedioPago, MedioEfectivo, StringComparison.OrdinalIgnoreCase);
                await _unitOfWork.Caja.InsertTurnoDetalleAsync(new CajaTurnoDetalle
                {
                    CajaTurnoId = turno.CajaTurnoId,
                    MedioPago = medio.MedioPago,
                    // En efectivo el esperado incluye el saldo que recibió del turno anterior.
                    MontoEsperado = esEfectivo ? efectivoEsperado : medio.MontoEsperado,
                    MontoContado = esEfectivo ? dto.EfectivoContado : null,
                    Diferencia = esEfectivo ? diferencia : null
                });
            }

            if (dto.CerrarCaja)
            {
                // El cierre resume el día entero, no solo el último turno: si un
                // turno anterior quedó con faltante, ese descuadre debe seguir
                // visible aunque los turnos posteriores hayan cuadrado exacto.
                // Cada turno arrastra como saldo inicial lo que se contó al final
                // del anterior, así que el esperado del día se reconstruye
                // sumando lo que cada turno vendió en efectivo sobre el fondo
                // con el que se abrió la caja.
                var turnos = (await _unitOfWork.Caja.GetTurnosByCajaAsync(caja.CajaAperturaId)).ToList();
                var ventasEfectivoDia = turnos.Sum(t => (t.EfectivoEsperado ?? t.SaldoInicial) - t.SaldoInicial);

                caja.FechaCierre = ahora;
                caja.UsuarioCierre = usuarioId;
                caja.NombreUsuarioCierre = nombreUsuario;
                caja.EfectivoEsperado = caja.MontoInicial + ventasEfectivoDia;
                caja.EfectivoContado = dto.EfectivoContado;
                // Equivale a la suma de las diferencias de todos los turnos.
                caja.Diferencia = dto.EfectivoContado - caja.EfectivoEsperado;
                caja.Observaciones = dto.Observaciones ?? caja.Observaciones;

                if (!await _unitOfWork.Caja.CerrarCajaAsync(caja))
                    throw new InvalidOperationException("La caja ya fue cerrada por otra sesión.");
            }

            _unitOfWork.Commit();

            var turnoDto = MapTurno(turno);
            turnoDto.Medios = medios;
            return turnoDto;
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    // ────────────────────────── Retiros de efectivo ──────────────────────────

    public async Task<CajaEstadoDto> RegistrarRetiroAsync(RegistrarRetiroDto dto, int usuarioId, string nombreUsuario)
    {
        if (dto.Monto <= 0)
            throw new ArgumentException("El monto del retiro debe ser mayor a cero.");

        if (string.IsNullOrWhiteSpace(dto.Motivo))
            throw new ArgumentException("Debe indicar el motivo del retiro.");

        var turno = await _unitOfWork.Caja.GetTurnoByIdAsync(dto.CajaTurnoId)
            ?? throw new KeyNotFoundException($"Turno {dto.CajaTurnoId} no encontrado.");

        if (turno.Estado != CajaTurno.EstadoAbierto)
            throw new InvalidOperationException("El turno ya fue cuadrado, no se puede retirar efectivo de él.");

        await _unitOfWork.Caja.InsertRetiroAsync(new CajaRetiro
        {
            CajaTurnoId = dto.CajaTurnoId,
            Monto = dto.Monto,
            Motivo = dto.Motivo.Trim(),
            FechaRetiro = DateTime.Now,
            UsuarioId = usuarioId,
            NombreUsuario = nombreUsuario
        });

        var caja = await _unitOfWork.Caja.GetCajaByIdAsync(turno.CajaAperturaId)
            ?? throw new KeyNotFoundException($"Caja {turno.CajaAperturaId} no encontrada.");

        return await GetEstadoAsync(caja.SucursalId, usuarioId);
    }

    // ────────────────────────────── Corte diario ──────────────────────────────

    public async Task<CorteDiarioDto> GetCorteDiarioAsync(int sucursalId, DateTime fecha, int? usuarioId)
    {
        var sucursal = await _unitOfWork.Caja.GetDatosSucursalAsync(sucursalId)
            ?? throw new KeyNotFoundException($"Sucursal {sucursalId} no encontrada.");

        var desde = fecha.Date;
        var hasta = fecha.Date.AddDays(1).AddTicks(-1);

        var turnosDelDia = (await _unitOfWork.Caja.GetTurnosPorFechaAsync(sucursalId, desde, hasta, usuarioId)).ToList();
        var turnoIds = turnosDelDia.Select(t => t.CajaTurnoId).ToList();

        var resumen = await _unitOfWork.Caja.GetResumenVentasAsync(
            sucursal.EmpresaRuc, sucursal.CodEstablecimiento, usuarioId, desde, hasta);

        var retiros = (await _unitOfWork.Caja.GetRetirosByTurnoIdsAsync(turnoIds)).ToList();
        var totalRetiros = retiros.Sum(r => r.Monto);

        var dineroInicial = turnosDelDia.Count > 0 ? turnosDelDia[0].SaldoInicial : 0m;
        var ventasEfectivo = MontoDe(resumen, MedioEfectivo);

        var ventasPorCategoria = await _unitOfWork.Caja.GetVentasPorCategoriaAsync(
            sucursal.EmpresaRuc, sucursal.CodEstablecimiento, usuarioId, desde, hasta);

        var rentabilidad = await _unitOfWork.InventarioLotes.GetRentabilidadDiaSucursalAsync(sucursalId, desde, hasta, usuarioId);

        // El selector de cajero necesita a TODOS los que trabajaron ese día, no
        // solo al filtrado, así que se pide sin usuarioId cuando ya hay uno.
        var cajerosDelDia = usuarioId.HasValue
            ? (await _unitOfWork.Caja.GetTurnosPorFechaAsync(sucursalId, desde, hasta, null)).ToList()
            : turnosDelDia;

        return new CorteDiarioDto
        {
            Fecha = desde,
            SucursalId = sucursalId,
            NombreSucursal = sucursal.Nombre,
            UsuarioId = usuarioId,
            NombreUsuario = turnosDelDia.FirstOrDefault()?.NombreUsuario,

            DineroInicial = dineroInicial,
            VentasEfectivo = ventasEfectivo,
            TotalRetiros = totalRetiros,
            EfectivoEsperado = dineroInicial + ventasEfectivo - totalRetiros,

            VentasTotales = resumen.TotalVentas,
            CantidadComprobantes = resumen.CantidadComprobantes,
            GananciaDia = rentabilidad.IngresoVentas > 0 || rentabilidad.CostoVentas > 0 ? rentabilidad.UtilidadBruta : null,

            OtrosMediosPago = resumen.Medios
                .Where(m => m.Monto > 0)
                .Select(m => new MedioPagoResumenDto { MedioPago = m.MedioPago, MontoEsperado = m.Monto })
                .ToList(),
            VentasPorCategoria = ventasPorCategoria
                .Select(v => new VentaCategoriaDto { Categoria = v.Categoria, Monto = v.Monto })
                .ToList(),
            Retiros = retiros.Select(MapRetiro).ToList(),
            Observaciones = turnosDelDia
                .Where(t => !string.IsNullOrWhiteSpace(t.Observaciones))
                .Select(t => new ObservacionTurnoDto { NombreUsuario = t.NombreUsuario, Texto = t.Observaciones! })
                .ToList(),
            CajerosDelDia = cajerosDelDia
                .GroupBy(t => t.UsuarioId)
                .Select(g => new CajeroResumenDto { UsuarioId = g.Key, NombreUsuario = g.First().NombreUsuario })
                .ToList()
        };
    }

    // ────────────────────────────── Historial ──────────────────────────────

    public async Task<CajaHistorialDto> GetHistorialAsync(
        string empresaRuc, int? sucursalId, DateTime? desde, DateTime? hasta,
        string? estado, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var (cajas, total) = await _unitOfWork.Caja.GetHistorialAsync(
            empresaRuc, sucursalId, desde, hasta, estado, page, pageSize);

        var items = new List<CajaHistorialItemDto>();
        foreach (var caja in cajas)
        {
            var turnos = (await _unitOfWork.Caja.GetTurnosByCajaAsync(caja.CajaAperturaId)).ToList();
            var item = new CajaHistorialItemDto
            {
                CajaAperturaId = caja.CajaAperturaId,
                EmpresaRuc = caja.EmpresaRuc,
                SucursalId = caja.SucursalId,
                MontoInicial = caja.MontoInicial,
                FechaApertura = caja.FechaApertura,
                UsuarioApertura = caja.UsuarioApertura,
                NombreUsuarioApertura = caja.NombreUsuarioApertura,
                FechaCierre = caja.FechaCierre,
                UsuarioCierre = caja.UsuarioCierre,
                NombreUsuarioCierre = caja.NombreUsuarioCierre,
                EfectivoEsperado = caja.EfectivoEsperado,
                EfectivoContado = caja.EfectivoContado,
                Diferencia = caja.Diferencia,
                Estado = caja.Estado,
                Observaciones = caja.Observaciones,
                CantidadTurnos = turnos.Count,
                TotalVentas = turnos.Sum(t => t.TotalVentas ?? 0m)
            };

            var sucursal = await _unitOfWork.Caja.GetDatosSucursalAsync(caja.SucursalId);
            item.NombreSucursal = sucursal?.Nombre;
            items.Add(item);
        }

        return new CajaHistorialDto { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<CajaDetalleDto?> GetDetalleAsync(int cajaAperturaId)
    {
        var caja = await _unitOfWork.Caja.GetCajaByIdAsync(cajaAperturaId);
        if (caja == null)
            return null;

        var sucursal = await _unitOfWork.Caja.GetDatosSucursalAsync(caja.SucursalId);
        var turnos = (await _unitOfWork.Caja.GetTurnosByCajaAsync(cajaAperturaId)).ToList();
        var detalles = (await _unitOfWork.Caja.GetDetallesByTurnoIdsAsync(turnos.Select(t => t.CajaTurnoId)))
            .ToLookup(d => d.CajaTurnoId);

        var turnosDto = new List<CajaTurnoDto>();
        foreach (var turno in turnos)
        {
            var dto = MapTurno(turno);
            dto.Medios = detalles[turno.CajaTurnoId]
                .Select(d => new MedioPagoResumenDto
                {
                    MedioPago = d.MedioPago ?? "Otro",
                    MontoEsperado = d.MontoEsperado,
                    MontoContado = d.MontoContado,
                    Diferencia = d.Diferencia
                })
                .ToList();
            turnosDto.Add(dto);
        }

        return new CajaDetalleDto { Caja = MapCaja(caja, sucursal?.Nombre), Turnos = turnosDto };
    }

    // ─────────────────────────────── Helpers ───────────────────────────────

    private static decimal MontoDe(ResumenVentasTurno resumen, string medioPago) =>
        resumen.Medios
            .Where(m => string.Equals(m.MedioPago, medioPago, StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.Monto);

    /// <summary>
    /// Los medios canónicos en orden fijo (con 0 si no recaudaron) más los que
    /// aparezcan en la base y no estén en la lista, para no perder montos.
    /// </summary>
    private static List<MedioPagoResumenDto> ArmarMedios(ResumenVentasTurno resumen)
    {
        var medios = MediosCanonicos
            .Select(nombre => new MedioPagoResumenDto
            {
                MedioPago = nombre,
                MontoEsperado = MontoDe(resumen, nombre)
            })
            .ToList();

        var extras = resumen.Medios
            .Where(m => !MediosCanonicos.Contains(m.MedioPago, StringComparer.OrdinalIgnoreCase))
            .GroupBy(m => m.MedioPago, StringComparer.OrdinalIgnoreCase)
            .Select(g => new MedioPagoResumenDto
            {
                MedioPago = g.Key,
                MontoEsperado = g.Sum(m => m.Monto)
            });

        medios.AddRange(extras);
        return medios;
    }

    private static CajaRetiroDto MapRetiro(CajaRetiro retiro) => new()
    {
        CajaRetiroId = retiro.CajaRetiroId,
        CajaTurnoId = retiro.CajaTurnoId,
        Monto = retiro.Monto,
        Motivo = retiro.Motivo,
        FechaRetiro = retiro.FechaRetiro,
        NombreUsuario = retiro.NombreUsuario
    };

    private static CajaAperturaDto MapCaja(CajaApertura caja, string? nombreSucursal) => new()
    {
        CajaAperturaId = caja.CajaAperturaId,
        EmpresaRuc = caja.EmpresaRuc,
        SucursalId = caja.SucursalId,
        NombreSucursal = nombreSucursal,
        MontoInicial = caja.MontoInicial,
        FechaApertura = caja.FechaApertura,
        UsuarioApertura = caja.UsuarioApertura,
        NombreUsuarioApertura = caja.NombreUsuarioApertura,
        FechaCierre = caja.FechaCierre,
        UsuarioCierre = caja.UsuarioCierre,
        NombreUsuarioCierre = caja.NombreUsuarioCierre,
        EfectivoEsperado = caja.EfectivoEsperado,
        EfectivoContado = caja.EfectivoContado,
        Diferencia = caja.Diferencia,
        Estado = caja.Estado,
        Observaciones = caja.Observaciones
    };

    private static CajaTurnoDto MapTurno(CajaTurno turno) => new()
    {
        CajaTurnoId = turno.CajaTurnoId,
        CajaAperturaId = turno.CajaAperturaId,
        UsuarioId = turno.UsuarioId,
        NombreUsuario = turno.NombreUsuario,
        FechaInicio = turno.FechaInicio,
        FechaFin = turno.FechaFin,
        SaldoInicial = turno.SaldoInicial,
        EfectivoEsperado = turno.EfectivoEsperado,
        EfectivoContado = turno.EfectivoContado,
        Diferencia = turno.Diferencia,
        TotalVentas = turno.TotalVentas,
        CantidadComprobantes = turno.CantidadComprobantes,
        Estado = turno.Estado,
        EsCierreCaja = turno.EsCierreCaja,
        Observaciones = turno.Observaciones
    };
}
