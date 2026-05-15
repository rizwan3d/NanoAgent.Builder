using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Pages;

public sealed class PlansModel : PageModel
{
    private readonly IBillingCheckoutService _billingCheckoutService;
    private readonly ISaasSubscriptionService _subscriptionService;

    public PlansModel(
        ISaasSubscriptionService subscriptionService,
        IBillingCheckoutService billingCheckoutService)
    {
        _subscriptionService = subscriptionService;
        _billingCheckoutService = billingCheckoutService;
    }

    public IReadOnlyList<SaasPlanDto> Plans { get; private set; } = [];

    public UserSubscriptionDto? CurrentSubscription { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(string planCode, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Challenge();
        }

        try
        {
            var successUrl = BuildAbsolutePageUri("/Billing/Success");
            var cancelUrl = BuildAbsolutePageUri("/Plans");

            var result = await _billingCheckoutService.SelectPackageForCurrentUserAsync(
                planCode,
                successUrl,
                cancelUrl,
                cancellationToken);

            if (result.RequiresRedirect && !string.IsNullOrWhiteSpace(result.RedirectUrl))
            {
                return Redirect(result.RedirectUrl);
            }

            TempData["StatusMessage"] = result.Message;
            return RedirectToPage();
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
        Plans = await _subscriptionService.ListPlansAsync(cancellationToken);
        CurrentSubscription = await _subscriptionService.GetCurrentSubscriptionAsync(cancellationToken);
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
