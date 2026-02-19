using System.Data;
using MySqlConnector;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Infrastructure.Persistence.Repositories;
using IdeatecAPI.Infrastructure.Persistence.Repositories.Clientes;

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
    private IClienteRepository? _clientes;
    private IDireccionRepository? _direccion;

    private IEmpresaRepository? _empresas;
    private INoteRepository? _notes;
    private INoteDetailRepository? _noteDetails;
    private INoteLegendRepository? _noteLegends;

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

    public IClienteRepository Clientes
    {
        get
        {
            _clientes ??= new ClienteRepository(Connection, _transaction);
            return _clientes;
        }
    }

    public IDireccionRepository Direcciones
    {
        get
        {
            _direccion ??= new DireccionRepository(Connection, _transaction);
            return _direccion;
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

    public IEmpresaRepository Empresas
    {
        get
        {
            _empresas ??= new EmpresaRepository(Connection, _transaction);
            return _empresas;
        }
    }

<<<<<<< HEAD
=======
    // Agregar propiedades públicas:
    public INoteRepository Notes
    {
        get
        {
            _notes ??= new NoteRepository(Connection, _transaction);
            return _notes;
        }
    }

    public INoteDetailRepository NoteDetails
    {
        get
        {
            _noteDetails ??= new NoteDetailRepository(Connection, _transaction);
            return _noteDetails;
        }
    }

    public INoteLegendRepository NoteLegends
    {
        get
        {
            _noteLegends ??= new NoteLegendRepository(Connection, _transaction);
            return _noteLegends;
        }
    }

>>>>>>> e14d2dbe1d2ba38aad9401ce548c62fa911aef41
    public void BeginTransaction()
    {
        _transaction = Connection.BeginTransaction();
        ResetRepositories(); // ← repos se crean con la transacción activa

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
            ResetRepositories(); // ← repos se recrean sin transacción

        }
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
        ResetRepositories(); // ← repos se recrean sin transacción

    }

    // Agregar este método privado al final de la clase
    private void ResetRepositories()
    {
        _notes = null;
        _noteDetails = null;
        _noteLegends = null;
        _empresas = null;
        _usuarios = null;
        _categorias = null;
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