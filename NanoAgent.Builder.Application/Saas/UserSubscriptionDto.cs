using NanoAgent.Builder.Domain.Saas;

namespace NanoAgent.Builder.Application.Saas;

public sealed record UserSubscriptionDto(
    Guid Id,
    string UserId,
    Guid PlanId,
    string PlanCode,
    string PlanName,
    SubscriptionStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? EndsAtUtc,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
    DateTimeOffset? CurrentPeriodStartsAtUtc,
    DateTimeOffset? CurrentPeriodEndsAtUtc);
