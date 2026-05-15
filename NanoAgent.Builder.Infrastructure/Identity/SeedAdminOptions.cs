namespace NanoAgent.Builder.Infrastructure.Identity;

public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public string Email { get; init; } = "admin@nanoagent.local";

    public string Password { get; init; } = "Admin#12345";

    public string DisplayName { get; init; } = "System Admin";
}
