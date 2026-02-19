namespace IdeatecAPI.Application.Features.Empresas.DTOs;

public class CertificadoResponseDto
{
    public string Pem { get; set; } = string.Empty;   // certificado + clave privada en PEM codificado Base64
    public string Cer { get; set; } = string.Empty;   // solo certificado público en PEM codificado Base64
}