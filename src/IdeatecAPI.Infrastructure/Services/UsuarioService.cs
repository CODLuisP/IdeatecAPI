using IdeatecAPI.Application.Common.Interfaces.Persistence;
using IdeatecAPI.Application.Features.Auth.DTOs;
using IdeatecAPI.Domain.Entities;
using BCrypt.Net;

namespace IdeatecAPI.Infrastructure.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUnitOfWork _unitOfWork;

    public UsuarioService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        try
        {
            // 1. Verificar si el username ya existe
            var usuarioExistente = await _unitOfWork.Usuarios.GetByIdentifierAsync(request.Username);
            if (usuarioExistente != null)
            {
                return new RegisterResponseDto
                {
                    Success = false,
                    Message = "El username ya está registrado"
                };
            }

            // 2. Verificar si el email ya existe
            usuarioExistente = await _unitOfWork.Usuarios.GetByIdentifierAsync(request.Email);
            if (usuarioExistente != null)
            {
                return new RegisterResponseDto
                {
                    Success = false,
                    Message = "El email ya está registrado"
                };
            }

            // 3. Verificar si el RUC ya existe (si fue proporcionado)
            if (!string.IsNullOrEmpty(request.Ruc))
            {
                usuarioExistente = await _unitOfWork.Usuarios.GetByIdentifierAsync(request.Ruc);
                if (usuarioExistente != null)
                {
                    return new RegisterResponseDto
                    {
                        Success = false,
                        Message = "El RUC ya está registrado"
                    };
                }
            }

            // 4. Hashear la contraseña automáticamente
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 5. Crear el nuevo usuario
            var nuevoUsuario = new Usuario
            {
                Username = request.Username,
                Email = request.Email,
                Password = passwordHash,
                NombreCompleto = request.NombreCompleto,
                Rol = request.Rol ?? "usuario",
                Estado = true,
                Ruc = request.Ruc,
                RazonSocial = request.RazonSocial,
                TokenVersion = 0,
                FechaCreacion = DateTime.UtcNow
            };

            // 6. Guardar en la base de datos
            var usuarioId = await _unitOfWork.Usuarios.CreateAsync(nuevoUsuario);

            if (usuarioId <= 0)
            {
                return new RegisterResponseDto
                {
                    Success = false,
                    Message = "Error al crear el usuario"
                };
            }

            // 7. Retornar respuesta exitosa
            return new RegisterResponseDto
            {
                Success = true,
                Message = "Usuario registrado exitosamente",
                User = new UsuarioDto
                {
                    UsuarioID = usuarioId,
                    Username = nuevoUsuario.Username,
                    NombreCompleto = nuevoUsuario.NombreCompleto,
                    Email = nuevoUsuario.Email,
                    Rol = nuevoUsuario.Rol,
                    Ruc = nuevoUsuario.Ruc,
                    RazonSocial = nuevoUsuario.RazonSocial,
                    Imagen = nuevoUsuario.Imagen,
                    Estado = nuevoUsuario.Estado
                }
            };
        }
        catch (Exception)
        {
            return new RegisterResponseDto
            {
                Success = false,
                Message = "Error interno del servidor al registrar usuario"
            };
        }
    }

    public async Task<bool> ChangePasswordAsync(int usuarioId, string currentPassword, string newPassword)
    {
        try
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdAsync(usuarioId);

            if (usuario == null)
            {
                return false;
            }

            // Verificar contraseña actual
            bool passwordValida = BCrypt.Net.BCrypt.Verify(currentPassword, usuario.Password);

            if (!passwordValida)
            {
                return false;
            }

            // Hashear nueva contraseña
            usuario.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            usuario.FechaActualizacion = DateTime.UtcNow;

            // Invalidar tokens anteriores
            usuario.TokenVersion++;

            return await _unitOfWork.Usuarios.UpdateAsync(usuario);
        }
        catch (Exception)
        {
            return false;
        }
    }
}