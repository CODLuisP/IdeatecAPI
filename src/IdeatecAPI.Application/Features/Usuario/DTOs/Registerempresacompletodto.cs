using System.ComponentModel.DataAnnotations;

namespace IdeatecAPI.Application.Features.Auth.DTOs;

/// <summary>
/// DTO para el endpoint unificado POST api/usuario/register-completo
/// Registra empresa + sucursal principal + usuario admin en una sola transacción.
/// </summary>
public class RegisterEmpresaCompletoDto
{
    // ── Datos de la empresa (vienen validados desde SUNAT/APISPERU en el front) ──

    [Required(ErrorMessage = "El RUC es obligatorio")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "El RUC debe tener exactamente 11 dígitos")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "El RUC debe contener solo dígitos")]
    public string Ruc { get; set; } = string.Empty;

    [Required(ErrorMessage = "La razón social es obligatoria")]
    [StringLength(200, ErrorMessage = "La razón social no puede superar 200 caracteres")]
    public string RazonSocial { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NombreComercial { get; set; }

    [StringLength(300)]
    public string? Direccion { get; set; }

    [StringLength(6)]
    public string? Ubigeo { get; set; }

    [StringLength(100)]
    public string? Urbanizacion { get; set; }

    [StringLength(100)]
    public string? Provincia { get; set; }

    [StringLength(100)]
    public string? Departamento { get; set; }

    [StringLength(100)]
    public string? Distrito { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    // ── Datos del usuario admin ──

    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "El usuario debe tener entre 4 y 50 caracteres")]
    [RegularExpression(@"^\S+$", ErrorMessage = "El usuario no puede contener espacios")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    public string Password { get; set; } = string.Empty;
}