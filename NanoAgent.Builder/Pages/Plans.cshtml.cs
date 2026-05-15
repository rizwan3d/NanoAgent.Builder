using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Pages;

public sealed class PlansModel : PageModel
{
    private readonly ISaasSubscriptionService _subscriptionService;

    public PlansModel(ISaasSubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
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
            await _subscriptionService.SubscribeCurrentUserAsync(planCode, cancellationToken);
            TempData["StatusMessage"] = "Your SaaS package has been updated.";
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
}
