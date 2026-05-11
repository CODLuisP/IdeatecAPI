using Microsoft.AspNetCore.Http;
using IdeatecAPI.Application.Common.Interfaces;

namespace IdeatecAPI.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Environment => _httpContextAccessor.HttpContext?.User?.FindFirst("environment")?.Value;
}
