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
    IClienteRepository Clientes { get; }
    IDireccionRepository Direcciones { get; }
>>>>>>> 69be5bd52a6a446f8c33d023d17045181f77844a
    IEmpresaRepository Empresas { get; }
    INoteRepository Notes { get; }
    INoteDetailRepository NoteDetails { get; }
    INoteLegendRepository NoteLegends { get; }
<<<<<<< HEAD
    
    // Transacciones
=======
>>>>>>> 69be5bd52a6a446f8c33d023d17045181f77844a

    void BeginTransaction();
    void Commit();
    void Rollback();

    IRepository<T> Repository<T>() where T : class;
}