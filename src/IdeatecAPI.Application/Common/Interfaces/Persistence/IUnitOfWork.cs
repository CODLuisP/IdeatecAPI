namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IUnitOfWork : IDisposable
{
    ICategoriaRepository Categorias { get; }
<<<<<<< HEAD
    IUsuarioRepository Usuarios { get; } 
    IClienteRepository Clientes {get; }
    IDireccionRepository Direcciones {get;}
=======
    IUsuarioRepository Usuarios { get; }
    IClienteRepository clientes {get; }
    IEmpresaRepository Empresas { get; }
    INoteRepository Notes { get; }
    INoteDetailRepository NoteDetails { get; }
    INoteLegendRepository NoteLegends { get; }
>>>>>>> e14d2dbe1d2ba38aad9401ce548c62fa911aef41
    
    // Transacciones

    void BeginTransaction();
    void Commit();
    void Rollback();


    IRepository<T> Repository<T>() where T : class;
}