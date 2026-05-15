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

    public bool IsCurrent => Status == SubscriptionStatus.Active && EndsAtUtc is null;

    public void ChangePlan(Guid subscriptionPlanId)
    {
        SubscriptionPlanId = subscriptionPlanId;
        Status = SubscriptionStatus.Active;
        EndsAtUtc = null;
    }

    public void MarkPastDue() => Status = SubscriptionStatus.PastDue;

    public void Cancel(DateTimeOffset? endsAtUtc = null)
    {
        Status = SubscriptionStatus.Canceled;
        EndsAtUtc = endsAtUtc ?? DateTimeOffset.UtcNow;
    }
}
