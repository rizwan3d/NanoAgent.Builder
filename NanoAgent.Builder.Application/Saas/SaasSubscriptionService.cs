using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Common;
using NanoAgent.Builder.Domain.Saas;

namespace NanoAgent.Builder.Application.Saas;

internal sealed class SaasSubscriptionService : ISaasSubscriptionService
{
    private readonly ICurrentUserContext _currentUser;
    private readonly ISaasPlanRepository _plans;
    private readonly IUserSubscriptionRepository _subscriptions;
    private readonly IUnitOfWork _unitOfWork;

    public SaasSubscriptionService(
        ICurrentUserContext currentUser,
        ISaasPlanRepository plans,
        IUserSubscriptionRepository subscriptions,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _plans = plans;
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SaasPlanDto>> ListPlansAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _plans.ListActiveAsync(cancellationToken);
        return plans.Select(MapPlan).ToList();
    }

    public async Task<UserSubscriptionDto?> GetCurrentSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            return null;
        }

        var subscription = await _subscriptions.GetCurrentForUserAsync(_currentUser.UserId, cancellationToken);
        return subscription is null ? null : MapSubscription(subscription);
    }

    public Task<UserSubscriptionDto> SubscribeCurrentUserAsync(string planCode, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new DomainException("You must sign in before selecting a SaaS package.");
        }

        return SubscribeUserAsync(_currentUser.UserId, planCode, cancellationToken);
    }

    public async Task<UserSubscriptionDto> SubscribeUserAsync(string userId, string planCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("User id is required for subscription changes.");
        }

        var plan = await _plans.GetByCodeAsync(planCode, cancellationToken);
        if (plan is null || !plan.IsActive)
        {
            throw new DomainException("The selected SaaS package is not available.");
        }

        if (plan.Tier == SubscriptionTier.Paid)
        {
            throw new DomainException("Paid packages must be selected through Stripe Checkout.");
        }

        var subscription = await _subscriptions.GetCurrentForUserAsync(userId, cancellationToken);
        if (subscription is null)
        {
            subscription = new UserSubscription(userId, plan.Id);
            await _subscriptions.AddAsync(subscription, cancellationToken);
        }
        else
        {
            subscription.ChangePlan(plan.Id);
            subscription.AttachStripeSubscription(null, null, null, null, null);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        subscription = await _subscriptions.GetCurrentForUserAsync(userId, cancellationToken) ?? subscription;
        return MapSubscription(subscription);
    }

    internal static SaasPlanDto MapPlan(SubscriptionPlan plan) =>
        new(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.Description,
            plan.Tier,
            plan.MonthlyPrice,
            plan.Currency,
            plan.ProjectLimit,
            plan.MonthlyTokenLimit,
            plan.GetAllowedLlmModels(),
            plan.IsActive,
            plan.DisplayOrder,
            plan.StripePriceId);

    internal static UserSubscriptionDto MapSubscription(UserSubscription subscription)
    {
        var plan = subscription.Plan ?? throw new DomainException("The subscription package could not be loaded.");

        return new UserSubscriptionDto(
            subscription.Id,
            subscription.UserId,
            plan.Id,
            plan.Code,
            plan.Name,
            subscription.Status,
            subscription.StartedAtUtc,
            subscription.EndsAtUtc,
            subscription.StripeCustomerId,
            subscription.StripeSubscriptionId,
            subscription.CurrentPeriodStartsAtUtc,
            subscription.CurrentPeriodEndsAtUtc);
    }
}
