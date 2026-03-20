namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IUnitOfWork : IDisposable
{
    ICategoriaRepository Categorias { get; }
    IUsuarioRepository Usuarios { get; } 
    IClienteRepository Clientes {get; }
    IDireccionRepository Direcciones {get;}
    IEmpresaRepository Empresas { get; }
    INoteRepository Notes { get; }
    INoteDetailRepository NoteDetails { get; }
    INoteLegendRepository NoteLegends { get; }
    IComunicacionBajaRepository Bajas { get; }
    IComunicacionBajaDetalleRepository BajaDetalles { get; }
    IComprobanteRepository Comprobantes { get; }
    IProductoRepository Productos { get; }
    ISucursalRepository Sucursal { get; }
    IGuiaRemisionRepository Guias { get; }
    IGuiaRemisionDetalleRepository GuiaDetalles { get; }
    IResumenComprobanteRepository ResumenComprobante { get; }
    
    // Transacciones

    void BeginTransaction();
    void Commit();
    void Rollback();

    IRepository<T> Repository<T>() where T : class;
}