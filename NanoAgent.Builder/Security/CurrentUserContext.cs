using System.Security.Claims;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Security;

namespace NanoAgent.Builder.Security;

internal sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool IsAdmin => _httpContextAccessor.HttpContext?.User.IsInRole(ApplicationRoles.Admin) == true;
}
