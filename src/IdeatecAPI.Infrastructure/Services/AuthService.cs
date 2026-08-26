using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Auth.DTOs;
using IdeatecAPI.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace IdeatecAPI.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IServiceScopeFactory _scopeFactory;

    public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService, IServiceScopeFactory scopeFactory)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _scopeFactory = scopeFactory;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        try
        {
            // ── Establecer el entorno antes de autenticar ──
            var env = request.Environment?.ToLower() ?? "production";
            _unitOfWork.SetEnvironment(env);

            // 1. Buscar usuario (1 roundtrip DB con JOIN sucursal + empresa)
            var usuario = await _unitOfWork.Usuarios.GetByIdentifierAsync(request.Identifier);

            if (usuario == null)
                return new LoginResponseDto { Success = false, Message = "Credenciales incorrectas" };

            // 2. Verificar cuenta activa
            if (!usuario.Estado)
                return new LoginResponseDto { Success = false, Message = "La cuenta está desactivada. Contacte al administrador." };

            // 3. Verificar contraseña
            if (request.Password != usuario.Password)
                return new LoginResponseDto { Success = false, Message = "Credenciales incorrectas" };

            usuario.Environment = env;

            // 4. Generar tokens (in-memory, <1ms)
            var accessToken  = _tokenService.GenerateAccessToken(usuario);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var expiresAt    = DateTime.UtcNow.AddMinutes(_tokenService.GetAccessTokenExpirationMinutes());

            // 5. ⚡ Fire & forget: guardar refreshToken + fecha en background
            //    El cliente NO espera este roundtrip — ahorra ~150ms
            var usuarioId = usuario.UsuarioID;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    uow.SetEnvironment(env);
                    await uow.Usuarios.UpdateRefreshTokenAndLastAccessAsync(usuarioId, refreshToken);
                }
                catch { /* no bloquear el login si falla el update */ }
            });

            // 6. Respuesta inmediata sin esperar el UPDATE
            return new LoginResponseDto
            {
                Success = true,
                Message = "Login exitoso",
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt    = expiresAt,
                User = new UsuarioDto
                {
                    UsuarioID      = usuario.UsuarioID,
                    Username       = usuario.Username,
                    Email          = usuario.Email,
                    Rol            = usuario.Rol,
                    Ruc            = usuario.Ruc,
                    SucursalID     = usuario.SucursalID,
                    NombreSucursal = usuario.NombreSucursal,
                    Igv            = usuario.Igv,
                    TipoEmision    = usuario.TipoEmision,
                    NombreEmpresa  = usuario.NombreEmpresa,
                    Environment    = usuario.Environment
                }
            };
        }
        catch (Exception)
        {
            return new LoginResponseDto
            {
                Success = false,
                Message = "Error interno del servidor. Intente nuevamente."
            };
        }
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Refresh token es requerido"
                };
            }

            // 1. Buscar usuario por refresh token (producción primero, luego beta)
            var usuario = await _unitOfWork.Usuarios.GetByRefreshTokenAsync(refreshToken);
            if (usuario == null)
            {
                _unitOfWork.SetEnvironment("beta");
                usuario = await _unitOfWork.Usuarios.GetByRefreshTokenAsync(refreshToken);
                if (usuario != null)
                {
                    usuario.Environment = "beta";
                }
            }
            else
            {
                usuario.Environment = "production";
            }

            if (usuario == null)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Refresh token inválido o expirado"
                };
            }

            // 2. Verificar cuenta activa
            if (!usuario.Estado)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "La cuenta está desactivada"
                };
            }

            // 3. Generar nuevos tokens
            var newAccessToken  = _tokenService.GenerateAccessToken(usuario);
            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var expiresAt       = DateTime.UtcNow.AddMinutes(_tokenService.GetAccessTokenExpirationMinutes());

            // 4. Actualizar refresh token en BD (fire & forget para no bloquear)
            var usuarioId = usuario.UsuarioID;
            var env = usuario.Environment ?? "production";
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    uow.SetEnvironment(env);
                    await uow.Usuarios.UpdateRefreshTokenAndLastAccessAsync(usuarioId, newRefreshToken);
                }
                catch { /* no bloquear el refresh si falla el update */ }
            });

            // 5. Respuesta con nuevos tokens
            return new LoginResponseDto
            {
                Success = true,
                Message = "Token renovado exitosamente",
                AccessToken  = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt    = expiresAt,
                User = new UsuarioDto
                {
                    UsuarioID      = usuario.UsuarioID,
                    Username       = usuario.Username,
                    Email          = usuario.Email,
                    Rol            = usuario.Rol,
                    Ruc            = usuario.Ruc,
                    SucursalID     = usuario.SucursalID,
                    NombreSucursal = usuario.NombreSucursal,
                    Igv            = usuario.Igv,
                    TipoEmision    = usuario.TipoEmision,
                    NombreEmpresa  = usuario.NombreEmpresa,
                    Environment    = usuario.Environment
                }
            };
        }
        catch (Exception)
        {
            return new LoginResponseDto
            {
                Success = false,
                Message = "Error al refrescar token"
            };
        }
    }

    public async Task<bool> LogoutAsync(int usuarioId)
    {
        try
        {
            await _unitOfWork.Usuarios.UpdateRefreshTokenAsync(usuarioId, string.Empty);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}