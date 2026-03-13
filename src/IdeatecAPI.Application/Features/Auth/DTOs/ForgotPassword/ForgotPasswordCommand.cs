using MediatR;
 
namespace IdeatecAPI.Application.Features.Auth.ForgotPassword;
 
public record ForgotPasswordCommand(string EmailOrUsername) : IRequest<ForgotPasswordResult>;
 
public record ForgotPasswordResult(bool Success, string Message);