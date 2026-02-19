namespace IdeatecAPI.Application.Features.Empresas.DTOs;

public class FileToBase64Dto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string Base64 { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}