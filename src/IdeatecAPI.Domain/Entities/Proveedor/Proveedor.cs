namespace IdeatecAPI.Domain.Entities;
public class Proveedor
{
    public int ProveedorId { get; set; }
    public string? NumDocumento { get; set; }
    public string? RazonSocial { get; set; }
    public string? NombreComercial { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? PersonaContacto { get; set; }
    public string? RucEmpresa { get; set; }
    public bool? Estado { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? IdUsuario { get; set; }
}
