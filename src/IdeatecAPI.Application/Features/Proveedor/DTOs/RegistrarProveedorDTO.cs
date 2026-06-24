namespace IdeatecAPI.Application.Features.Proveedor.DTOs;

public class RegistrarProveedorDTO
{
    public string? NumDocumento { get; set; }
    public string? RazonSocial { get; set; }
    public string? NombreComercial { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? PersonaContacto { get; set; }
    public string? RucEmpresa { get; set; }
    public int? IdUsuario { get; set; }
}
