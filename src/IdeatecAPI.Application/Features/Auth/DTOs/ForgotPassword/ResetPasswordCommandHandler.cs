using MediatR;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using BC = BCrypt.Net.BCrypt;

namespace IdeatecAPI.Application.Features.Auth.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResult>
{
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ResetPasswordResult> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar usuario por token
        var usuario = await _unitOfWork.Usuarios.GetByResetTokenAsync(request.Token);

        if (usuario is null)
            return new ResetPasswordResult(false, "El enlace de recuperación no es válido.");

        // 2. Verificar que el token no haya expirado
        if (!usuario.TieneTokenValido(request.Token))
            return new ResetPasswordResult(false, "El enlace de recuperación ha expirado. Solicita uno nuevo.");

        // 3. Validar longitud mínima
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return new ResetPasswordResult(false, "La contraseña debe tener al menos 8 caracteres.");

        // 4. Hashear con BCrypt
        var hashedPassword = BC.HashPassword(request.NewPassword, workFactor: 12);

        // 5. Actualizar contraseña y limpiar token
        await _unitOfWork.Usuarios.UpdatePasswordAsync(usuario.UsuarioID, hashedPassword);

        return new ResetPasswordResult(true, "Contraseña actualizada correctamente.");
    }
}