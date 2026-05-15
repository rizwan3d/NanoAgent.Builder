using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;
using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Application.Projects;

internal sealed class AgentProjectService : IAgentProjectService
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IAgentProjectRepository _projects;
    private readonly IProjectQuotaService _quotaService;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IProjectStorageRepository _projectStorage;
    private readonly IUnitOfWork _unitOfWork;

    public AgentProjectService(
        ICurrentUserContext currentUser,
        IAgentProjectRepository projects,
        IProjectQuotaService quotaService,
        ITokenUsageService tokenUsageService,
        IProjectStorageRepository projectStorage,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _projects = projects;
        _quotaService = quotaService;
        _tokenUsageService = tokenUsageService;
        _projectStorage = projectStorage;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AgentProjectDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        RequireSignedInUser();

        var projects = _currentUser.IsAdmin
            ? await _projects.ListAllAsync(cancellationToken)
            : await _projects.ListForOwnerAsync(_currentUser.UserId!, cancellationToken);

        return projects.Select(MapToDto).ToList();
    }

    public async Task<AgentProjectDto> CreateAsync(CreateAgentProjectRequest request, CancellationToken cancellationToken = default)
    {
        var userId = RequireSignedInUser();

        await _quotaService.EnsureCanCreateProjectAsync(userId, cancellationToken);
        await _tokenUsageService.EnsureModelAllowedAsync(userId, request.LlmModel, cancellationToken);

        var project = new AgentProject(userId, request.Name, request.Description, request.LlmModel);

        await _projects.AddAsync(project, cancellationToken);
        await SeedProjectStorageAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(project);
    }

    public async Task<AgentProjectDto> RenameAsync(RenameAgentProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = await GetOwnedOrAdminProjectAsync(request.ProjectId, cancellationToken);

        project.Rename(request.Name);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(project);
    }

    public async Task DeleteAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await GetOwnedOrAdminProjectAsync(projectId, cancellationToken);

        _projects.Remove(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }


    private async Task SeedProjectStorageAsync(AgentProject project, CancellationToken cancellationToken)
    {
        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "README.md",
                "markdown",
                $"""
                # {project.Name}

                This project is ready for NanoAgent workspace storage.

                - Chat messages are stored in `ProjectMessages`.
                - Files are stored in `ProjectFiles`.
                - Build/generation runs are stored in `ProjectRuns`.
                - Generated outputs are stored in `GeneratedArtifacts`.
                """),
            cancellationToken);

        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "src/App.tsx",
                "typescript",
                """
                export default function App() {
                  return (
                    <main className="app-shell">
                      <h1>NanoAgent generated app</h1>
                      <p>Use the chat panel to request changes. This editor is backed by project file storage.</p>
                    </main>
                  );
                }
                """),
            cancellationToken);

        await _projectStorage.AddFileAsync(
            new ProjectFile(
                project.Id,
                "agent.config.json",
                "json",
                $$"""
                {
                  "projectId": "{{project.Id}}",
                  "llmModel": "{{project.LlmModel}}",
                  "storage": {
                    "files": "ProjectFiles",
                    "messages": "ProjectMessages",
                    "runs": "ProjectRuns",
                    "artifacts": "GeneratedArtifacts"
                  }
                }
                """),
            cancellationToken);

        await _projectStorage.AddArtifactAsync(
            new GeneratedArtifact(
                project.Id,
                null,
                "Initial workspace artifact",
                "workspace-note",
                "README.md",
                "Starter project storage was created."),
            cancellationToken);
    }

    private async Task<AgentProject> GetOwnedOrAdminProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (projectId == Guid.Empty)
        {
            throw new DomainException("Project id is required.");
        }

        var userId = RequireSignedInUser();
        var project = await _projects.GetByIdAsync(projectId, cancellationToken);

        if (project is null)
        {
            throw new DomainException("The selected project was not found.");
        }

        if (!_currentUser.IsAdmin && !string.Equals(project.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new DomainException("You do not have permission to manage this project.");
        }

        return project;
    }

    private string RequireSignedInUser()
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new DomainException("You must sign in before managing projects.");
        }

        return _currentUser.UserId;
    }

    private static AgentProjectDto MapToDto(AgentProject project) =>
        new(project.Id, project.OwnerUserId, project.Name, project.Description, project.LlmModel, project.CreatedAtUtc);
}
