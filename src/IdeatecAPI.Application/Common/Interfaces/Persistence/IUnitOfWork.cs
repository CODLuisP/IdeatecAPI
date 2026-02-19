namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IUnitOfWork : IDisposable
{
    ICategoriaRepository Categorias { get; }
<<<<<<< HEAD
    IUsuarioRepository Usuarios { get; }
    IEmpresaRepository Empresas { get; }


    // Agregar junto a ICategoriaRepository, IUsuarioRepository, IEmpresaRepository:
    INoteRepository Notes { get; }
    INoteDetailRepository NoteDetails { get; }
    INoteLegendRepository NoteLegends { get; }


=======
    IUsuarioRepository Usuarios { get; } 
    IClienteRepository clientes {get; }
    
    // Transacciones
    IEmpresaRepository Empresas { get; } 
>>>>>>> 4981bb4898f122372199cb065b3613abd4469e12
    void BeginTransaction();
    void Commit();
    void Rollback();


    IRepository<T> Repository<T>() where T : class;
}