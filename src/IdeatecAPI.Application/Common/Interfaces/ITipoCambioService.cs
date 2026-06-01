namespace IdeatecAPI.Application.Common.Interfaces;

public interface ITipoCambioService
{
    /// <summary>
    /// Retorna el tipo de cambio venta oficial SUNAT para la fecha indicada.
    /// Si la llamada falla devuelve null.
    /// </summary>
    Task<decimal?> GetTipoCambioVentaAsync(DateTime? fecha = null);
}
