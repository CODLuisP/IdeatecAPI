
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;
public interface ISerieCorrelativoRepository : IRepository<SerieCorrelativo>
{
    Task<IEnumerable<SerieCorrelativo>> GetSerieCorrelativoAsync(int EmpresaId, string TipoComprobante);
    Task<int> RegistrarSerieCorrelativoAsync(SerieCorrelativo serieCorrelativo);
}