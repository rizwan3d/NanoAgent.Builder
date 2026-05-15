namespace NanoAgent.Builder.Application.Admin;

public sealed record AdminUserRowDto(
    string Id,
    string Email,
    string? DisplayName,
    IReadOnlyCollection<string> Roles,
    string PlanName,
    string SubscriptionStatus,
    int ProjectCount,
    DateTimeOffset CreatedAtUtc);
