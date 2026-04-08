namespace IdeatecAPI.Application.Common.Interfaces.Persistence;

public interface IGuiaPdfService
{
    Task<byte[]> GenerarPdfAsync(int guiaId);
}