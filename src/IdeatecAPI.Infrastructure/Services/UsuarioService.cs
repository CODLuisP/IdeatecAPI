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

        // 2. Hashear la contraseña automáticamente
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // 3. Crear el nuevo usuario
        var nuevoUsuario = new Usuario
        {
            Username = request.Username,
            Email = request.Email,
            Password = passwordHash,
            Rol = request.Rol ?? "usuario",
            Estado = true,
            Ruc = request.Ruc,
            TokenVersion = 0,
            FechaCreacion = DateTime.UtcNow
        };

        // 4. Guardar en la base de datos
        var usuarioId = await _unitOfWork.Usuarios.CreateAsync(nuevoUsuario);

        if (usuarioId <= 0)
        {
            return new RegisterResponseDto
            {
                Success = false,
                Message = "Error al crear el usuario"
            };
        }

        return new RegisterResponseDto
        {
            Success = true,
            Message = "Usuario registrado exitosamente",
            User = new UsuarioDto
            {
                UsuarioID = usuarioId,
                Username = nuevoUsuario.Username,
                Email = nuevoUsuario.Email,
                Rol = nuevoUsuario.Rol,
                Ruc = nuevoUsuario.Ruc,
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