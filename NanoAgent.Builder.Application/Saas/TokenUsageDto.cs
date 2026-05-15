namespace NanoAgent.Builder.Application.Saas;

public sealed record TokenUsageDto(
    string UserId,
    string PlanCode,
    string PlanName,
    int MonthlyTokenLimit,
    int UsedTokens,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    IReadOnlyList<string> AllowedLlmModels)
{
    public bool HasUnlimitedTokens => MonthlyTokenLimit == -1;

    public int? RemainingTokens => HasUnlimitedTokens ? null : Math.Max(0, MonthlyTokenLimit - UsedTokens);

    public string MonthlyTokenLimitDisplay => HasUnlimitedTokens ? "Unlimited" : MonthlyTokenLimit.ToString("N0");

    public string RemainingTokensDisplay => HasUnlimitedTokens ? "Unlimited" : RemainingTokens.GetValueOrDefault().ToString("N0");
}
