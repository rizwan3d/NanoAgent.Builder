using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Domain.Saas;

public sealed class UserSubscription : Entity
{
    private UserSubscription()
    {
    }

    public UserSubscription(string userId, Guid subscriptionPlanId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("User id is required for a subscription.");
        }

        UserId = userId;
        SubscriptionPlanId = subscriptionPlanId;
        Status = SubscriptionStatus.Active;
        StartedAtUtc = DateTimeOffset.UtcNow;
    }

    public string UserId { get; private set; } = string.Empty;

    public Guid SubscriptionPlanId { get; private set; }

    public SubscriptionPlan? Plan { get; private set; }

    public SubscriptionStatus Status { get; private set; }

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset? EndsAtUtc { get; private set; }

    public string? StripeCustomerId { get; private set; }

    public string? StripeSubscriptionId { get; private set; }

    public string? StripePriceId { get; private set; }

    public DateTimeOffset? CurrentPeriodEndsAtUtc { get; private set; }

    public bool IsCurrent => Status == SubscriptionStatus.Active && EndsAtUtc is null;

    public void ChangePlan(Guid subscriptionPlanId)
    {
        SubscriptionPlanId = subscriptionPlanId;
        Status = SubscriptionStatus.Active;
        EndsAtUtc = null;
    }

    public void AttachStripeSubscription(
        string? stripeCustomerId,
        string? stripeSubscriptionId,
        string? stripePriceId,
        DateTimeOffset? currentPeriodEndsAtUtc)
    {
        StripeCustomerId = CleanOptionalStripeId(stripeCustomerId, nameof(stripeCustomerId));
        StripeSubscriptionId = CleanOptionalStripeId(stripeSubscriptionId, nameof(stripeSubscriptionId));
        StripePriceId = CleanOptionalStripeId(stripePriceId, nameof(stripePriceId));
        CurrentPeriodEndsAtUtc = currentPeriodEndsAtUtc;
    }

    public void MarkActive(DateTimeOffset? currentPeriodEndsAtUtc = null)
    {
        Status = SubscriptionStatus.Active;
        EndsAtUtc = null;
        CurrentPeriodEndsAtUtc = currentPeriodEndsAtUtc ?? CurrentPeriodEndsAtUtc;
    }

    public void MarkIncomplete(DateTimeOffset? currentPeriodEndsAtUtc = null)
    {
        Status = SubscriptionStatus.Incomplete;
        EndsAtUtc = null;
        CurrentPeriodEndsAtUtc = currentPeriodEndsAtUtc ?? CurrentPeriodEndsAtUtc;
    }

    public void MarkPastDue(DateTimeOffset? currentPeriodEndsAtUtc = null)
    {
        Status = SubscriptionStatus.PastDue;
        EndsAtUtc = null;
        CurrentPeriodEndsAtUtc = currentPeriodEndsAtUtc ?? CurrentPeriodEndsAtUtc;
    }

    public void Cancel(DateTimeOffset? endsAtUtc = null)
    {
        Status = SubscriptionStatus.Canceled;
        EndsAtUtc = endsAtUtc ?? DateTimeOffset.UtcNow;
    }

    private static string? CleanOptionalStripeId(string? value, string parameterName)
    {
        if (value is { Length: > 200 })
        {
            throw new DomainException($"{parameterName} cannot be longer than 200 characters.");
        }

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
