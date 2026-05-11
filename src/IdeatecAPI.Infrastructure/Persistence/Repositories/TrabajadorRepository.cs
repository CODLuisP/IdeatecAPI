using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Trabajadores.DTOs;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class TrabajadorRepository : DapperRepository<Trabajador>, ITrabajadorRepository
{
    public TrabajadorRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    public async Task<IEnumerable<Trabajador>> GetAllBySucursalAsync(int sucursalId)
    {
        var sql = @"
            SELECT
                id          AS Id,
                nombres     AS Nombres,
                apellidos   AS Apellidos,
                dni         AS Dni,
                celular     AS Celular,
                email       AS Email,
                estado      AS Estado,
                sucursal    AS SucursalId,
                created_at  AS CreatedAt,
                updated_at  AS UpdatedAt
            FROM trabajador
            WHERE sucursal = @SucursalId
              AND estado = 1
            ORDER BY apellidos ASC, nombres ASC;";

        return await _connection.QueryAsync<Trabajador>(
            sql,
            new { SucursalId = sucursalId },
            _transaction
        );
    }

    public new async Task<Trabajador?> GetByIdAsync(int id)
    {
        var sql = @"
        SELECT
            id          AS Id,
            nombres     AS Nombres,
            apellidos   AS Apellidos,
            dni         AS Dni,
            celular     AS Celular,
            email       AS Email,
            estado      AS Estado,
            sucursal    AS SucursalId,
            created_at  AS CreatedAt,
            updated_at  AS UpdatedAt
        FROM trabajador
        WHERE id = @Id
          AND estado = 1;";

        return await _connection.QueryFirstOrDefaultAsync<Trabajador>(
            sql,
            new { Id = id },
            _transaction
        );
    }

    public async Task<Trabajador?> GetByDniAsync(string dni)
    {
        var sql = @"
            SELECT id AS Id, dni AS Dni
            FROM trabajador
            WHERE dni = @Dni AND estado = 1
            LIMIT 1;";

        return await _connection.QueryFirstOrDefaultAsync<Trabajador>(
            sql,
            new { Dni = dni },
            _transaction
        );
    }

    public async Task<Trabajador?> GetByDniEnSucursalAsync(string dni, int sucursalId)
    {
        var sql = @"
            SELECT id AS Id, dni AS Dni
            FROM trabajador
            WHERE dni = @Dni
              AND sucursal = @SucursalId
              AND estado = 1
            LIMIT 1;";

        return await _connection.QueryFirstOrDefaultAsync<Trabajador>(
            sql,
            new { Dni = dni, SucursalId = sucursalId },
            _transaction
        );
    }

    public async Task<Trabajador> RegistrarAsync(Trabajador trabajador)
    {
        var sql = @"
            INSERT INTO trabajador
                (nombres, apellidos, dni, celular, email, estado, sucursal, created_at, updated_at)
            VALUES
                (@Nombres, @Apellidos, @Dni, @Celular, @Email, 1, @SucursalId, @CreatedAt, @UpdatedAt);
            SELECT LAST_INSERT_ID();";

        var newId = await _connection.ExecuteScalarAsync<int>(sql, trabajador, _transaction);
        trabajador.Id = newId;
        return trabajador;
    }

    public async Task<bool> EditarAsync(Trabajador trabajador)
    {
        var sql = @"
            UPDATE trabajador
            SET
                nombres    = @Nombres,
                apellidos  = @Apellidos,
                dni        = @Dni,
                celular    = @Celular,
                email      = @Email,
                sucursal   = CASE WHEN @SucursalId > 0 THEN @SucursalId ELSE sucursal END,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        var result = await _connection.ExecuteAsync(sql, trabajador, _transaction);
        return result > 0;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var sql = @"
            UPDATE trabajador
            SET estado = 0, updated_at = NOW()
            WHERE id = @Id;";

        var result = await _connection.ExecuteAsync(sql, new { Id = id }, _transaction);
        return result > 0;
    }

    public async Task<IEnumerable<Trabajador>> SearchBySucursalAsync(int sucursalId, string palabra)
    {
        var sql = @"
            SELECT
                id          AS Id,
                nombres     AS Nombres,
                apellidos   AS Apellidos,
                dni         AS Dni,
                celular     AS Celular,
                email       AS Email,
                estado      AS Estado,
                sucursal    AS SucursalId,
                created_at  AS CreatedAt,
                updated_at  AS UpdatedAt
            FROM trabajador
            WHERE sucursal = @SucursalId
              AND estado = 1
              AND (nombres LIKE @Palabra
                   OR apellidos LIKE @Palabra
                   OR dni LIKE @Palabra)
            ORDER BY apellidos ASC, nombres ASC
            LIMIT 10;";

        return await _connection.QueryAsync<Trabajador>(
            sql,
            new { SucursalId = sucursalId, Palabra = $"%{palabra}%" },
            _transaction
        );
    }

    // ── Agregar este método en TrabajadorRepository.cs ──────────────────────────

    public async Task<IEnumerable<ReporteServicioRawDTO>> GetServiciosByTrabajadorAsync(
        int trabajadorId,
        DateTime? fechaDesde,
        DateTime? fechaHasta)
    {
        var sql = @"
        SELECT
            t.id                AS TrabajadorId,
            t.nombres           AS Nombres,
            t.apellidos         AS Apellidos,
            t.dni               AS Dni,

            c.comprobanteID     AS ComprobanteId,
            c.numeroCompleto    AS NumeroCompleto,
            c.tipoComprobante   AS TipoComprobante,
            c.fechaEmision      AS FechaEmision,
            c.tipoMoneda        AS TipoMoneda,
            c.estadoSunat       AS EstadoSunat,

            c.clienteNumDoc     AS ClienteNumDoc,
            c.clienteRznSocial  AS ClienteRazonSocial,

            cd.detalleID        AS DetalleId,
            cd.codigo           AS Codigo,
            cd.descripcion      AS Descripcion,
            cd.cantidad         AS Cantidad,
            cd.unidadMedida     AS UnidadMedida,
            cd.precioUnitario   AS PrecioUnitario,
            cd.totalVentaItem   AS TotalVentaItem

        FROM trabajador t
        INNER JOIN comprobantedetalle cd ON cd.trabajadorID = t.id
        INNER JOIN comprobante c ON c.comprobanteID = cd.comprobanteID
        WHERE t.id = @TrabajadorId
          AND t.estado = 1
          AND (@FechaDesde IS NULL OR c.fechaEmision >= @FechaDesde)
          AND (@FechaHasta IS NULL OR c.fechaEmision <= @FechaHasta)
        ORDER BY c.fechaEmision DESC, c.comprobanteID, cd.item;";

        return await _connection.QueryAsync<ReporteServicioRawDTO>(
            sql,
            new { TrabajadorId = trabajadorId, FechaDesde = fechaDesde, FechaHasta = fechaHasta },
            _transaction
        );
    }

    public async Task<IEnumerable<RankingTrabajadorDTO>> GetRankingBySucursalAsync(
    int sucursalId,
    DateTime? fechaDesde,
    DateTime? fechaHasta)
    {
        var sql = @"
        SELECT
            t.id            AS TrabajadorId,
            t.nombres       AS Nombres,
            t.apellidos     AS Apellidos,
            t.dni           AS Dni,
            COUNT(DISTINCT cd.comprobanteID) AS TotalComprobantes,
            COUNT(cd.detalleID)              AS TotalServicios,
            SUM(cd.totalVentaItem)           AS TotalMonto
        FROM trabajador t
        INNER JOIN comprobantedetalle cd ON cd.trabajadorID = t.id
        INNER JOIN comprobante c ON c.comprobanteID = cd.comprobanteID
        WHERE t.sucursal = @SucursalId
          AND t.estado = 1
          AND (@FechaDesde IS NULL OR c.fechaEmision >= @FechaDesde)
          AND (@FechaHasta IS NULL OR c.fechaEmision <= @FechaHasta)
        GROUP BY t.id, t.nombres, t.apellidos, t.dni
        ORDER BY TotalMonto DESC;";

        return await _connection.QueryAsync<RankingTrabajadorDTO>(
            sql,
            new { SucursalId = sucursalId, FechaDesde = fechaDesde, FechaHasta = fechaHasta },
            _transaction
        );
    }

    public async Task<IEnumerable<ServicioTopDTO>> GetServiciosTopBySucursalAsync(
        int sucursalId,
        DateTime? fechaDesde,
        DateTime? fechaHasta)
    {
        var sql = @"
        SELECT
            cd.descripcion          AS Descripcion,
            cd.codigo               AS Codigo,
            COUNT(cd.detalleID)     AS TotalVeces,
            SUM(cd.cantidad)        AS TotalCantidad,
            SUM(cd.totalVentaItem)  AS TotalMonto,
            AVG(cd.precioUnitario)  AS PrecioPromedio
        FROM comprobantedetalle cd
        INNER JOIN comprobante c ON c.comprobanteID = cd.comprobanteID
        INNER JOIN trabajador t ON t.id = cd.trabajadorID
        WHERE t.sucursal = @SucursalId
          AND (@FechaDesde IS NULL OR c.fechaEmision >= @FechaDesde)
          AND (@FechaHasta IS NULL OR c.fechaEmision <= @FechaHasta)
        GROUP BY cd.descripcion, cd.codigo
        ORDER BY TotalVeces DESC;"; 

        return await _connection.QueryAsync<ServicioTopDTO>(
            sql,
            new { SucursalId = sucursalId, FechaDesde = fechaDesde, FechaHasta = fechaHasta},
            _transaction
        );
    }
}