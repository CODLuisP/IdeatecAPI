using System.Data;
using System.Linq;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class CajaRepository : DapperRepository<CajaApertura>, ICajaRepository
{
    public CajaRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    public async Task<DatosSucursalCaja?> GetDatosSucursalAsync(int sucursalId)
    {
        var sql = @"
            SELECT empresaRuc         AS EmpresaRuc,
                   codEstablecimiento AS CodEstablecimiento,
                   nombre             AS Nombre
            FROM sucursal
            WHERE sucursalID = @SucursalId
            LIMIT 1;";

        return await _connection.QueryFirstOrDefaultAsync<DatosSucursalCaja>(
            sql, new { SucursalId = sucursalId }, _transaction);
    }

    public async Task<IReadOnlyDictionary<int, DatosSucursalCaja>> GetDatosSucursalesAsync(IEnumerable<int> sucursalIds)
    {
        var ids = sucursalIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<int, DatosSucursalCaja>();

        var sql = @"
            SELECT sucursalID         AS SucursalId,
                   empresaRuc         AS EmpresaRuc,
                   codEstablecimiento AS CodEstablecimiento,
                   nombre             AS Nombre
            FROM sucursal
            WHERE sucursalID IN @Ids;";

        var rows = await _connection.QueryAsync<(int SucursalId, string EmpresaRuc, string CodEstablecimiento, string Nombre)>(
            sql, new { Ids = ids }, _transaction);

        return rows.ToDictionary(r => r.SucursalId, r => new DatosSucursalCaja(r.EmpresaRuc, r.CodEstablecimiento, r.Nombre));
    }

    // ─────────────────────────── Caja del día ───────────────────────────

    private const string SelectCaja = @"
        SELECT cajaAperturaID        AS CajaAperturaId,
               empresaRuc            AS EmpresaRuc,
               sucursalID            AS SucursalId,
               codEstablecimiento    AS CodEstablecimiento,
               montoInicial          AS MontoInicial,
               fechaApertura         AS FechaApertura,
               usuarioApertura       AS UsuarioApertura,
               nombreUsuarioApertura AS NombreUsuarioApertura,
               fechaCierre           AS FechaCierre,
               usuarioCierre         AS UsuarioCierre,
               nombreUsuarioCierre   AS NombreUsuarioCierre,
               efectivoEsperado      AS EfectivoEsperado,
               efectivoContado       AS EfectivoContado,
               diferencia            AS Diferencia,
               estado                AS Estado,
               observaciones         AS Observaciones
        FROM caja_apertura";

    public async Task<CajaApertura?> GetCajaAbiertaAsync(int sucursalId)
    {
        var sql = SelectCaja + @"
            WHERE sucursalID = @SucursalId AND estado = 'ABIERTA'
            ORDER BY cajaAperturaID DESC
            LIMIT 1;";

        return await _connection.QueryFirstOrDefaultAsync<CajaApertura>(
            sql, new { SucursalId = sucursalId }, _transaction);
    }

    public async Task<CajaApertura?> GetCajaByIdAsync(int cajaAperturaId)
    {
        var sql = SelectCaja + " WHERE cajaAperturaID = @Id LIMIT 1;";
        return await _connection.QueryFirstOrDefaultAsync<CajaApertura>(
            sql, new { Id = cajaAperturaId }, _transaction);
    }

    public async Task<CajaApertura?> GetUltimaCajaCerradaAsync(int sucursalId)
    {
        var sql = SelectCaja + @"
            WHERE sucursalID = @SucursalId AND estado = 'CERRADA'
            ORDER BY cajaAperturaID DESC
            LIMIT 1;";

        return await _connection.QueryFirstOrDefaultAsync<CajaApertura>(
            sql, new { SucursalId = sucursalId }, _transaction);
    }

    public async Task<int> InsertCajaAsync(CajaApertura caja)
    {
        var sql = @"
            INSERT INTO caja_apertura
                (empresaRuc, sucursalID, codEstablecimiento, montoInicial,
                 fechaApertura, usuarioApertura, nombreUsuarioApertura,
                 estado, observaciones)
            VALUES
                (@EmpresaRuc, @SucursalId, @CodEstablecimiento, @MontoInicial,
                 @FechaApertura, @UsuarioApertura, @NombreUsuarioApertura,
                 @Estado, @Observaciones);
            SELECT LAST_INSERT_ID();";

        return await _connection.ExecuteScalarAsync<int>(sql, caja, _transaction);
    }

    public async Task<bool> CerrarCajaAsync(CajaApertura caja)
    {
        // El AND estado = 'ABIERTA' evita cerrar dos veces si llegan dos peticiones.
        var sql = @"
            UPDATE caja_apertura
            SET fechaCierre         = @FechaCierre,
                usuarioCierre       = @UsuarioCierre,
                nombreUsuarioCierre = @NombreUsuarioCierre,
                efectivoEsperado    = @EfectivoEsperado,
                efectivoContado     = @EfectivoContado,
                diferencia          = @Diferencia,
                observaciones       = @Observaciones,
                estado              = 'CERRADA'
            WHERE cajaAperturaID = @CajaAperturaId AND estado = 'ABIERTA';";

        var result = await _connection.ExecuteAsync(sql, caja, _transaction);
        return result > 0;
    }

    // ───────────────────────────── Turnos ─────────────────────────────

    private const string SelectTurno = @"
        SELECT cajaTurnoID          AS CajaTurnoId,
               cajaAperturaID       AS CajaAperturaId,
               usuarioID            AS UsuarioId,
               nombreUsuario        AS NombreUsuario,
               fechaInicio          AS FechaInicio,
               fechaFin             AS FechaFin,
               saldoInicial         AS SaldoInicial,
               efectivoEsperado     AS EfectivoEsperado,
               efectivoContado      AS EfectivoContado,
               diferencia           AS Diferencia,
               totalVentas          AS TotalVentas,
               cantidadComprobantes AS CantidadComprobantes,
               estado               AS Estado,
               cerradoPorUsuarioID  AS CerradoPorUsuarioId,
               esCierreCaja         AS EsCierreCaja,
               observaciones        AS Observaciones
        FROM caja_turno";

    public async Task<CajaTurno?> GetTurnoAbiertoAsync(int cajaAperturaId)
    {
        var sql = SelectTurno + @"
            WHERE cajaAperturaID = @CajaAperturaId AND estado = 'ABIERTO'
            ORDER BY cajaTurnoID DESC
            LIMIT 1;";

        return await _connection.QueryFirstOrDefaultAsync<CajaTurno>(
            sql, new { CajaAperturaId = cajaAperturaId }, _transaction);
    }

    public async Task<CajaTurno?> GetTurnoByIdAsync(int cajaTurnoId)
    {
        var sql = SelectTurno + " WHERE cajaTurnoID = @Id LIMIT 1;";
        return await _connection.QueryFirstOrDefaultAsync<CajaTurno>(
            sql, new { Id = cajaTurnoId }, _transaction);
    }

    public async Task<CajaTurno?> GetUltimoTurnoCerradoAsync(int cajaAperturaId)
    {
        var sql = SelectTurno + @"
            WHERE cajaAperturaID = @CajaAperturaId AND estado = 'CERRADO'
            ORDER BY cajaTurnoID DESC
            LIMIT 1;";

        return await _connection.QueryFirstOrDefaultAsync<CajaTurno>(
            sql, new { CajaAperturaId = cajaAperturaId }, _transaction);
    }

    public async Task<IEnumerable<CajaTurno>> GetTurnosByCajaAsync(int cajaAperturaId)
    {
        var sql = SelectTurno + @"
            WHERE cajaAperturaID = @CajaAperturaId
            ORDER BY cajaTurnoID ASC;";

        return await _connection.QueryAsync<CajaTurno>(
            sql, new { CajaAperturaId = cajaAperturaId }, _transaction);
    }

    public async Task<IEnumerable<CajaTurno>> GetTurnosByCajasAsync(IEnumerable<int> cajaAperturaIds)
    {
        var ids = cajaAperturaIds.Distinct().ToArray();
        if (ids.Length == 0) return Enumerable.Empty<CajaTurno>();

        var sql = SelectTurno + @"
            WHERE cajaAperturaID IN @Ids
            ORDER BY cajaAperturaID, cajaTurnoID ASC;";

        return await _connection.QueryAsync<CajaTurno>(
            sql, new { Ids = ids }, _transaction);
    }

    public async Task<bool> UsuarioTieneTurnoCerradoAsync(int cajaAperturaId, int usuarioId)
    {
        var sql = @"
            SELECT COUNT(*)
            FROM caja_turno
            WHERE cajaAperturaID = @CajaAperturaId
              AND usuarioID = @UsuarioId
              AND estado = 'CERRADO';";

        var total = await _connection.ExecuteScalarAsync<int>(
            sql, new { CajaAperturaId = cajaAperturaId, UsuarioId = usuarioId }, _transaction);

        return total > 0;
    }

    public async Task<int> InsertTurnoAsync(CajaTurno turno)
    {
        var sql = @"
            INSERT INTO caja_turno
                (cajaAperturaID, usuarioID, nombreUsuario, fechaInicio,
                 saldoInicial, estado, esCierreCaja)
            VALUES
                (@CajaAperturaId, @UsuarioId, @NombreUsuario, @FechaInicio,
                 @SaldoInicial, @Estado, @EsCierreCaja);
            SELECT LAST_INSERT_ID();";

        return await _connection.ExecuteScalarAsync<int>(sql, turno, _transaction);
    }

    public async Task<bool> CerrarTurnoAsync(CajaTurno turno)
    {
        var sql = @"
            UPDATE caja_turno
            SET fechaFin             = @FechaFin,
                efectivoEsperado     = @EfectivoEsperado,
                efectivoContado      = @EfectivoContado,
                diferencia           = @Diferencia,
                totalVentas          = @TotalVentas,
                cantidadComprobantes = @CantidadComprobantes,
                cerradoPorUsuarioID  = @CerradoPorUsuarioId,
                esCierreCaja         = @EsCierreCaja,
                observaciones        = @Observaciones,
                estado               = 'CERRADO'
            WHERE cajaTurnoID = @CajaTurnoId AND estado = 'ABIERTO';";

        var result = await _connection.ExecuteAsync(sql, turno, _transaction);
        return result > 0;
    }

    // ──────────────────── Detalle por medio de pago ────────────────────

    public async Task<int> InsertTurnoDetalleAsync(CajaTurnoDetalle detalle)
    {
        var sql = @"
            INSERT INTO caja_turno_detalle
                (cajaTurnoID, medioPago, montoEsperado, montoContado, diferencia)
            VALUES
                (@CajaTurnoId, @MedioPago, @MontoEsperado, @MontoContado, @Diferencia);
            SELECT LAST_INSERT_ID();";

        return await _connection.ExecuteScalarAsync<int>(sql, detalle, _transaction);
    }

    public async Task<IEnumerable<CajaTurnoDetalle>> GetDetallesByTurnoIdsAsync(IEnumerable<int> cajaTurnoIds)
    {
        var ids = cajaTurnoIds.ToList();
        if (ids.Count == 0)
            return Enumerable.Empty<CajaTurnoDetalle>();

        var sql = @"
            SELECT cajaTurnoDetalleID AS CajaTurnoDetalleId,
                   cajaTurnoID        AS CajaTurnoId,
                   medioPago          AS MedioPago,
                   montoEsperado      AS MontoEsperado,
                   montoContado       AS MontoContado,
                   diferencia         AS Diferencia
            FROM caja_turno_detalle
            WHERE cajaTurnoID IN @Ids
            ORDER BY cajaTurnoDetalleID ASC;";

        return await _connection.QueryAsync<CajaTurnoDetalle>(sql, new { Ids = ids }, _transaction);
    }

    // ───────────────────── Recaudación del turno ─────────────────────

    public async Task<ResumenVentasTurno> GetResumenVentasAsync(
        string empresaRuc,
        string codEstablecimiento,
        int? usuarioId,
        DateTime desde,
        DateTime? hasta)
    {
        // Los comprobantes del turno son los que ese usuario creó en esa
        // sucursal dentro de la ventana del turno. Normalmente se ubican por
        // fechaCreacion (hora real de inserción en el servidor), que coincide
        // con el momento de la venta cuando hay conexión.
        //
        // Una venta hecha SIN CONEXIÓN se encola en el dispositivo y recién se
        // inserta (fechaCreacion) cuando vuelve el internet — que puede ser
        // minutos u horas después, incluso ya con el turno original cerrado.
        // Si solo mirásemos fechaCreacion, esa venta quedaría fuera de la
        // ventana de CUALQUIER turno de ese usuario: el efectivo estaría
        // físicamente en el cajón pero no en ningún cuadre (sobrante fantasma
        // al cuadrar). Por eso también se acepta por horaEmision, que es el
        // instante real de la venta que el cliente congela al guardarla
        // offline. El usuarioCreacion sigue siendo obligatorio en ambos casos,
        // así que esto nunca puede atribuir una venta al turno de otro usuario.
        // Con usuarioId nulo se agregan todos los usuarios (corte del día).
        const string filtroComprobantes = @"
            FROM comprobante c
            WHERE c.empresaRuc = @EmpresaRuc
              AND c.establecimientoAnexo = @CodEstablecimiento
              AND (@UsuarioId IS NULL OR c.usuarioCreacion = @UsuarioId)
              AND (
                    (c.fechaCreacion >= @Desde AND (@Hasta IS NULL OR c.fechaCreacion <= @Hasta))
                 OR (c.horaEmision   >= @Desde AND (@Hasta IS NULL OR c.horaEmision   <= @Hasta))
                  )
              AND (c.estadoSunat IS NULL OR c.estadoSunat NOT IN ('RECHAZADO', 'ANULADO'))";

        var parametros = new
        {
            EmpresaRuc = empresaRuc,
            CodEstablecimiento = codEstablecimiento,
            UsuarioId = usuarioId,
            Desde = desde,
            Hasta = hasta
        };

        var sqlMedios = $@"
            SELECT COALESCE(p.medioPago, 'Otro') AS MedioPago,
                   COALESCE(SUM(p.monto), 0)     AS Monto
            FROM pago p
            WHERE p.comprobanteID IN (
                SELECT c.comprobanteID {filtroComprobantes}
            )
            GROUP BY COALESCE(p.medioPago, 'Otro');";

        var medios = (await _connection.QueryAsync<(string MedioPago, decimal Monto)>(
            sqlMedios, parametros, _transaction)).ToList();

        var sqlTotales = $@"
            SELECT COALESCE(SUM(c.importeTotal), 0) AS TotalVentas,
                   COUNT(*)                         AS CantidadComprobantes
            {filtroComprobantes};";

        var totales = await _connection.QueryFirstOrDefaultAsync<(decimal TotalVentas, int CantidadComprobantes)>(
            sqlTotales, parametros, _transaction);

        return new ResumenVentasTurno(medios, totales.TotalVentas, totales.CantidadComprobantes);
    }

    public async Task<IEnumerable<VentaCategoria>> GetVentasPorCategoriaAsync(
        string empresaRuc,
        string codEstablecimiento,
        int? usuarioId,
        DateTime desde,
        DateTime hasta)
    {
        // Mismo filtro de comprobantes válidos que GetResumenVentasAsync
        // (incluye la venta offline por horaEmision además de fechaCreacion,
        // ver comentario ahí), agregando el detalle por categoría del producto.
        var sql = @"
            SELECT COALESCE(cat.categoriaNombre, 'Sin categoría') AS Categoria,
                   COALESCE(SUM(cd.totalVentaItem), 0)            AS Monto
            FROM comprobantedetalle cd
            INNER JOIN comprobante c ON c.comprobanteID = cd.comprobanteId
            LEFT JOIN producto p     ON p.productoID = cd.productoId
            LEFT JOIN categoria cat  ON cat.categoriaID = p.categoriaID
            WHERE c.empresaRuc = @EmpresaRuc
              AND c.establecimientoAnexo = @CodEstablecimiento
              AND (@UsuarioId IS NULL OR c.usuarioCreacion = @UsuarioId)
              AND (
                    (c.fechaCreacion >= @Desde AND c.fechaCreacion <= @Hasta)
                 OR (c.horaEmision   >= @Desde AND c.horaEmision   <= @Hasta)
                  )
              AND (c.estadoSunat IS NULL OR c.estadoSunat NOT IN ('RECHAZADO', 'ANULADO'))
            GROUP BY COALESCE(cat.categoriaNombre, 'Sin categoría')
            ORDER BY Monto DESC;";

        return await _connection.QueryAsync<VentaCategoria>(
            sql,
            new { EmpresaRuc = empresaRuc, CodEstablecimiento = codEstablecimiento, UsuarioId = usuarioId, Desde = desde, Hasta = hasta },
            _transaction);
    }

    // ───────────────────────── Retiros de efectivo ─────────────────────────

    public async Task<int> InsertRetiroAsync(CajaRetiro retiro)
    {
        var sql = @"
            INSERT INTO caja_retiro
                (cajaTurnoID, monto, motivo, fechaRetiro, usuarioID, nombreUsuario)
            VALUES
                (@CajaTurnoId, @Monto, @Motivo, @FechaRetiro, @UsuarioId, @NombreUsuario);
            SELECT LAST_INSERT_ID();";

        return await _connection.ExecuteScalarAsync<int>(sql, retiro, _transaction);
    }

    public async Task<IEnumerable<CajaRetiro>> GetRetirosByTurnoIdsAsync(IEnumerable<int> cajaTurnoIds)
    {
        var ids = cajaTurnoIds.ToList();
        if (ids.Count == 0)
            return Enumerable.Empty<CajaRetiro>();

        var sql = @"
            SELECT cajaRetiroID  AS CajaRetiroId,
                   cajaTurnoID   AS CajaTurnoId,
                   monto         AS Monto,
                   motivo        AS Motivo,
                   fechaRetiro   AS FechaRetiro,
                   usuarioID     AS UsuarioId,
                   nombreUsuario AS NombreUsuario
            FROM caja_retiro
            WHERE cajaTurnoID IN @Ids
            ORDER BY fechaRetiro ASC;";

        return await _connection.QueryAsync<CajaRetiro>(sql, new { Ids = ids }, _transaction);
    }

    // ──────────────────────────── Corte diario ────────────────────────────

    public async Task<IEnumerable<CajaTurno>> GetTurnosPorFechaAsync(
        int sucursalId,
        DateTime desde,
        DateTime hasta,
        int? usuarioId)
    {
        // Columnas calificadas con "t.": caja_apertura también tiene
        // cajaAperturaID/estado/observaciones, así que el SELECT sin prefijo de
        // SelectTurno sería ambiguo apenas se une con esa tabla.
        var sql = @"
            SELECT t.cajaTurnoID          AS CajaTurnoId,
                   t.cajaAperturaID       AS CajaAperturaId,
                   t.usuarioID            AS UsuarioId,
                   t.nombreUsuario        AS NombreUsuario,
                   t.fechaInicio          AS FechaInicio,
                   t.fechaFin             AS FechaFin,
                   t.saldoInicial         AS SaldoInicial,
                   t.efectivoEsperado     AS EfectivoEsperado,
                   t.efectivoContado      AS EfectivoContado,
                   t.diferencia           AS Diferencia,
                   t.totalVentas          AS TotalVentas,
                   t.cantidadComprobantes AS CantidadComprobantes,
                   t.estado               AS Estado,
                   t.cerradoPorUsuarioID  AS CerradoPorUsuarioId,
                   t.esCierreCaja         AS EsCierreCaja,
                   t.observaciones        AS Observaciones
            FROM caja_turno t
            INNER JOIN caja_apertura a ON a.cajaAperturaID = t.cajaAperturaID
            WHERE a.sucursalID = @SucursalId
              AND t.fechaInicio >= @Desde
              AND t.fechaInicio <= @Hasta
              AND (@UsuarioId IS NULL OR t.usuarioID = @UsuarioId)
            ORDER BY t.fechaInicio ASC;";

        return await _connection.QueryAsync<CajaTurno>(
            sql, new { SucursalId = sucursalId, Desde = desde, Hasta = hasta, UsuarioId = usuarioId }, _transaction);
    }

    // ──────────────────────────── Historial ────────────────────────────

    public async Task<(IEnumerable<CajaApertura> Items, int Total)> GetHistorialAsync(
        string empresaRuc,
        int? sucursalId,
        DateTime? desde,
        DateTime? hasta,
        string? estado,
        int page,
        int pageSize)
    {
        const string where = @"
            WHERE empresaRuc = @EmpresaRuc
              AND (@SucursalId IS NULL OR sucursalID = @SucursalId)
              AND (@Desde IS NULL OR fechaApertura >= @Desde)
              AND (@Hasta IS NULL OR fechaApertura <= @Hasta)
              AND (@Estado IS NULL OR estado = @Estado)";

        var parametros = new
        {
            EmpresaRuc = empresaRuc,
            SucursalId = sucursalId,
            Desde = desde,
            Hasta = hasta,
            Estado = estado,
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        };

        var total = await _connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM caja_apertura {where};", parametros, _transaction);

        var items = await _connection.QueryAsync<CajaApertura>(
            SelectCaja + where + @"
            ORDER BY fechaApertura DESC
            LIMIT @PageSize OFFSET @Offset;", parametros, _transaction);

        return (items, total);
    }
}
