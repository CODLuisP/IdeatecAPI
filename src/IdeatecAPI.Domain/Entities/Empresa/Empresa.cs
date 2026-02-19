namespace IdeatecAPI.Domain.Entities;

public class Empresa
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
    public string? SolUsuario { get; set; }
    public string? SolClave { get; set; }
    public bool Activo { get; set; }
    public DateTime CreadoEn { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? LogoBase64 { get; set; }
    public string? CertificadoPem { get; set; }
    public string? CertificadoPassword { get; set; }
    public string? ClienteId { get; set; }
    public string? ClientSecret { get; set; }
    public string Plan { get; set; } = "free";
    public string Environment { get; set; } = "beta";
    public DateTime? ActualizadoEn { get; set; }
}