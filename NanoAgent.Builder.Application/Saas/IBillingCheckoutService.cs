namespace NanoAgent.Builder.Application.Saas;

public interface IBillingCheckoutService
{
    Task<BillingPlanSelectionResult> SelectPackageForCurrentUserAsync(
        string planCode,
        Uri successUrl,
        Uri cancelUrl,
        CancellationToken cancellationToken = default);

    Task<BillingPortalResult> CreatePortalForCurrentUserAsync(
        Uri returnUrl,
        CancellationToken cancellationToken = default);
}

public sealed record BillingPlanSelectionResult(
    bool RequiresRedirect,
    string? RedirectUrl,
    string Message);

public sealed record BillingPortalResult(string RedirectUrl);
