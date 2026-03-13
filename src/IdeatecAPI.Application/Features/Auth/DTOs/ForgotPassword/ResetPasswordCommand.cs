using MediatR;
 
namespace IdeatecAPI.Application.Features.Auth.ResetPassword;
 
public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<ResetPasswordResult>;
 
public record ResetPasswordResult(bool Success, string Message);