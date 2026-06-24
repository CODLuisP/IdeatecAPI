namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IUnitOfWork : IDisposable
{
    ICategoriaRepository Categorias { get; }
    IUsuarioRepository Usuarios { get; }
    IClienteRepository Clientes { get; }
    IDireccionRepository Direcciones { get; }
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
    IDashboardRepository Dashboard { get; }
    IReportesRepository Reportes { get; }
    ICuentasPorCobrarRepository CuentasPorCobrar { get; }
    IDeudaContadoRepository DeudaContado { get; }
    ITrabajadorRepository Trabajadores { get; }
    IPlantillaVelsatRepository PlantillaVelsat { get; }
    INotificacionEnviadaRepository NotificacionesEnviadas { get; }
    INotificacionDiasRepository NotificacionDias { get; }
    IValeRepository Vales { get; }
    IConfiguracionRepository Configuracion { get; }
    IProveedorRepository Proveedores { get; }
    ICompraProveedorRepository ComprasProveedor { get; }

    void SetEnvironment(string env);

    // Transacciones
    void BeginTransaction();
    void Commit();
    void Rollback();

    IRepository<T> Repository<T>() where T : class;
}