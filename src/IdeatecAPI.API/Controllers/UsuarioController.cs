using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Auth.DTOs;
using IdeatecAPI.Application.Features.Usuario.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuarioController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsuarioService _usuarioService;


    public UsuarioController(IUnitOfWork unitOfWork, IUsuarioService usuarioService) // ← CAMBIAR
    {
        _unitOfWork = unitOfWork;
        _usuarioService = usuarioService; // ← CAMBIAR
    }

    /// <summary>
    /// Registrar nuevo usuario
    /// POST: api/usuario/register
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Datos inválidos",
                errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
            });
        }

        var result = await _usuarioService.RegisterAsync(request); // ← USA _usuarioService

        if (!result.Success)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            user = result.User
        });
    }
    /// <summary>
    /// Obtener todos los usuarios
    /// GET: api/usuario
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool incluirInactivos = false)
    {
        var ruc = User.FindFirst("ruc")?.Value;
        var sucursalID = User.FindFirst("sucursalID")?.Value;
        var rolClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        string? filtrarPorSucursal = null;
        int? filtrarPorUsuario = null;

        if (rolClaim == "superadmin")
        {
            filtrarPorSucursal = null; // ve toda la empresa
        }
        else if (rolClaim == "admin")
        {
            filtrarPorSucursal = sucursalID; // ve su sucursal
        }
        else
        {
            // facturador → solo se ve a sí mismo
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out var uid)) filtrarPorUsuario = uid;
        }

        var usuarios = await _unitOfWork.Usuarios.GetAllAsync(!incluirInactivos, ruc, filtrarPorSucursal, filtrarPorUsuario);

        var usuariosDto = usuarios.Select(u => new UsuarioDto
        {
            UsuarioID = u.UsuarioID,
            Username = u.Username,
            Email = u.Email,
            Rol = u.Rol,
            Ruc = u.Ruc,
            SucursalID = u.SucursalID,
            NombreSucursal = u.NombreSucursal,
            Estado = u.Estado,
            FechaUltimoAcceso = u.FechaUltimoAcceso
        });

        return Ok(new { success = true, data = usuariosDto, total = usuariosDto.Count() });
    }

    /// <summary>
    /// Actualizar usuario
    /// PUT: api/usuario/{id}
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUsuarioDto dto)
    {
        if (id != dto.UsuarioID)
            return BadRequest(new { success = false, message = "ID no coincide" });

        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Datos inválidos" });

        var usuarioExistente = await _unitOfWork.Usuarios.GetByIdAsync(id);
        if (usuarioExistente == null)
            return NotFound(new { success = false, message = "Usuario no encontrado" });

        var existe = await _unitOfWork.Usuarios.ExistsAsync(dto.Username, null, null, id);
        if (existe)
            return BadRequest(new { success = false, message = "El username o email ya están en uso" });

        usuarioExistente.Username = dto.Username;
        usuarioExistente.Email = dto.Email;

        if (!string.IsNullOrEmpty(dto.Rol))
            usuarioExistente.Rol = dto.Rol;

        if (!string.IsNullOrEmpty(dto.NuevaContrasena))
            usuarioExistente.Password = BCrypt.Net.BCrypt.HashPassword(dto.NuevaContrasena);

        var actualizado = await _unitOfWork.Usuarios.UpdateAsync(usuarioExistente);
        if (!actualizado)
            return BadRequest(new { success = false, message = "Error al actualizar" });

        return Ok(new { success = true, message = "Usuario actualizado correctamente" });
    }

    /// <summary>
    /// Eliminar usuario (soft delete)
    /// DELETE: api/usuario/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUserId = User.FindFirst("sub")?.Value;
        if (currentUserId == id.ToString())
            return BadRequest(new { success = false, message = "No puedes eliminar tu propia cuenta" });

        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(id);
        if (usuario == null)
            return NotFound(new { success = false, message = "Usuario no encontrado" });

        var eliminado = await _unitOfWork.Usuarios.DeleteAsync(id);
        if (!eliminado)
            return BadRequest(new { success = false, message = "Error al eliminar" });

        return Ok(new { success = true, message = "Usuario eliminado correctamente" });
    }

    /// <summary>
    /// Cambiar contraseña
    /// POST: api/usuario/change-password
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Datos inválidos",
                errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (currentUserId != dto.UsuarioID.ToString())
        {
            return Forbid();
        }

        var resultado = await _usuarioService.ChangePasswordAsync(
            dto.UsuarioID,
            dto.CurrentPassword,
            dto.NewPassword
        );

        if (!resultado)
        {
            return BadRequest(new
            {
                success = false,
                message = "Error al cambiar la contraseña. Verifica que la contraseña actual sea correcta."
            });
        }

        return Ok(new
        {
            success = true,
            message = "Contraseña actualizada correctamente. Debes iniciar sesión nuevamente."
        });
    }

    /// <summary>
    /// Obtener usuarios por RUC y opcionalmente por SucursalID
    /// GET: api/usuario/por-ruc?ruc=12345678901&sucursalID=S01
    /// </summary>
    [HttpGet("por-ruc")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> GetByRuc(
        [FromQuery] string ruc,
        [FromQuery] string? sucursalID = null)
    {
        if (string.IsNullOrWhiteSpace(ruc))
            return BadRequest(new { success = false, message = "El RUC es obligatorio" });

        var usuarios = await _unitOfWork.Usuarios.GetAllAsync(
            soloActivos: true,
            ruc: ruc,
            sucursalID: sucursalID
        );

        var resultado = usuarios.Select(u => new UsuarioPorRucDto
        {
            UsuarioID = u.UsuarioID,
            Username = u.Username,
            Rol = u.Rol,
            SucursalID = u.SucursalID,
            Email = u.Email,
            Ruc = u.Ruc
        });

        return Ok(new
        {
            success = true,
            data = resultado,
            total = resultado.Count()
        });
    }
}