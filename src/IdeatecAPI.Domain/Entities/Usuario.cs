namespace IdeatecAPI.Domain.Entities
{
    public class Usuario
{
    public int UsuarioID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? SucursalID { get; set; }
    public string? NombreSucursal { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? EmailVerified { get; set; }
    public string? Ruc { get; set; }
    public int TokenVersion { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? FechaUltimoAcceso { get; set; }
    public DateTime? FechaActualizacion { get; set; }
 
    // ── Nuevos campos para recuperación de contraseña ──
    public string? ResetPasswordToken { get; set; }
    public DateTime? ResetPasswordExpires { get; set; }
 
    // ── Método helper ──
    public bool TieneTokenValido(string token) =>
        ResetPasswordToken == token &&
        ResetPasswordExpires.HasValue &&
        ResetPasswordExpires.Value > DateTime.UtcNow;
}
}