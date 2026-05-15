using Microsoft.AspNetCore.Mvc.RazorPages;
using NanoAgent.Builder.Application.Projects;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Pages;

public class WorkspaceModel : PageModel
{
    private readonly IAgentProjectService _projectService;
    private readonly ISaasSubscriptionService _subscriptionService;
    private readonly ITokenUsageService _tokenUsageService;

    public WorkspaceModel(
        IAgentProjectService projectService,
        ISaasSubscriptionService subscriptionService,
        ITokenUsageService tokenUsageService)
    {
        _projectService = projectService;
        _subscriptionService = subscriptionService;
        _tokenUsageService = tokenUsageService;
    }

    public IReadOnlyList<AgentProjectDto> Projects { get; private set; } = [];

    public AgentProjectDto? SelectedProject { get; private set; }

    public UserSubscriptionDto? CurrentSubscription { get; private set; }

    public TokenUsageDto? CurrentUsage { get; private set; }

    public string SelectedProjectName => SelectedProject?.Name ?? "Untitled project";

    public async Task OnGetAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        CurrentSubscription = await _subscriptionService.GetCurrentSubscriptionAsync(cancellationToken);
        CurrentUsage = await _tokenUsageService.GetCurrentUsageForCurrentUserAsync(cancellationToken);
        Projects = await _projectService.ListAsync(cancellationToken);

        SelectedProject = projectId.HasValue
            ? Projects.FirstOrDefault(project => project.Id == projectId.Value)
            : Projects.FirstOrDefault();

        if (projectId.HasValue && SelectedProject is null)
        {
            ModelState.AddModelError(string.Empty, "The selected project was not found or is not available to your account.");
        }
    }
}
