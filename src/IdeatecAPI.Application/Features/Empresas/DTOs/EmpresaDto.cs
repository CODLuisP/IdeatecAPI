namespace IdeatecAPI.Application.Features.Empresas.DTOs;

public class EmpresaDto
{
    public int Id { get; set; }
    public string Ruc { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string? Direccion { get; set; }
    public string? Ubigeo { get; set; }
    public string? Urbanizacion { get; set; }
    public string? Provincia { get; set; }
    public string? Departamento { get; set; }
    public string? Distrito { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? LogoBase64 { get; set; }
    public string Plan { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool TieneCertificado { get; set; }
    public bool TieneCredencialesSunat { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}