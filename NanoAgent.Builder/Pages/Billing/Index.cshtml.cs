using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Pages.Billing;

public sealed class IndexModel : PageModel
{
    private readonly IBillingCheckoutService _billingCheckoutService;
    private readonly ISaasSubscriptionService _subscriptionService;
    private readonly ITokenUsageService _tokenUsageService;

    public IndexModel(
        ISaasSubscriptionService subscriptionService,
        IBillingCheckoutService billingCheckoutService,
        ITokenUsageService tokenUsageService)
    {
        _subscriptionService = subscriptionService;
        _billingCheckoutService = billingCheckoutService;
        _tokenUsageService = tokenUsageService;
    }

    public UserSubscriptionDto? CurrentSubscription { get; private set; }

    public TokenUsageDto? CurrentUsage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostPortalAsync(CancellationToken cancellationToken)
    {
        try
        {
            var returnUrl = BuildAbsolutePageUri("/Billing/Index");
            var portal = await _billingCheckoutService.CreatePortalForCurrentUserAsync(returnUrl, cancellationToken);
            return Redirect(portal.RedirectUrl);
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadAsync(cancellationToken);
            return Page();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        CurrentSubscription = await _subscriptionService.GetCurrentSubscriptionAsync(cancellationToken);
        CurrentUsage = await _tokenUsageService.GetCurrentUsageForCurrentUserAsync(cancellationToken);
    }

    private Uri BuildAbsolutePageUri(string page)
    {
        var url = Url.PageLink(page);
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new DomainException($"Could not build URL for page {page}.");
        }

        return new Uri(url);
    }
}
