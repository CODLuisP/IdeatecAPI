using MediatR;
using IdeatecAPI.Application.Common.Interfaces;
using IdeatecAPI.Application.Common.Interfaces.Persistence;
using Microsoft.Extensions.Configuration;

namespace IdeatecAPI.Application.Features.Auth.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;

    public ForgotPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IConfiguration config)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _config = config;
    }

    public async Task<ForgotPasswordResult> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Buscar usuario por email o username
        var usuario = await _unitOfWork.Usuarios.GetByEmailOrUsernameAsync(request.EmailOrUsername);

        // Respuesta genérica por seguridad
        if (usuario is null || !usuario.Estado)
            return new ForgotPasswordResult(true, "Si los datos son correctos, recibirás un correo en breve.");

        // 2. Generar token seguro SHA-256
        var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var token = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawToken)
            )
        ).ToLower();

        var expires = DateTime.UtcNow.AddMinutes(30);

        // 3. Guardar token en BD
        await _unitOfWork.Usuarios.SaveResetTokenAsync(usuario.UsuarioID, token, expires);

        // 4. Construir enlace
        var frontendUrl = _config["App:FrontendUrl"] ?? "http://localhost:3000";
        var resetLink = $"{frontendUrl}/reset-password?token={token}";

        // 5. Enviar correo
        await _emailService.SendPasswordResetEmailAsync(
            usuario.Email,
            usuario.NombreCompleto,
            resetLink
        );

        return new ForgotPasswordResult(true, "Si los datos son correctos, recibirás un correo en breve.");
    }
}