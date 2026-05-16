using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Projects;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Pages;

public class IndexModel : PageModel
{
    private readonly IAgentProjectService _projectService;
    private readonly IDatabaseInfoProvider _databaseInfoProvider;
    private readonly ISaasSubscriptionService _subscriptionService;
    private readonly ITokenUsageService _tokenUsageService;

    public IndexModel(
        IAgentProjectService projectService,
        IDatabaseInfoProvider databaseInfoProvider,
        ISaasSubscriptionService subscriptionService,
        ITokenUsageService tokenUsageService)
    {
        _projectService = projectService;
        _databaseInfoProvider = databaseInfoProvider;
        _subscriptionService = subscriptionService;
        _tokenUsageService = tokenUsageService;
    }

    public CreateProjectInput CreateInput { get; set; } = new();

    public IReadOnlyList<AgentProjectDto> Projects { get; private set; } = [];

    public DatabaseInfo DatabaseInfo { get; private set; } = new("Unknown", "Unknown");

    public UserSubscriptionDto? CurrentSubscription { get; private set; }

    public TokenUsageDto? CurrentUsage { get; private set; }

    public SaasPlanDto? CurrentPlan { get; private set; }

    public int ProjectLimitUsagePercent => CurrentPlan is null || CurrentPlan.ProjectLimit == -1
        ? 0
        : Math.Min(100, (int)Math.Round(Projects.Count * 100d / CurrentPlan.ProjectLimit));

    public int TokenUsagePercent => CurrentUsage is null || CurrentUsage.MonthlyTokenLimit == -1
        ? 0
        : Math.Min(100, (int)Math.Round(CurrentUsage.UsedTokens * 100d / CurrentUsage.MonthlyTokenLimit));

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadPageDataAsync(cancellationToken);
        PrimeCreateInput();
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        CreateInput = new CreateProjectInput();
        await TryUpdateModelAsync(CreateInput, nameof(CreateInput));

        await LoadPageDataAsync(cancellationToken);
        CreateInput.LlmModel = string.IsNullOrWhiteSpace(CreateInput.LlmModel)
            ? CurrentUsage?.AllowedLlmModels.FirstOrDefault() ?? string.Empty
            : CreateInput.LlmModel;

        if (!TryValidateModel(CreateInput, nameof(CreateInput)))
        {
            return Page();
        }

        try
        {
            var project = await _projectService.CreateAsync(
                new CreateAgentProjectRequest(CreateInput.Name, CreateInput.Description, CreateInput.LlmModel),
                cancellationToken);

            return RedirectToPage("/Workspace", new { projectId = project.Id });
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRenameAsync(Guid projectId, string name, CancellationToken cancellationToken)
    {
        try
        {
            await _projectService.RenameAsync(new RenameAgentProjectRequest(projectId, name), cancellationToken);
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadPageDataAsync(cancellationToken);
            PrimeCreateInput();
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            await _projectService.DeleteAsync(projectId, cancellationToken);
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadPageDataAsync(cancellationToken);
            PrimeCreateInput();
            return Page();
        }

        return RedirectToPage();
    }

    private async Task LoadPageDataAsync(CancellationToken cancellationToken)
    {
        DatabaseInfo = _databaseInfoProvider.GetCurrent();
        CurrentSubscription = await _subscriptionService.GetCurrentSubscriptionAsync(cancellationToken);
        CurrentUsage = await _tokenUsageService.GetCurrentUsageForCurrentUserAsync(cancellationToken);
        Projects = await _projectService.ListAsync(cancellationToken);

        var plans = await _subscriptionService.ListPlansAsync(cancellationToken);
        CurrentPlan = CurrentSubscription is null
            ? plans.FirstOrDefault(plan => string.Equals(plan.Code, SaasPlanCodes.Free, StringComparison.OrdinalIgnoreCase))
            : plans.FirstOrDefault(plan => string.Equals(plan.Code, CurrentSubscription.PlanCode, StringComparison.OrdinalIgnoreCase));
    }

    private void PrimeCreateInput()
    {
        CreateInput.LlmModel = CurrentUsage?.AllowedLlmModels.FirstOrDefault() ?? string.Empty;
    }

    public sealed class CreateProjectInput
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Project name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Project description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "LLM model")]
        [StringLength(100)]
        public string LlmModel { get; set; } = string.Empty;
    }
}
