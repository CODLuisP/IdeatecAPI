namespace IdeatecAPI.Application.Common.Interfaces.Persistence;
 
public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetLink);
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, byte[]? adjunto = null, string? nombreAdjunto = null);
}
 