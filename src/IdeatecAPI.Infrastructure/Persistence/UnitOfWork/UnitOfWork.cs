using System.Data;
using MySqlConnector;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Infrastructure.Persistence.Repositories;

namespace IdeatecAPI.Infrastructure.Persistence.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly string _connectionString;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;
    
    // Repositorios específicos
    private ICategoriaRepository? _categorias;
    private IUsuarioRepository? _usuarios;

    public UnitOfWork(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection Connection
    {
        get
        {
            if (_connection == null)
            {
                _connection = new MySqlConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }
    }

    // Propiedad para acceder al repositorio de Categorias
    public ICategoriaRepository Categorias
    {
        get
        {
            _categorias ??= new CategoriaRepository(Connection, _transaction);
            return _categorias;
        }
    }

    // Propiedad para acceder al repositorio de Usuarios
    public IUsuarioRepository Usuarios
    {
        get
        {
            _usuarios ??= new UsuarioRepository(Connection, _transaction);
            return _usuarios;
        }
    }

    public void BeginTransaction()
    {
        _transaction = Connection.BeginTransaction();
    }

    public void Commit()
    {
        try
        {
            _transaction?.Commit();
        }
        catch
        {
            _transaction?.Rollback();
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        return new DapperRepository<T>(Connection, _transaction);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }
}