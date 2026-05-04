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
            // 1. Verificar username
            var usuarioExistente = await _unitOfWork.Usuarios.GetByIdentifierAsync(request.Username);
            if (usuarioExistente != null)
                return new RegisterResponseDto { Success = false, Message = "El username ya está registrado" };

            // 2. Verificar email + RUC
            usuarioExistente = await _unitOfWork.Usuarios.GetByIdentifierAsync(request.Email);
            if (usuarioExistente != null && usuarioExistente.Ruc != request.Ruc)
                return new RegisterResponseDto { Success = false, Message = "El email ya está registrado con otro RUC" };

            // 3. Hashear la contraseña automáticamente
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // 4. Crear el nuevo usuario
            var nuevoUsuario = new Usuario
            {
                Username = request.Username,
                Email = request.Email,
                Password = passwordHash,
                SucursalID = request.SucursalID,
                Rol = request.Rol ?? "usuario",
                Estado = true,
                Ruc = request.Ruc,
                TokenVersion = 0,
                FechaCreacion = DateTime.UtcNow
            };

            // 5. Guardar en la base de datos
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
                    Estado = nuevoUsuario.Estado,
                    SucursalID = nuevoUsuario.SucursalID
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

    public async Task<RegisterResponseDto> RegisterEmpresaCompletoAsync(RegisterEmpresaCompletoDto dto)
    {
        // ── Validaciones ANTES de abrir la transacción ────────────────────────────
        // Los repos se usan aquí sin transacción activa, sin problema.

        var usuarioPorUsername = await _unitOfWork.Usuarios.GetByIdentifierAsync(dto.Username);
        if (usuarioPorUsername != null)
            return new RegisterResponseDto { Success = false, Message = "El username ya está registrado" };

        var usuarioPorEmail = await _unitOfWork.Usuarios.GetByIdentifierAsync(dto.Email);
        if (usuarioPorEmail != null && usuarioPorEmail.Ruc != dto.Ruc)
            return new RegisterResponseDto { Success = false, Message = "El email ya está registrado con otro RUC" };

        var empresaExistente = await _unitOfWork.Empresas.GetEmpresaByRucAsync(dto.Ruc);
        if (empresaExistente != null)
            return new RegisterResponseDto { Success = false, Message = "El RUC ya tiene una empresa registrada" };

        // ── Abrir transacción DESPUÉS de las validaciones ─────────────────────────
        // BeginTransaction() llama ResetRepositories(), así que los repos
        // se recrean limpios con _transaction activa desde aquí en adelante.
        _unitOfWork.BeginTransaction();

        try
        {
            // ── 1. Crear empresa ──────────────────────────────────────────────────
            var empresa = new Empresa
            {
                Ruc = dto.Ruc,
                RazonSocial = dto.RazonSocial,
                NombreComercial = dto.NombreComercial,
                Direccion = dto.Direccion,
                Ubigeo = dto.Ubigeo,
                Urbanizacion = dto.Urbanizacion,
                Provincia = dto.Provincia,
                Departamento = dto.Departamento,
                Distrito = dto.Distrito,
                Telefono = dto.Telefono,
                Email = dto.Email,
                Plan = "free",
                Environment = "beta",
                Activo = true,
                CreadoEn = DateTime.UtcNow
            };

            await _unitOfWork.Empresas.CreateEmpresaAsync(empresa);

            // ── 2. Obtener sucursal Principal creada por el trigger ───────────────
            var sucursales = await _unitOfWork.Sucursal.GetByRucSucursalAsync(dto.Ruc);
            var sucursal = sucursales?.FirstOrDefault();

            if (sucursal == null)
                throw new InvalidOperationException(
                    "No se encontró la sucursal principal. Verifica que el trigger de MySQL esté activo.");

            // ── 3. Crear usuario admin ────────────────────────────────────────────
            var nuevoUsuario = new Usuario
            {
                Username = dto.Username,
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                SucursalID = sucursal.SucursalId.ToString(),
                Rol = "admin",
                Estado = true,
                Ruc = dto.Ruc,
                TokenVersion = 0,
                FechaCreacion = DateTime.UtcNow
            };

            var usuarioId = await _unitOfWork.Usuarios.CreateAsync(nuevoUsuario);

            if (usuarioId <= 0)
                throw new InvalidOperationException("No se pudo crear el usuario.");

            // ── 4. Confirmar todo ─────────────────────────────────────────────────
            _unitOfWork.Commit();

            return new RegisterResponseDto
            {
                Success = true,
                Message = "Registro completado exitosamente",
                User = new UsuarioDto
                {
                    UsuarioID = usuarioId,
                    Username = nuevoUsuario.Username,
                    Email = nuevoUsuario.Email,
                    Rol = nuevoUsuario.Rol,
                    Ruc = nuevoUsuario.Ruc,
                    Estado = nuevoUsuario.Estado,
                    SucursalID = nuevoUsuario.SucursalID
                }
            };
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1062)
        {
            _unitOfWork.Rollback();

            var message = ex.Message.ToLower() switch
            {
                var m when m.Contains("username") => "El nombre de usuario ya está registrado",
                var m when m.Contains("email") => "El correo ya está registrado con otro RUC",
                var m when m.Contains("ruc") => "El RUC ya tiene una empresa registrada",
                _ => "Ya existe un registro con esos datos"
            };

            return new RegisterResponseDto { Success = false, Message = message };
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            return new RegisterResponseDto
            {
                Success = false,
                Message = $"Error al completar el registro: {ex.Message}"
            };
        }
    }
}