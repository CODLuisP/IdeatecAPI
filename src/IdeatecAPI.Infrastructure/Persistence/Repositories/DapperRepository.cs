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
    // 20 detalles en 20 viajes de red.
    //
    // Este helper solo ARMA el SQL y llena los parametros, sin ejecutar. Devolver la
    // sentencia en vez de lanzarla es lo que permite encadenar varias en un unico comando
    // (cabecera + hijos, o descuento + kardex) y pagar un solo viaje por todas.
    //
    // expresionPrimeraColumna sirve para columnas cuyo valor es una expresion SQL y no un
    // parametro, como el "@cid" que arrastra el ID recien insertado de la cabecera.
    protected static string ConstruirInsertMasivo<TItem>(
        string tabla,
        IReadOnlyList<string> columnas,
        IReadOnlyList<TItem> items,
        Func<TItem, object?[]> valores,
        DynamicParameters parametros,
        string prefijo,
        string? expresionPrimeraColumna = null)
    {
        if (items.Count == 0)
            return "";

        var columnasParametrizadas = expresionPrimeraColumna is null ? columnas.Count : columnas.Count - 1;
        var tuplas = new List<string>(items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            var fila = valores(items[i]);
            if (fila.Length != columnasParametrizadas)
                throw new InvalidOperationException(
                    $"INSERT masivo en '{tabla}': la fila {i} trae {fila.Length} valores y se esperaban {columnasParametrizadas}.");

            var marcadores = new List<string>(columnas.Count);
            if (expresionPrimeraColumna is not null)
                marcadores.Add(expresionPrimeraColumna);

            for (var c = 0; c < fila.Length; c++)
            {
                var nombre = $"{prefijo}{i}_{c}";
                parametros.Add(nombre, fila[c]);
                marcadores.Add("@" + nombre);
            }

            tuplas.Add($"({string.Join(", ", marcadores)})");
        }

        return $"INSERT INTO {tabla} ({string.Join(", ", columnas)}) VALUES {string.Join(", ", tuplas)}; ";
    }

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
            var bloque = items.Skip(inicio).Take(tamanoLote).ToList();
            var parametros = new DynamicParameters();
            var sql = ConstruirInsertMasivo(tabla, columnas, bloque, valores, parametros, $"p{inicio}_");

            filasAfectadas += await _connection.ExecuteAsync(sql, parametros, _transaction);
        }

        return filasAfectadas;
    }

    // Arma (sin ejecutar) una resta distinta por fila en una sola sentencia:
    //   UPDATE tabla SET valor = valor - CASE clave WHEN @k0 THEN @v0 ... END
    //   WHERE clave IN (...) AND valor >= CASE clave WHEN @k0 THEN @v0 ... END
    // La guardia repetida en el WHERE es la que impide dejar una fila en negativo: las que
    // no la cumplen no se tocan y quedan fuera del conteo de filas afectadas, que el
    // llamador compara con lo esperado para abortar la transaccion.
    // Como todas las restas son mayores que cero, cada fila que entra cambia de valor y ese
    // conteo es el mismo tanto si el driver reporta filas encontradas como modificadas.
    protected static string ConstruirRestaEnLote(
        string tabla,
        string columnaClave,
        string columnaValor,
        IReadOnlyList<KeyValuePair<int, decimal>> restas,
        DynamicParameters parametros,
        string prefijo,
        string? condicionExtra = null)
    {
        if (restas.Count == 0)
            return "";

        var casos = new List<string>(restas.Count);
        var claves = new List<string>(restas.Count);

        for (var i = 0; i < restas.Count; i++)
        {
            parametros.Add($"{prefijo}k{i}", restas[i].Key);
            parametros.Add($"{prefijo}v{i}", restas[i].Value);
            casos.Add($"WHEN @{prefijo}k{i} THEN @{prefijo}v{i}");
            claves.Add($"@{prefijo}k{i}");
        }

        var caso = $"CASE {columnaClave} {string.Join(" ", casos)} END";
        var filtroExtra = condicionExtra is null ? "" : $"AND {condicionExtra}";

        return $@"
            UPDATE {tabla}
            SET {columnaValor} = {columnaValor} - ({caso})
            WHERE {columnaClave} IN ({string.Join(", ", claves)})
            {filtroExtra}
            AND {columnaValor} >= ({caso});
";
    }

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

        var filasAfectadas = 0;

        foreach (var bloque in restasPorClave.Chunk(tamanoLote))
        {
            var parametros = new DynamicParameters();
            var sql = ConstruirRestaEnLote(tabla, columnaClave, columnaValor, bloque, parametros, "r", condicionExtra);

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