namespace IdeatecAPI.Domain.Entities
{
    public class Usuario
    {
        public int UsuarioID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = "usuario";
        public bool Estado { get; set; } = true;
        public string? Ruc { get; set; }
        public string? RazonSocial { get; set; }
        public string? Imagen { get; set; }
        public int TokenVersion { get; set; } = 0;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimoAcceso { get; set; }
    }
}