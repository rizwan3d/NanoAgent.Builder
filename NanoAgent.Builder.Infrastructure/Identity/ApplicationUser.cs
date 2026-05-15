using Microsoft.AspNetCore.Identity;

namespace NanoAgent.Builder.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
