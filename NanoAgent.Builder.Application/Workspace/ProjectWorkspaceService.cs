using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Projects;
using NanoAgent.Builder.Application.Saas;
using NanoAgent.Builder.Domain.Common;
using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Application.Workspace;

internal sealed class ProjectWorkspaceService : IProjectWorkspaceService
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IAgentProjectRepository _projects;
    private readonly IProjectStorageRepository _storage;
    private readonly IProjectWorkspaceFileSystem _workspaceFileSystem;
    private readonly ITokenUsageService _tokenUsageService;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectWorkspaceService(
        ICurrentUserContext currentUser,
        IAgentProjectRepository projects,
        IProjectStorageRepository storage,
        IProjectWorkspaceFileSystem workspaceFileSystem,
        ITokenUsageService tokenUsageService,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _projects = projects;
        _storage = storage;
        _workspaceFileSystem = workspaceFileSystem;
        _tokenUsageService = tokenUsageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProjectWorkspaceDto> GetWorkspaceAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var userId = RequireSignedInUser();
        var project = await GetOwnedOrAdminProjectAsync(projectId, cancellationToken);
        var usage = await _tokenUsageService.GetCurrentUsageForUserAsync(userId, cancellationToken);

        return await BuildWorkspaceAsync(project, usage, null, cancellationToken);
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
        var existingFiles = await _storage.ListFilesAsync(project.Id, cancellationToken);

        await _tokenUsageService.EnsureModelAllowedAsync(userId, selectedModel, cancellationToken);

        var inputTokens = EstimateTokens(request.Message);
        var generatedFilePath = BuildGeneratedFilePath(existingFiles);
        var assistantResponse = $"Created `{generatedFilePath}` from your request. Open it in the Code tab to review or edit it manually.";
        var outputTokens = EstimateTokens(assistantResponse);
        var usage = await _tokenUsageService.RecordUsageAsync(
            userId,
            selectedModel,
            inputTokens,
            outputTokens,
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
            "running",
            selectedModel,
            request.Message,
            inputTokens,
            0);

        var generatedFile = new ProjectFile(
            project.Id,
            generatedFilePath,
            "markdown",
            BuildGeneratedFileContent(project.Name, request.Message, selectedModel));

        run.Complete(outputTokens);

        var systemMessage = new ProjectMessage(
            project.Id,
            "assistant",
            assistantResponse,
            selectedModel,
            0,
            outputTokens);

        await _storage.AddMessageAsync(userMessage, cancellationToken);
        await _storage.AddRunAsync(run, cancellationToken);
        await _storage.AddFileAsync(generatedFile, cancellationToken);
        await _storage.AddMessageAsync(systemMessage, cancellationToken);
        await _storage.AddArtifactAsync(
            new GeneratedArtifact(
                project.Id,
                run.Id,
                $"Generated file {generatedFile.Path}",
                "generated-file",
                generatedFile.Path,
                generatedFile.Content),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _workspaceFileSystem.WriteFileAsync(project, generatedFile, cancellationToken);

        return await BuildWorkspaceAsync(project, usage, generatedFile.Id, cancellationToken);
    }

    public async Task<ProjectWorkspaceDto> UpdateFileAsync(
        UpdateProjectFileRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireSignedInUser();
        var project = await GetOwnedOrAdminProjectAsync(request.ProjectId, cancellationToken);

        if (request.FileId == Guid.Empty)
        {
            throw new DomainException("A file must be selected before saving.");
        }

        var file = await _storage.GetFileAsync(project.Id, request.FileId, cancellationToken);
        if (file is null)
        {
            throw new DomainException("The selected file was not found.");
        }

        file.UpdateContent(request.Content);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _workspaceFileSystem.WriteFileAsync(project, file, cancellationToken);

        var usage = await _tokenUsageService.GetCurrentUsageForUserAsync(userId, cancellationToken);
        return await BuildWorkspaceAsync(project, usage, file.Id, cancellationToken);
    }

    private async Task<ProjectWorkspaceDto> BuildWorkspaceAsync(
        AgentProject project,
        TokenUsageDto usage,
        Guid? selectedFileId,
        CancellationToken cancellationToken)
    {
        var files = await _storage.ListFilesAsync(project.Id, cancellationToken);
        var messages = await _storage.ListMessagesAsync(project.Id, cancellationToken: cancellationToken);
        var runs = await _storage.ListRunsAsync(project.Id, cancellationToken: cancellationToken);
        var artifacts = await _storage.ListArtifactsAsync(project.Id, cancellationToken: cancellationToken);
        var resolvedSelectedFileId = ResolveSelectedFileId(files, selectedFileId);
        await _workspaceFileSystem.EnsureProjectWorkspaceAsync(project, files, cancellationToken);

        return new ProjectWorkspaceDto(
            MapProject(project),
            files.Select(MapFile).ToList(),
            messages.Select(MapMessage).ToList(),
            runs.Select(MapRun).ToList(),
            artifacts.Select(MapArtifact).ToList(),
            usage,
            usage.AllowedLlmModels,
            resolvedSelectedFileId,
            _workspaceFileSystem.GetProjectRootPath(project),
            "npm install",
            "npm run dev");
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

    private static Guid? ResolveSelectedFileId(IReadOnlyList<ProjectFile> files, Guid? selectedFileId)
    {
        if (selectedFileId.HasValue && files.Any(file => file.Id == selectedFileId.Value))
        {
            return selectedFileId.Value;
        }

        return files.FirstOrDefault()?.Id;
    }

    private static string BuildGeneratedFilePath(IReadOnlyList<ProjectFile> files)
    {
        var nextIndex = files.Count(file => file.Path.StartsWith("generated/", StringComparison.OrdinalIgnoreCase)) + 1;
        return $"generated/run-{nextIndex:000}.md";
    }

    private static string BuildGeneratedFileContent(string projectName, string message, string model)
    {
        return $"""
                # Generated Workspace Note

                Project: {projectName}
                Model: {model}
                Created: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC

                ## User Request

                {message.Trim()}

                ## Suggested Next Step

                Review this generated note, then update the app files in the workspace editor.
                """;
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
