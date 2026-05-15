using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NanoAgent.Builder.Application.Projects;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Application.Workspace;
using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Pages;

public class WorkspaceModel : PageModel
{
    private readonly IAgentProjectService _projectService;
    private readonly ISaasSubscriptionService _subscriptionService;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IProjectWorkspaceService _workspaceService;

    public WorkspaceModel(
        IAgentProjectService projectService,
        ISaasSubscriptionService subscriptionService,
        ITokenUsageService tokenUsageService,
        IProjectWorkspaceService workspaceService)
    {
        _projectService = projectService;
        _subscriptionService = subscriptionService;
        _tokenUsageService = tokenUsageService;
        _workspaceService = workspaceService;
    }

    public IReadOnlyList<AgentProjectDto> Projects { get; private set; } = [];

    public AgentProjectDto? SelectedProject { get; private set; }

    public ProjectWorkspaceDto? Workspace { get; private set; }

    public UserSubscriptionDto? CurrentSubscription { get; private set; }

    public TokenUsageDto? CurrentUsage { get; private set; }

    [BindProperty]
    public ChatInput SendInput { get; set; } = new();

    public string SelectedProjectName => SelectedProject?.Name ?? "Untitled project";

    public async Task OnGetAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        await LoadPageDataAsync(projectId, cancellationToken);
    }

    public async Task<IActionResult> OnPostSendAsync(Guid projectId, CancellationToken cancellationToken)
    {
        SendInput = new ChatInput();
        await TryUpdateModelAsync(SendInput, nameof(SendInput));

        if (!TryValidateModel(SendInput, nameof(SendInput)))
        {
            await LoadPageDataAsync(projectId, cancellationToken);
            return Page();
        }

        try
        {
            Workspace = await _workspaceService.SubmitMessageAsync(
                new SubmitProjectMessageRequest(projectId, SendInput.Message, SendInput.LlmModel),
                cancellationToken);
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadPageDataAsync(projectId, cancellationToken);
            return Page();
        }

        return RedirectToPage(new { projectId });
    }

    private async Task LoadPageDataAsync(Guid? projectId, CancellationToken cancellationToken)
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

        if (SelectedProject is not null)
        {
            try
            {
                Workspace = await _workspaceService.GetWorkspaceAsync(SelectedProject.Id, cancellationToken);
                CurrentUsage = Workspace.TokenUsage;
            }
            catch (DomainException exception)
            {
                ModelState.AddModelError(string.Empty, exception.Message);
            }
        }

        PrimeSendInput();
    }

    private void PrimeSendInput()
    {
        var allowedModels = Workspace?.AllowedModels ?? CurrentUsage?.AllowedLlmModels ?? Array.Empty<string>();
        var selectedProjectModel = SelectedProject?.LlmModel;

        SendInput.LlmModel = allowedModels.Contains(selectedProjectModel, StringComparer.OrdinalIgnoreCase)
            ? selectedProjectModel!
            : allowedModels.FirstOrDefault() ?? string.Empty;
    }

    public sealed class ChatInput
    {
        [Required]
        [StringLength(8000)]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Model")]
        public string LlmModel { get; set; } = string.Empty;
    }
}
