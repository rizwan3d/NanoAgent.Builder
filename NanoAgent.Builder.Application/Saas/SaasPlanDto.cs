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
    bool IsActive,
    int DisplayOrder);
