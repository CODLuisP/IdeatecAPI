using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Auth.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;                                                    // ← AGREGAR
using IdeatecAPI.Application.Features.Auth.ForgotPassword;        // ← AGREGAR
using IdeatecAPI.Application.Features.Auth.ResetPassword;  

namespace IdeatecAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;     

    public AuthController(IAuthService authService, IMediator mediator)
    {
        _authService = authService;
        _mediator = mediator;

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
    /// Solicitar enlace de recuperación de contraseña
    /// POST: api/auth/forgot-password
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EmailOrUsername))
            return BadRequest(new { success = false, message = "El campo es requerido." });

        var result = await _mediator.Send(new ForgotPasswordCommand(request.EmailOrUsername));

        // Siempre 200 para no revelar si el email existe
        return Ok(new { success = true, message = result.Message });
    }

    /// <summary>
    /// Restablecer contraseña con token
    /// POST: api/auth/reset-password
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { success = false, message = "Token y contraseña son requeridos." });

        var result = await _mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword));

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message });
    }
}