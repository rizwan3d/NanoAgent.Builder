namespace NanoAgent.Builder.Application.Admin;

public sealed record AdminUserRowDto(
    string Id,
    string Email,
    string? DisplayName,
    IReadOnlyCollection<string> Roles,
    string PlanName,
    string SubscriptionStatus,
    int ProjectCount,
    int UsedTokensThisPeriod,
    int MonthlyTokenLimit,
    IReadOnlyList<string> AllowedLlmModels,
    DateTimeOffset CreatedAtUtc)
{
    public string MonthlyTokenLimitDisplay => MonthlyTokenLimit == -1 ? "Unlimited" : MonthlyTokenLimit.ToString("N0");

    public string UsedTokensDisplay => UsedTokensThisPeriod.ToString("N0");
}
