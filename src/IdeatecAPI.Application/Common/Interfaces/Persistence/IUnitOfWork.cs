namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IUnitOfWork : IDisposable
{
    ICategoriaRepository Categorias { get; }
    IUsuarioRepository Usuarios { get; } 
<<<<<<< HEAD
    IClienteRepository clientes {get; }
    
    // Transacciones
=======
    IEmpresaRepository Empresas { get; } 
>>>>>>> da9220907266fa87052a45d1d39c9f0da1a8a1b2
    void BeginTransaction();
    void Commit();
    void Rollback();
    
    IRepository<T> Repository<T>() where T : class;
}