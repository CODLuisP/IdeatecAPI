namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IUnitOfWork : IDisposable
{
    // Repositorios específicos
    ICategoriaRepository Categorias { get; }
    IUsuarioRepository Usuarios { get; } 
    IClienteRepository clientes {get; }
    
    // Transacciones
    void BeginTransaction();
    void Commit();
    void Rollback();
    
    // Repositorio genérico
    IRepository<T> Repository<T>() where T : class;
}