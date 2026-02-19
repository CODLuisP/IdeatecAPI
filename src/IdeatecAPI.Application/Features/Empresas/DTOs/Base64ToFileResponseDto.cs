namespace IdeatecAPI.Application.Features.Empresas.DTOs;

public class Base64ToFileResponseDto
{
    public byte[] Bytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}