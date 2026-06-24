namespace IdeatecAPI.Application.Features.Proveedor.DTOs;

public class EditarProveedorDTO
{
    public int? ProveedorId { get; set; }
    public string? NumDocumento { get; set; }
    public string? RazonSocial { get; set; }
    public string? NombreComercial { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? PersonaContacto { get; set; }
}
