using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Common;
using NanoAgent.Builder.Domain.Saas;

namespace NanoAgent.Builder.Application.Saas;

internal sealed class BillingCheckoutService : IBillingCheckoutService
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ISaasPlanRepository _plans;
    private readonly ISaasSubscriptionService _subscriptionService;

    public BillingCheckoutService(
        ICurrentUserContext currentUser,
        IPaymentGateway paymentGateway,
        ISaasPlanRepository plans,
        ISaasSubscriptionService subscriptionService)
    {
        _currentUser = currentUser;
        _paymentGateway = paymentGateway;
        _plans = plans;
        _subscriptionService = subscriptionService;
    }

    public async Task<BillingPlanSelectionResult> SelectPackageForCurrentUserAsync(
        string planCode,
        Uri successUrl,
        Uri cancelUrl,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new DomainException("You must sign in before selecting a SaaS package.");
        }

        var plan = await _plans.GetByCodeAsync(planCode, cancellationToken);
        if (plan is null || !plan.IsActive)
        {
            throw new DomainException("The selected SaaS package is not available.");
        }

        if (plan.Tier == SubscriptionTier.Free)
        {
            await _subscriptionService.SubscribeCurrentUserAsync(plan.Code, cancellationToken);
            return new BillingPlanSelectionResult(false, null, "Your SaaS package has been updated.");
        }

        if (string.IsNullOrWhiteSpace(plan.StripePriceId))
        {
            throw new DomainException($"Stripe is not configured for the {plan.Name} package yet.");
        }

        var checkoutSession = await _paymentGateway.CreateSubscriptionCheckoutSessionAsync(
            new PaymentCheckoutRequest(
                _currentUser.UserId,
                _currentUser.Email,
                plan.Code,
                plan.Name,
                plan.StripePriceId,
                successUrl,
                cancelUrl),
            cancellationToken);

        return new BillingPlanSelectionResult(true, checkoutSession.RedirectUrl, "Redirecting to Stripe Checkout.");
    }

    public async Task<BillingPortalResult> CreatePortalForCurrentUserAsync(
        Uri returnUrl,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new DomainException("You must sign in before managing billing.");
        }

        var portalSession = await _paymentGateway.CreateCustomerPortalSessionAsync(
            _currentUser.UserId,
            returnUrl,
            cancellationToken);

        return new BillingPortalResult(portalSession.RedirectUrl);
    }
}
