using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Projects;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;
using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Application.Workspace;

internal sealed class ProjectWorkspaceService : IProjectWorkspaceService
{
    private const int EstimatedAssistantPlaceholderTokens = 0;

    private readonly ICurrentUserContext _currentUser;
    private readonly IAgentProjectRepository _projects;
    private readonly IProjectStorageRepository _storage;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectWorkspaceService(
        ICurrentUserContext currentUser,
        IAgentProjectRepository projects,
        IProjectStorageRepository storage,
        ITokenUsageService tokenUsageService,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _projects = projects;
        _storage = storage;
        _tokenUsageService = tokenUsageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectWorkspaceDto> GetWorkspaceAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var userId = RequireSignedInUser();
        var project = await GetOwnedOrAdminProjectAsync(projectId, cancellationToken);
        var usage = await _tokenUsageService.GetCurrentUsageForUserAsync(userId, cancellationToken);

        return await BuildWorkspaceAsync(project, usage, cancellationToken);
    }

    public async Task<ProjectWorkspaceDto> SubmitMessageAsync(
        SubmitProjectMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new DomainException("Message text is required.");
        }

        if (request.Message.Length > 8000)
        {
            throw new DomainException("Message text cannot be longer than 8,000 characters.");
        }

        var userId = RequireSignedInUser();
        var project = await GetOwnedOrAdminProjectAsync(request.ProjectId, cancellationToken);
        var selectedModel = string.IsNullOrWhiteSpace(request.LlmModel) ? project.LlmModel : request.LlmModel.Trim();

        await _tokenUsageService.EnsureModelAllowedAsync(userId, selectedModel, cancellationToken);

        var inputTokens = EstimateTokens(request.Message);
        var usage = await _tokenUsageService.RecordUsageAsync(
            userId,
            selectedModel,
            inputTokens,
            EstimatedAssistantPlaceholderTokens,
            cancellationToken);

        var userMessage = new ProjectMessage(
            project.Id,
            "user",
            request.Message,
            selectedModel,
            inputTokens,
            0);

        var run = new ProjectRun(
            project.Id,
            "queued",
            selectedModel,
            request.Message,
            inputTokens,
            0);

        var systemMessage = new ProjectMessage(
            project.Id,
            "assistant",
            "Request saved. A future generator can consume this run, generate files/artifacts, and update the preview.",
            selectedModel,
            0,
            0);

        await _storage.AddMessageAsync(userMessage, cancellationToken);
        await _storage.AddRunAsync(run, cancellationToken);
        await _storage.AddMessageAsync(systemMessage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildWorkspaceAsync(project, usage, cancellationToken);
    }

    private async Task<ProjectWorkspaceDto> BuildWorkspaceAsync(
        AgentProject project,
        TokenUsageDto usage,
        CancellationToken cancellationToken)
    {
        var files = await _storage.ListFilesAsync(project.Id, cancellationToken);
        var messages = await _storage.ListMessagesAsync(project.Id, cancellationToken: cancellationToken);
        var runs = await _storage.ListRunsAsync(project.Id, cancellationToken: cancellationToken);
        var artifacts = await _storage.ListArtifactsAsync(project.Id, cancellationToken: cancellationToken);

        return new ProjectWorkspaceDto(
            MapProject(project),
            files.Select(MapFile).ToList(),
            messages.Select(MapMessage).ToList(),
            runs.Select(MapRun).ToList(),
            artifacts.Select(MapArtifact).ToList(),
            usage,
            usage.AllowedLlmModels);
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
            throw new DomainException("You do not have permission to open this project.");
        }

        return project;
    }

    private string RequireSignedInUser()
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new DomainException("You must sign in before using the workspace.");
        }

        return _currentUser.UserId;
    }

    private static int EstimateTokens(string text)
    {
        var characters = string.IsNullOrWhiteSpace(text) ? 0 : text.Trim().Length;
        return Math.Max(1, (int)Math.Ceiling(characters / 4d));
    }

    private static AgentProjectDto MapProject(AgentProject project) =>
        new(project.Id, project.OwnerUserId, project.Name, project.Description, project.LlmModel, project.CreatedAtUtc);

    private static ProjectFileDto MapFile(ProjectFile file) =>
        new(file.Id, file.ProjectId, file.Path, file.Language, file.Content, file.CreatedAtUtc, file.UpdatedAtUtc);

    private static ProjectMessageDto MapMessage(ProjectMessage message) =>
        new(message.Id, message.ProjectId, message.Role, message.Content, message.LlmModel, message.InputTokens, message.OutputTokens, message.CreatedAtUtc);

    private static ProjectRunDto MapRun(ProjectRun run) =>
        new(run.Id, run.ProjectId, run.Status, run.RequestedModel, run.Prompt, run.InputTokens, run.OutputTokens, run.StartedAtUtc, run.CompletedAtUtc, run.ErrorMessage);

    private static GeneratedArtifactDto MapArtifact(GeneratedArtifact artifact) =>
        new(artifact.Id, artifact.ProjectId, artifact.ProjectRunId, artifact.Name, artifact.ArtifactType, artifact.Path, artifact.Content, artifact.CreatedAtUtc);
}
