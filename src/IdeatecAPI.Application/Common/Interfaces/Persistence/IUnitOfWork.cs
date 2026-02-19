namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IUnitOfWork : IDisposable
{
    ICategoriaRepository Categorias { get; }
    IUsuarioRepository Usuarios { get; }
    IEmpresaRepository Empresas { get; }


    // Agregar junto a ICategoriaRepository, IUsuarioRepository, IEmpresaRepository:
    INoteRepository Notes { get; }
    INoteDetailRepository NoteDetails { get; }
    INoteLegendRepository NoteLegends { get; }


    void BeginTransaction();
    void Commit();
    void Rollback();


    IRepository<T> Repository<T>() where T : class;
}