using System.Data;
using MySqlConnector;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Infrastructure.Persistence.Repositories;
using IdeatecAPI.Infrastructure.Persistence.Repositories.Comprobantes;
using IdeatecAPI.Domain.Entities;
using IdeatecAPI.Infrastructure.Persistence.Repositories.CuentasPorCobrar;

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
    private IComprobanteRepository? _comprobantes;
    private IProductoRepository? _productos;
    private ISucursalRepository? _sucursales;

    private IComunicacionBajaRepository? _bajas;
    private IComunicacionBajaDetalleRepository? _bajaDetalles;
    private IResumenComprobanteRepository? _resumenComprobante;

    private IGuiaRemisionRepository? _guias;
    private IGuiaRemisionDetalleRepository? _guiaDetalles;
    private IDashboardRepository? _dashboard;
    private IReportesRepository? _reportes;
    private ICuentasPorCobrarRepository? _cuentasPorCobrar;

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

    public IComunicacionBajaRepository Bajas
    {
        get
        {
            _bajas ??= new ComunicacionBajaRepository(Connection, _transaction);
            return _bajas;
        }
    }

    public IComunicacionBajaDetalleRepository BajaDetalles
    {
        get
        {
            _bajaDetalles ??= new ComunicacionBajaDetalleRepository(Connection, _transaction);
            return _bajaDetalles;
        }
    }

    public IComprobanteRepository Comprobantes
    {
        get
        {
            _comprobantes ??= new ComprobanteRepository(Connection, _transaction);
            return _comprobantes;
        }
    }

    public IProductoRepository Productos
    {
        get
        {
            _productos ??= new ProductoRepository(Connection, _transaction);
            return _productos;
        }
    }

    public ISucursalRepository Sucursal
    {
        get
        {
            _sucursales ??= new SucursalRepository(Connection, _transaction);
            return _sucursales;
        }
    }

    public IGuiaRemisionRepository Guias
    {
        get
        {
            _guias ??= new GuiaRemisionRepository(Connection, _transaction);
            return _guias;
        }
    }

    public IGuiaRemisionDetalleRepository GuiaDetalles
    {
        get
        {
            _guiaDetalles ??= new GuiaRemisionDetalleRepository(Connection, _transaction);
            return _guiaDetalles;}
    }
    public IResumenComprobanteRepository ResumenComprobante
    {
        get
        {
            _resumenComprobante ??= new ResumenComprobanteRepository(Connection, _transaction);
            return _resumenComprobante;
        }
    }

    public IDashboardRepository Dashboard
    {
        get
        {
            _dashboard ??= new DashboardRepository(Connection, _transaction);
            return _dashboard;
        }
    }

    public IReportesRepository Reportes
    {
        get
        {
            _reportes ??= new ReportesRepository(Connection, _transaction);
            return _reportes;
        }
    }

    public ICuentasPorCobrarRepository CuentasPorCobrar
    {
        get
        {
            _cuentasPorCobrar ??= new CuentasPorCobrarRepository(Connection, _transaction);
            return _cuentasPorCobrar;
        }
    }

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
        _direccion = null;
        _usuarios = null;
        _clientes = null;
        _categorias = null;
        _bajas = null;
        _bajaDetalles = null;
        _guias = null;
        _guiaDetalles = null;
        _productos = null;     
        _comprobantes = null;    
        _sucursales = null; 
        _resumenComprobante = null;
        _dashboard = null;
        _reportes = null;
        _cuentasPorCobrar = null;
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