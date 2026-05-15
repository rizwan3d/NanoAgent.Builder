using NanoAgent.Builder.Domain.Saas;

namespace NanoAgent.Builder.Application.Saas;

public sealed record SaasPlanDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    SubscriptionTier Tier,
    decimal MonthlyPrice,
    string Currency,
    int ProjectLimit,
    int MonthlyTokenLimit,
    IReadOnlyList<string> AllowedLlmModels,
    bool IsActive,
    int DisplayOrder,
    string? StripePriceId)
{
    public bool RequiresPayment => Tier == SubscriptionTier.Paid;

    public bool IsPaymentConfigured => !RequiresPayment || !string.IsNullOrWhiteSpace(StripePriceId);

    public string ProjectLimitDisplay => ProjectLimit == -1 ? "Unlimited" : ProjectLimit.ToString("N0");

    public string MonthlyTokenLimitDisplay => MonthlyTokenLimit == -1 ? "Unlimited" : MonthlyTokenLimit.ToString("N0");
}
