using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdeatecAPI.Domain.Entities;

namespace IdeatecAPI.Application.Common.Interfaces.Persistence;
public interface IResumenComprobanteRepository : IRepository<ResumenComprobante>
{
    Task<IEnumerable<ResumenComprobante>> GetAllResumenComprobanteAsync();
    Task<ResumenComprobante?> GetResumenComprobanteByIdAsync(int id);
    Task<int> RegistrarResumenComprobanteAsync(ResumenComprobante resumenComprobante);


    Task<bool> ExisteIdentificadorAsync(string identificador);
    // IComprobanteResumenRepository
    Task UpdateEstadoSunatAsync(int resumenId, string estado, string ticket,
        string codigoRespuesta, string mensajeRespuesta, string xmlGenerado, DateTime fechaEnvio);
}