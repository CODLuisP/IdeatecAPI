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

    // Dapper, cuando recibe un IEnumerable como parametro, NO genera un INSERT multi-fila:
    // itera la lista y manda un comando por elemento. Contra una BD remota eso convierte
    // 20 detalles en 20 viajes de red. Este helper arma un unico
    // INSERT INTO tabla (cols) VALUES (..),(..),(..) con parametros numerados.
    protected async Task<int> EjecutarInsertMasivoAsync<TItem>(
        string tabla,
        IReadOnlyList<string> columnas,
        IReadOnlyList<TItem> items,
        Func<TItem, object?[]> valores,
        int tamanoLote = 200)
    {
        if (items.Count == 0)
            return 0;

        var filasAfectadas = 0;

        for (var inicio = 0; inicio < items.Count; inicio += tamanoLote)
        {
            var fin = Math.Min(inicio + tamanoLote, items.Count);
            var parametros = new DynamicParameters();
            var tuplas = new List<string>(fin - inicio);

            for (var i = inicio; i < fin; i++)
            {
                var fila = valores(items[i]);
                if (fila.Length != columnas.Count)
                    throw new InvalidOperationException(
                        $"INSERT masivo en '{tabla}': la fila {i} trae {fila.Length} valores y se esperaban {columnas.Count}.");

                var marcadores = new string[columnas.Count];
                for (var c = 0; c < columnas.Count; c++)
                {
                    var nombre = $"p{i}_{c}";
                    parametros.Add(nombre, fila[c]);
                    marcadores[c] = "@" + nombre;
                }

                tuplas.Add($"({string.Join(", ", marcadores)})");
            }

            var sql = $"INSERT INTO {tabla} ({string.Join(", ", columnas)}) VALUES {string.Join(", ", tuplas)};";
            filasAfectadas += await _connection.ExecuteAsync(sql, parametros, _transaction);
        }

        return filasAfectadas;
    }

    // Aplica una resta distinta a cada fila en una sola sentencia:
    //   UPDATE tabla SET valor = valor - CASE clave WHEN @k0 THEN @v0 ... END
    //   WHERE clave IN (...) AND valor >= CASE clave WHEN @k0 THEN @v0 ... END
    // La guardia repetida en el WHERE es la que impide dejar una fila en negativo: las que
    // no la cumplen no se tocan y quedan fuera del conteo devuelto, y el llamador compara
    // ese conteo con lo esperado para abortar la transaccion.
    // Como todas las restas son mayores que cero, cada fila que entra cambia de valor y el
    // conteo es el mismo tanto si el driver reporta filas encontradas como modificadas.
    protected async Task<int> RestarEnLoteAsync(
        string tabla,
        string columnaClave,
        string columnaValor,
        IReadOnlyDictionary<int, decimal> restasPorClave,
        string? condicionExtra = null,
        int tamanoLote = 500)
    {
        if (restasPorClave.Count == 0)
            return 0;

        var filtroExtra = condicionExtra is null ? "" : $"AND {condicionExtra}";
        var filasAfectadas = 0;

        foreach (var bloque in restasPorClave.Chunk(tamanoLote))
        {
            var parametros = new DynamicParameters();
            var casos = new List<string>(bloque.Length);
            var claves = new List<string>(bloque.Length);

            for (var i = 0; i < bloque.Length; i++)
            {
                parametros.Add($"k{i}", bloque[i].Key);
                parametros.Add($"v{i}", bloque[i].Value);
                casos.Add($"WHEN @k{i} THEN @v{i}");
                claves.Add($"@k{i}");
            }

            var caso = $"CASE {columnaClave} {string.Join(" ", casos)} END";

            var sql = $@"
                UPDATE {tabla}
                SET {columnaValor} = {columnaValor} - ({caso})
                WHERE {columnaClave} IN ({string.Join(", ", claves)})
                {filtroExtra}
                AND {columnaValor} >= ({caso})";

            filasAfectadas += await _connection.ExecuteAsync(sql, parametros, _transaction);
        }

        return filasAfectadas;
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