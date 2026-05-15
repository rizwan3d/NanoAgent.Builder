namespace NanoAgent.Builder.Application.Saas;

public interface ISubscriptionProvisioningService
{
    Task ActivatePaidSubscriptionAsync(
        PaidSubscriptionProvisioningRequest request,
        CancellationToken cancellationToken = default);

    Task MarkPaidSubscriptionPastDueAsync(
        StripeSubscriptionStateChangeRequest request,
        CancellationToken cancellationToken = default);

    Task MarkPaidSubscriptionIncompleteAsync(
        StripeSubscriptionStateChangeRequest request,
        CancellationToken cancellationToken = default);

    Task CancelPaidSubscriptionAsync(
        StripeSubscriptionStateChangeRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record PaidSubscriptionProvisioningRequest(
    string? UserId,
    string? PlanCode,
    string? StripeCustomerId,
    string? StripeSubscriptionId,
    string? StripePriceId,
    DateTimeOffset? CurrentPeriodStartsAtUtc,
    DateTimeOffset? CurrentPeriodEndsAtUtc);

public sealed record StripeSubscriptionStateChangeRequest(
    string? UserId,
    string? PlanCode,
    string? StripeSubscriptionId,
    string? StripePriceId,
    DateTimeOffset? CurrentPeriodStartsAtUtc,
    DateTimeOffset? CurrentPeriodEndsAtUtc);
