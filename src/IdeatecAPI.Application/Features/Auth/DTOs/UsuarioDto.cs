namespace IdeatecAPI.Application.Features.Auth.DTOs;

public class UsuarioDto
    {
        public int UsuarioID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? SucursalID { get; set; }
        public string? NombreSucursal { get; set; }
        public string? NombreEmpresa { get; set; }

        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string? Ruc { get; set; }
        public bool Estado { get; set; }                    // ← AGREGAR
        public DateTime? FechaUltimoAcceso { get; set; } 
    }