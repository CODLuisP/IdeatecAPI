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
        var sql = "SELECT * FROM categorias";
        return await _connection.QueryAsync<Categoria>(sql, transaction: _transaction);
    }

    public async Task<Categoria?> GetCategoriaByIdAsync(int id)
    {
        var sql = "SELECT * FROM categorias WHERE id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<Categoria>(sql, new { Id = id }, _transaction);
    }
}