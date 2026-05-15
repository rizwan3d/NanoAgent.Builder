using Microsoft.AspNetCore.Mvc.RazorPages;
using NanoAgent.Builder.Application.Saas;

namespace NanoAgent.Builder.Pages.Billing;

public sealed class SuccessModel : PageModel
{
    private readonly ISaasSubscriptionService _subscriptionService;

    public SuccessModel(ISaasSubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    public UserSubscriptionDto? CurrentSubscription { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        CurrentSubscription = await _subscriptionService.GetCurrentSubscriptionAsync(cancellationToken);
    }
}
