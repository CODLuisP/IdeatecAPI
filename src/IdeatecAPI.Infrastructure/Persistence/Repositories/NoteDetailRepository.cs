using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class NoteDetailRepository : DapperRepository<NoteDetail>, INoteDetailRepository
{
    public NoteDetailRepository(IDbConnection connection, IDbTransaction? transaction = null)
        : base(connection, transaction)
    {
    }

    public async Task<IEnumerable<NoteDetail>> GetByComprobanteIdAsync(int comprobanteId)
    {
        var sql = @"
            SELECT * FROM comprobanteDetalle 
            WHERE comprobanteID = @ComprobanteId 
            ORDER BY item";

        return await _connection.QueryAsync<NoteDetail>(sql, new { ComprobanteId = comprobanteId }, _transaction);
    }

    public async Task<int> CreateDetailAsync(NoteDetail detail)
    {
        var sql = @"
            INSERT INTO comprobanteDetalle (
                comprobanteID, item, productoID, codigo, descripcion,
                cantidad, unidadMedida, precioUnitario,
                tipoAfectacionIGV, porcentajeIGV, montoIGV,
                descuentoUnitario, descuentoTotal,
                valorVenta, precioVenta
            ) VALUES (
                @ComprobanteId, @Item, @ProductoId, @CodProducto, @Descripcion,
                @Cantidad, @Unidad, @MtoValorUnitario,
                @TipoAfectacionIGV, @PorcentajeIGV, @Igv,
                @DescuentoUnitario, @DescuentoTotal,
                @MtoValorVenta, @MtoPrecioUnitario
            );
            SELECT LAST_INSERT_ID();";

        return await _connection.ExecuteScalarAsync<int>(sql, detail, _transaction);
    }

    public async Task DeleteByComprobanteIdAsync(int comprobanteId)
    {
        var sql = "DELETE FROM comprobanteDetalle WHERE comprobanteID = @ComprobanteId";
        await _connection.ExecuteAsync(sql, new { ComprobanteId = comprobanteId }, _transaction);
    }
}