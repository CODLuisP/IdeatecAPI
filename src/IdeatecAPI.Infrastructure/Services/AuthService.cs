using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Auth.DTOs;
using BCrypt.Net;

namespace IdeatecAPI.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public AuthService(IUnitOfWork unitOfWork, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        try
        {
            // 1. Buscar usuario por email, username o RUC
            var usuario = await _unitOfWork.Usuarios.GetByIdentifierAsync(request.Identifier);

            if (usuario == null)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Credenciales incorrectas"
                };
            }

            // 2. Verificar si la cuenta está activa
            if (!usuario.Estado)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "La cuenta está desactivada. Contacte al administrador."
                };
            }

            // 3. Verificar la contraseña
            bool passwordValida = BCrypt.Net.BCrypt.Verify(request.Password, usuario.Password);

            if (!passwordValida)
            {
                return new LoginResponseDto
                {
                    Success = false,
                    Message = "Credenciales incorrectas"
                };
            }

            // 4. Generar tokens
            var accessToken = _tokenService.GenerateAccessToken(usuario);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // 5. Guardar refresh token en BD
            await _unitOfWork.Usuarios.UpdateRefreshTokenAsync(usuario.UsuarioID, refreshToken);

            // 6. Actualizar última fecha de acceso
            await _unitOfWork.Usuarios.UpdateLastAccessAsync(usuario.UsuarioID);

            // 7. Calcular expiración del token
            var expiresAt = DateTime.UtcNow.AddMinutes(30); // Debe coincidir con appsettings

            // 8. Retornar respuesta exitosa
            return new LoginResponseDto
            {
                Success = true,
                Message = "Login exitoso",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = new UsuarioDto
                {
                    UsuarioID = usuario.UsuarioID,
                    Username = usuario.Username,
                    NombreCompleto = usuario.NombreCompleto,
                    Email = usuario.Email,
                    Rol = usuario.Rol,
                    Ruc = usuario.Ruc,
                    RazonSocial = usuario.RazonSocial,
                    Imagen = usuario.Imagen
                }
            };
        }
        catch (Exception)
        {
            // Log del error aquí si tienes un logger
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
            // Buscar usuario por refresh token
            // Por simplicidad, podrías agregar un método en el repository
            // Por ahora retornamos error
            return new LoginResponseDto
            {
                Success = false,
                Message = "Funcionalidad de refresh token no implementada aún"
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
            // Invalidar el refresh token
            await _unitOfWork.Usuarios.UpdateRefreshTokenAsync(usuarioId, string.Empty);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}