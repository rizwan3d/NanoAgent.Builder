using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Pages.Billing;

public sealed class IndexModel : PageModel
{
    private readonly IBillingCheckoutService _billingCheckoutService;
    private readonly ISaasSubscriptionService _subscriptionService;

    public IndexModel(
        ISaasSubscriptionService subscriptionService,
        IBillingCheckoutService billingCheckoutService)
    {
        _subscriptionService = subscriptionService;
        _billingCheckoutService = billingCheckoutService;
    }

    public UserSubscriptionDto? CurrentSubscription { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        CurrentSubscription = await _subscriptionService.GetCurrentSubscriptionAsync(cancellationToken);
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
            CurrentSubscription = await _subscriptionService.GetCurrentSubscriptionAsync(cancellationToken);
            return Page();
        }
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
