using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Common;
using NanoAgent.Builder.Domain.Saas;

namespace NanoAgent.Builder.Application.Saas;

internal sealed class SubscriptionProvisioningService : ISubscriptionProvisioningService
{
    private readonly ISaasPlanRepository _plans;
    private readonly IUserSubscriptionRepository _subscriptions;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionProvisioningService(
        ISaasPlanRepository plans,
        IUserSubscriptionRepository subscriptions,
        IUnitOfWork unitOfWork)
    {
        _plans = plans;
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
    }

    public async Task ActivatePaidSubscriptionAsync(
        PaidSubscriptionProvisioningRequest request,
        CancellationToken cancellationToken = default)
    {
        var plan = await ResolvePaidPlanAsync(request.PlanCode, request.StripePriceId, cancellationToken);
        var subscription = await ResolveSubscriptionAsync(request.UserId, request.StripeSubscriptionId, cancellationToken);

        if (subscription is null)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                throw new DomainException("Stripe event did not include a user id and no existing subscription matched it.");
            }

            subscription = new UserSubscription(request.UserId, plan.Id);
            await _subscriptions.AddAsync(subscription, cancellationToken);
        }
        else
        {
            subscription.ChangePlan(plan.Id);
        }

        subscription.AttachStripeSubscription(
            request.StripeCustomerId,
            request.StripeSubscriptionId,
            request.StripePriceId,
            request.CurrentPeriodEndsAtUtc);
        subscription.MarkActive(request.CurrentPeriodEndsAtUtc);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkPaidSubscriptionPastDueAsync(
        StripeSubscriptionStateChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var subscription = await ResolveSubscriptionAsync(request.UserId, request.StripeSubscriptionId, cancellationToken);
        if (subscription is null)
        {
            return;
        }

        subscription.MarkPastDue(request.CurrentPeriodEndsAtUtc);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkPaidSubscriptionIncompleteAsync(
        StripeSubscriptionStateChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var subscription = await ResolveSubscriptionAsync(request.UserId, request.StripeSubscriptionId, cancellationToken);
        if (subscription is null)
        {
            return;
        }

        subscription.MarkIncomplete(request.CurrentPeriodEndsAtUtc);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelPaidSubscriptionAsync(
        StripeSubscriptionStateChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var subscription = await ResolveSubscriptionAsync(request.UserId, request.StripeSubscriptionId, cancellationToken);
        if (subscription is null)
        {
            return;
        }

        subscription.Cancel(request.CurrentPeriodEndsAtUtc);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<SubscriptionPlan> ResolvePaidPlanAsync(
        string? planCode,
        string? stripePriceId,
        CancellationToken cancellationToken)
    {
        SubscriptionPlan? plan = null;

        if (!string.IsNullOrWhiteSpace(planCode))
        {
            plan = await _plans.GetByCodeAsync(planCode, cancellationToken);
        }

        if (plan is null && !string.IsNullOrWhiteSpace(stripePriceId))
        {
            plan = await _plans.GetByStripePriceIdAsync(stripePriceId, cancellationToken);
        }

        if (plan is null || !plan.IsActive || plan.Tier != SubscriptionTier.Paid)
        {
            throw new DomainException("The Stripe event did not match an active paid SaaS package.");
        }

        return plan;
    }

    private async Task<UserSubscription?> ResolveSubscriptionAsync(
        string? userId,
        string? stripeSubscriptionId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(stripeSubscriptionId))
        {
            var byStripeSubscription = await _subscriptions.GetByStripeSubscriptionIdAsync(stripeSubscriptionId, cancellationToken);
            if (byStripeSubscription is not null)
            {
                return byStripeSubscription;
            }
        }

        return string.IsNullOrWhiteSpace(userId)
            ? null
            : await _subscriptions.GetCurrentForUserAsync(userId, cancellationToken);
    }
}
