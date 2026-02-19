using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Auth.DTOs;
using IdeatecAPI.Application.Features.Usuario.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Todos los endpoints requieren autenticación
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
    [Authorize(Roles = "admin")]
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
    [Authorize(Roles = "admin")] // Solo admins pueden ver todos los usuarios
    public async Task<IActionResult> GetAll([FromQuery] bool incluirInactivos = false)
    {
        var usuarios = await _unitOfWork.Usuarios.GetAllAsync(!incluirInactivos);

        var usuariosDto = usuarios.Select(u => new UsuarioDto
        {
            UsuarioID = u.UsuarioID,
            Username = u.Username,
            NombreCompleto = u.NombreCompleto,
            Email = u.Email,
            Rol = u.Rol,
            Ruc = u.Ruc,
            RazonSocial = u.RazonSocial,
            Imagen = u.Imagen,
            Estado = u.Estado,
            FechaUltimoAcceso = u.FechaUltimoAcceso
        });

        return Ok(new
        {
            success = true,
            data = usuariosDto,
            total = usuariosDto.Count()
        });
    }

    /// <summary>
    /// Actualizar usuario
    /// PUT: api/usuario/{id}
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUsuarioDto dto)
    {
        if (id != dto.UsuarioID)
        {
            return BadRequest(new
            {
                success = false,
                message = "El ID de la ruta no coincide con el ID del cuerpo"
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Datos inválidos",
                errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

        // Obtener ID del usuario autenticado
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        // Verificar permisos: solo puede actualizar su propio perfil o ser admin
        if (currentUserId != id.ToString() && !User.IsInRole("admin"))
        {
            return Forbid();
        }

        // Verificar que el usuario existe
        var usuarioExistente = await _unitOfWork.Usuarios.GetByIdAsync(id);
        if (usuarioExistente == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Usuario no encontrado"
            });
        }

        // Verificar duplicados (username, email, ruc)
        var existe = await _unitOfWork.Usuarios.ExistsAsync(
            dto.Username,
            dto.Email,
            dto.Ruc,
            id
        );

        if (existe)
        {
            return BadRequest(new
            {
                success = false,
                message = "El username, email o RUC ya están en uso por otro usuario"
            });
        }

        // Actualizar campos
        usuarioExistente.Username = dto.Username;
        usuarioExistente.Email = dto.Email;
        usuarioExistente.NombreCompleto = dto.NombreCompleto;
        usuarioExistente.Ruc = dto.Ruc;
        usuarioExistente.RazonSocial = dto.RazonSocial;
        usuarioExistente.Imagen = dto.Imagen;

        // Solo admin puede cambiar el rol
        if (User.IsInRole("admin") && !string.IsNullOrEmpty(dto.Rol))
        {
            usuarioExistente.Rol = dto.Rol;
        }

        var actualizado = await _unitOfWork.Usuarios.UpdateAsync(usuarioExistente);

        if (!actualizado)
        {
            return BadRequest(new
            {
                success = false,
                message = "Error al actualizar el usuario"
            });
        }

        return Ok(new
        {
            success = true,
            message = "Usuario actualizado correctamente"
        });
    }

    /// <summary>
    /// Eliminar usuario (soft delete)
    /// DELETE: api/usuario/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")] // Solo admins pueden eliminar usuarios
    public async Task<IActionResult> Delete(int id)
    {
        var usuario = await _unitOfWork.Usuarios.GetByIdAsync(id);

        if (usuario == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Usuario no encontrado"
            });
        }

        // No permitir eliminar al propio usuario admin
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (currentUserId == id.ToString())
        {
            return BadRequest(new
            {
                success = false,
                message = "No puedes desactivar tu propia cuenta"
            });
        }

        var eliminado = await _unitOfWork.Usuarios.DeleteAsync(id);

        if (!eliminado)
        {
            return BadRequest(new
            {
                success = false,
                message = "Error al desactivar el usuario"
            });
        }

        return Ok(new
        {
            success = true,
            message = "Usuario desactivado correctamente"
        });
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
}