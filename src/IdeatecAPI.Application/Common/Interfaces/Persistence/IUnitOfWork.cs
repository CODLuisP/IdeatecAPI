namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IUnitOfWork : IDisposable
{
    ICategoriaRepository Categorias { get; }
    IUsuarioRepository Usuarios { get; } 
    IClienteRepository Clientes {get; }
    IDireccionRepository Direcciones {get;}
    
    // Transacciones
    IEmpresaRepository Empresas { get; } 
    void BeginTransaction();
    void Commit();
    void Rollback();
    
    IRepository<T> Repository<T>() where T : class;
}