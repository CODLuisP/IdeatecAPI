using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Auth.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Generar hash BCrypt
    /// POST: api/auth/generate-hash
    /// </summary>
    [HttpPost("generate-hash")]
    [AllowAnonymous]
    public IActionResult GenerateHash([FromBody] string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        // Verificar que el hash funciona
        var verifies = BCrypt.Net.BCrypt.Verify(password, hash);

        return Ok(new
        {
            password = password,
            hash = hash,
            verifies = verifies
        });
    }

    /// <summary>
    /// Login de usuario
    /// POST: api/auth/login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
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

        var result = await _authService.LoginAsync(request);

        if (!result.Success)
        {
            return Unauthorized(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresAt = result.ExpiresAt,
            user = result.User
        });
    }

    /// <summary>
    /// Refrescar token
    /// POST: api/auth/refresh-token
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
    {
        var result = await _authService.RefreshTokenAsync(refreshToken);

        if (!result.Success)
        {
            return Unauthorized(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            success = true,
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresAt = result.ExpiresAt
        });
    }

    /// <summary>
    /// Logout (invalidar refresh token)
    /// POST: api/auth/logout
    /// </summary>
    [HttpPost("logout")]
    [Authorize] // Requiere estar autenticado
    public async Task<IActionResult> Logout()
    {
        // Obtener el ID del usuario del token JWT
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int usuarioId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Token inválido"
            });
        }

        var result = await _authService.LogoutAsync(usuarioId);

        if (!result)
        {
            return BadRequest(new
            {
                success = false,
                message = "Error al cerrar sesión"
            });
        }

        return Ok(new
        {
            success = true,
            message = "Sesión cerrada correctamente"
        });
    }

    /// <summary>
    /// Endpoint de prueba para verificar autenticación
    /// GET: api/auth/me
    /// </summary>
    [HttpGet("me")]
    [Authorize] // Solo usuarios autenticados
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var rol = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var nombreCompleto = User.FindFirst("nombreCompleto")?.Value;
        var ruc = User.FindFirst("ruc")?.Value;

        return Ok(new
        {
            success = true,
            user = new
            {
                usuarioID = userId,
                username = username,
                email = email,
                rol = rol,
                nombreCompleto = nombreCompleto,
                ruc = ruc
            }
        });
    }
}