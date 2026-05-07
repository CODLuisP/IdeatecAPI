namespace IdeatecAPI.Application.Common.Interfaces;

public interface IStorageService
{
    Task<string> SubirZipAsync(string ruc, string tipoComprobante, string nombreArchivo, byte[] zipBytes);

    Task<string> SubirCdrAsync(string ruc, string tipoComprobante, string nombreArchivo, string cdrBase64);
}