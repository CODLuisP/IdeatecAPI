namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IRepository<T> where T : class
{
    // Queries
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> QueryAsync(string sql, object? param = null);
    Task<T?> QueryFirstOrDefaultAsync(string sql, object? param = null);
    
    // Commands
    Task<int> ExecuteAsync(string sql, object? param = null);
    Task<int> InsertAsync(T entity);
    Task<int> UpdateAsync(T entity);
    Task<int> DeleteAsync(int id);
}