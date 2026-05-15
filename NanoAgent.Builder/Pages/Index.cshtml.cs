using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Projects;
using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Pages;

public class IndexModel : PageModel
{
    private readonly IAgentProjectService _projectService;
    private readonly IDatabaseInfoProvider _databaseInfoProvider;

    public IndexModel(IAgentProjectService projectService, IDatabaseInfoProvider databaseInfoProvider)
    {
        _projectService = projectService;
        _databaseInfoProvider = databaseInfoProvider;
    }

    [BindProperty]
    public CreateProjectInput Input { get; set; } = new();

    public IReadOnlyList<AgentProjectDto> Projects { get; private set; } = [];

    public DatabaseInfo DatabaseInfo { get; private set; } = new("Unknown", "Unknown");

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadPageDataAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync(cancellationToken);
            return Page();
        }

        try
        {
            await _projectService.CreateAsync(
                new CreateAgentProjectRequest(Input.Name, Input.Description),
                cancellationToken);
        }
        catch (DomainException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            await LoadPageDataAsync(cancellationToken);
            return Page();
        }

        return RedirectToPage();
    }

    private async Task LoadPageDataAsync(CancellationToken cancellationToken)
    {
        DatabaseInfo = _databaseInfoProvider.GetCurrent();
        Projects = await _projectService.ListAsync(cancellationToken);
    }

    public sealed class CreateProjectInput
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}
