using System.Data;
using MySqlConnector;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Infrastructure.Persistence.Repositories;
using IdeatecAPI.Infrastructure.Persistence.Repositories.Comprobantes;
using IdeatecAPI.Infrastructure.Persistence.Repositories.CuentasPorCobrar;
using Microsoft.Extensions.Configuration;
using IdeatecAPI.Application.Common.Interfaces;

namespace IdeatecAPI.Infrastructure.Persistence.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly IConfiguration _configuration;
    private string _connectionString;
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
    private ITrabajadorRepository? _trabajadores;

    public UnitOfWork(IConfiguration configuration, ICurrentUserService currentUserService)
    {
        _configuration = configuration;
        
        // Determinar entorno inicial desde el usuario (claim JWT)
        var env = currentUserService.Environment?.ToLower();
        _connectionString = GetConnectionStringByEnv(env);
    }

    public void SetEnvironment(string env)
    {
        var newConnectionString = GetConnectionStringByEnv(env);
        
        if (_connectionString != newConnectionString)
        {
            _connectionString = newConnectionString;
            
            // Cerrar conexión anterior si existe
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
            
            ResetRepositories();
        }
    }

    private string GetConnectionStringByEnv(string? env)
    {
        return env == "beta" 
            ? _configuration.GetConnectionString("BetaConnection")!
            : _configuration.GetConnectionString("ProductionConnection")!;
    }

    private IDbConnection CurrentConnection
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

    private async Task<IDbConnection> GetConnectionAsync()
    {
        if (_connection == null)
        {
            var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();
            _connection = conn;
        }
        return _connection;
    }

    // ── Repositorios (Todos dinámicos según el entorno activo) ──

    public IUsuarioRepository Usuarios
    {
        get
        {
            _usuarios ??= new UsuarioRepository(CurrentConnection, _transaction);
            return _usuarios;
        }
    }

    public IEmpresaRepository Empresas
    {
        get
        {
            _empresas ??= new EmpresaRepository(CurrentConnection, _transaction);
            return _empresas;
        }
    }

    public ISucursalRepository Sucursal
    {
        get
        {
            _sucursales ??= new SucursalRepository(CurrentConnection, _transaction);
            return _sucursales;
        }
    }

    public ICategoriaRepository Categorias
    {
        get
        {
            _categorias ??= new CategoriaRepository(CurrentConnection, _transaction);
            return _categorias;
        }
    }

    public IClienteRepository Clientes
    {
        get
        {
            _clientes ??= new ClienteRepository(CurrentConnection, _transaction);
            return _clientes;
        }
    }

    public IDireccionRepository Direcciones
    {
        get
        {
            _direccion ??= new DireccionRepository(CurrentConnection, _transaction);
            return _direccion;
        }
    }

    public INoteRepository Notes
    {
        get
        {
            _notes ??= new NoteRepository(CurrentConnection, _transaction);
            return _notes;
        }
    }

    public INoteDetailRepository NoteDetails
    {
        get
        {
            _noteDetails ??= new NoteDetailRepository(CurrentConnection, _transaction);
            return _noteDetails;
        }
    }

    public INoteLegendRepository NoteLegends
    {
        get
        {
            _noteLegends ??= new NoteLegendRepository(CurrentConnection, _transaction);
            return _noteLegends;
        }
    }

    public IComunicacionBajaRepository Bajas
    {
        get
        {
            _bajas ??= new ComunicacionBajaRepository(CurrentConnection, _transaction);
            return _bajas;
        }
    }

    public IComunicacionBajaDetalleRepository BajaDetalles
    {
        get
        {
            _bajaDetalles ??= new ComunicacionBajaDetalleRepository(CurrentConnection, _transaction);
            return _bajaDetalles;
        }
    }

    public IComprobanteRepository Comprobantes
    {
        get
        {
            _comprobantes ??= new ComprobanteRepository(CurrentConnection, _transaction);
            return _comprobantes;
        }
    }

    public IProductoRepository Productos
    {
        get
        {
            _productos ??= new ProductoRepository(CurrentConnection, _transaction);
            return _productos;
        }
    }

    public IGuiaRemisionRepository Guias
    {
        get
        {
            _guias ??= new GuiaRemisionRepository(CurrentConnection, _transaction);
            return _guias;
        }
    }

    public IGuiaRemisionDetalleRepository GuiaDetalles
    {
        get
        {
            _guiaDetalles ??= new GuiaRemisionDetalleRepository(CurrentConnection, _transaction);
            return _guiaDetalles;
        }
    }

    public IResumenComprobanteRepository ResumenComprobante
    {
        get
        {
            _resumenComprobante ??= new ResumenComprobanteRepository(CurrentConnection, _transaction);
            return _resumenComprobante;
        }
    }

    public IDashboardRepository Dashboard
    {
        get
        {
            _dashboard ??= new DashboardRepository(CurrentConnection, _transaction);
            return _dashboard;
        }
    }

    public IReportesRepository Reportes
    {
        get
        {
            _reportes ??= new ReportesRepository(CurrentConnection, _transaction);
            return _reportes;
        }
    }

    public ICuentasPorCobrarRepository CuentasPorCobrar
    {
        get
        {
            _cuentasPorCobrar ??= new CuentasPorCobrarRepository(CurrentConnection, _transaction);
            return _cuentasPorCobrar;
        }
    }

    public ITrabajadorRepository Trabajadores
    {
        get
        {
            _trabajadores ??= new TrabajadorRepository(CurrentConnection, _transaction);
            return _trabajadores;
        }
    }

    public void BeginTransaction()
    {
        _transaction = CurrentConnection.BeginTransaction();
        ResetRepositories();
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
            ResetRepositories();
        }
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
        ResetRepositories();
    }

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
        _trabajadores = null;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        return new DapperRepository<T>(CurrentConnection, _transaction);
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