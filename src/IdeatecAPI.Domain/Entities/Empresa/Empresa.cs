namespace IdeatecAPI.Domain.Entities;

public class Empresa
{
    public int Id { get; set; }
    public string Ruc { get; set; } = string.Empty;
    public double Igv { get; set; } = 18;
    public bool TipoEmision { get; set; } = true;
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
    // Logo específico para documentos PDF (comprobantes, guías, reportes). Si no está
    // configurado, se usa LogoBase64 (el de comprobantes/HTML) como respaldo, para que
    // los PDFs no queden sin logo mientras el usuario no suba uno propio.
    public string? LogoPdfBase64 { get; set; }
    public string? LogoPdfEfectivo => string.IsNullOrEmpty(LogoPdfBase64) ? LogoBase64 : LogoPdfBase64;
    public string? CertificadoPem { get; set; }
    public string? CertificadoPassword { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string Plan { get; set; } = "free";
    public string Environment { get; set; } = "beta";
    public DateTime? ActualizadoEn { get; set; }
}