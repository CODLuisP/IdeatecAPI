namespace IdeatecAPI.Application.Features.Empresas.DTOs;

public class Base64ToFileRequestDto
{
    public string Base64 { get; set; } = string.Empty;        // contenido en Base64
    public string Extension { get; set; } = string.Empty;     // ej: .pfx, .png, .jpg
    public string? FileName { get; set; }                      // nombre opcional
}