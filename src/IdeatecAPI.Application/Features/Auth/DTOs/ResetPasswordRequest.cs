namespace IdeatecAPI.Application.Features.Auth.DTOs;

public record ResetPasswordRequest(string Token, string NewPassword);