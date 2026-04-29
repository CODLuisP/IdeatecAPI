using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class CategoriaRepository : DapperRepository<Categoria>, ICategoriaRepository
{
    public CategoriaRepository(IDbConnection connection, IDbTransaction? transaction = null) 
        : base(connection, transaction)
    {
    }
    public async Task<IEnumerable<Categoria>> GetAllCategoriasAsync()
    {
        var sql = "SELECT * FROM categoria";
        return await _connection.QueryAsync<Categoria>(sql, transaction: _transaction);
    }

    public async Task<Categoria?> GetCategoriaByIdAsync(int id)
    {
        var sql = "SELECT * FROM categoria WHERE categoriaID = @Id";
        return await _connection.QueryFirstOrDefaultAsync<Categoria>(sql, new { Id = id }, _transaction);
    }

    public async Task<IEnumerable<Categoria>> GetCategoriasByEmpresaRucAsync(string empresaRuc)
    {
        var sql = "SELECT * FROM categoria WHERE empresaRuc = @EmpresaRuc";
        return await _connection.QueryAsync<Categoria>(sql, new { EmpresaRuc = empresaRuc }, _transaction);
    }

    public async Task<bool> RegistrarCategoriaAsync(Categoria categoria)
    {
        var sql = @"
            INSERT INTO categoria 
            (empresaRuc, categoriaNombre, descripcion)
            VALUES 
            (@EmpresaRuc, @CategoriaNombre, @Descripcion);";

        var result = await _connection.ExecuteAsync(sql, categoria, _transaction);

        return result > 0;
    }

    public async Task<bool> EditarCategoriaAsync(Categoria categoria)
    {
        var sql = @"
            UPDATE categoria
            SET 
                empresaRuc      = @EmpresaRuc,
                categoriaNombre = @CategoriaNombre,
                descripcion     = @Descripcion
            WHERE categoriaID = @CategoriaId;";

        var result = await _connection.ExecuteAsync(sql, categoria, _transaction);

        return result > 0;
    }

    public async Task<bool> EliminarCategoriaAsync(int categoriaId)
    {
        var sql = @"
            UPDATE categoria
            SET estado = 0
            WHERE categoriaID = @CategoriaId;";

        var result = await _connection.ExecuteAsync(
            sql,
            new { CategoriaId = categoriaId },
            _transaction
        );

        return result > 0;
    }
}