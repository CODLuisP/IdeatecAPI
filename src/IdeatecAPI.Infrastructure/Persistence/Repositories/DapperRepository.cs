using System.Data;
using Dapper;
using IdeatecAPI.Application.Common.Interfaces.Persistence;

namespace IdeatecAPI.Infrastructure.Persistence.Repositories;

public class DapperRepository<T> : IRepository<T> where T : class
{
    protected readonly IDbConnection _connection;
    protected readonly IDbTransaction? _transaction;

    public DapperRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        var tableName = typeof(T).Name;
        var sql = $"SELECT * FROM {tableName} WHERE Id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id }, _transaction);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var tableName = typeof(T).Name;
        var sql = $"SELECT * FROM {tableName}";
        return await _connection.QueryAsync<T>(sql, transaction: _transaction);
    }

    public async Task<IEnumerable<T>> QueryAsync(string sql, object? param = null)
    {
        return await _connection.QueryAsync<T>(sql, param, _transaction);
    }

    public async Task<T?> QueryFirstOrDefaultAsync(string sql, object? param = null)
    {
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, param, _transaction);
    }

    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        return await _connection.ExecuteAsync(sql, param, _transaction);
    }

    public async Task<int> InsertAsync(T entity)
    {
        // Implementación básica - deberás personalizarla según tu entidad
        var tableName = typeof(T).Name;
        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id")
            .Select(p => p.Name);
        
        var columns = string.Join(", ", properties);
        var values = string.Join(", ", properties.Select(p => $"@{p}"));
        
        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values}); SELECT LAST_INSERT_ID();";
        return await _connection.ExecuteScalarAsync<int>(sql, entity, _transaction);
    }

    public async Task<int> UpdateAsync(T entity)
    {
        var tableName = typeof(T).Name;
        var properties = typeof(T).GetProperties()
            .Where(p => p.Name != "Id")
            .Select(p => $"{p.Name} = @{p.Name}");
        
        var setClause = string.Join(", ", properties);
        var sql = $"UPDATE {tableName} SET {setClause} WHERE Id = @Id";
        
        return await _connection.ExecuteAsync(sql, entity, _transaction);
    }

    public async Task<int> DeleteAsync(int id)
    {
        var tableName = typeof(T).Name;
        var sql = $"DELETE FROM {tableName} WHERE Id = @Id";
        return await _connection.ExecuteAsync(sql, new { Id = id }, _transaction);
    }
}