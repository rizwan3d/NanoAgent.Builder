using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NanoAgent.Builder.Application.Admin;
using NanoAgent.Builder.Application.Security;

namespace NanoAgent.Builder.Pages.Admin;

[Authorize(Roles = ApplicationRoles.Admin)]
public sealed class IndexModel : PageModel
{
    private readonly IAdminDashboardService _dashboardService;

    public IndexModel(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public AdminDashboardDto Dashboard { get; private set; } = new(0, 0, 0, [], []);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await _dashboardService.GetDashboardAsync(cancellationToken);
    }
}
