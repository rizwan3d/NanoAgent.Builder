using System.Text;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.LLM;
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
    private readonly ILLMProvider _provider;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectWorkspaceService(
        ICurrentUserContext currentUser,
        IAgentProjectRepository projects,
        IProjectStorageRepository storage,
        IProjectWorkspaceFileSystem workspaceFileSystem,
        ITokenUsageService tokenUsageService,
        ILLMProvider provider,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _projects = projects;
        _storage = storage;
        _workspaceFileSystem = workspaceFileSystem;
        _tokenUsageService = tokenUsageService;
        _provider = provider;
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

        var estimatedInputTokens = EstimateTokens(request.Message) + EstimateFileTokens(existingFiles);
        await _tokenUsageService.EnsureCanUseTokensAsync(userId, selectedModel, estimatedInputTokens, cancellationToken);

        var userMessage = new ProjectMessage(
            project.Id,
            "user",
            request.Message,
            selectedModel,
            estimatedInputTokens,
            0);

        var run = new ProjectRun(
            project.Id,
            "running",
            selectedModel,
            request.Message,
            estimatedInputTokens,
            0);

        await _storage.AddMessageAsync(userMessage, cancellationToken);
        await _storage.AddRunAsync(run, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var providerRequest = new LLMGenerationRequest(
                project.Id,
                project.Name,
                project.Description,
                selectedModel,
                request.Message,
                existingFiles.Select(MapFile).ToList());

            var textBuilder = new StringBuilder();
            var patches = new List<GeneratedFilePatch>();
            var inputTokens = estimatedInputTokens;
            var outputTokens = 0;

            await foreach (var streamEvent in _provider.GenerateFilePatchesAsync(providerRequest, cancellationToken))
            {
                switch (streamEvent)
                {
                    case LLMTextDelta textDelta:
                        textBuilder.Append(textDelta.Text);
                        break;

                    case LLMFilePatchDelta patchDelta:
                        patches.Add(patchDelta.Patch);
                        break;

                    case LLMUsageDelta usageDelta:
                        inputTokens = usageDelta.InputTokens > 0 ? usageDelta.InputTokens : inputTokens;
                        outputTokens = usageDelta.OutputTokens > 0 ? usageDelta.OutputTokens : outputTokens;
                        break;

                    case LLMGenerationCompleted completed:
                        if (completed.Patches.Count > 0)
                        {
                            patches.Clear();
                            patches.AddRange(completed.Patches);
                        }

                        inputTokens = completed.InputTokens > 0 ? completed.InputTokens : inputTokens;
                        outputTokens = completed.OutputTokens > 0 ? completed.OutputTokens : outputTokens;
                        break;
                }
            }

            if (patches.Count == 0)
            {
                throw new DomainException("No file patches were returned.");
            }

            if (outputTokens <= 0)
            {
                outputTokens = EstimateTokens(textBuilder.ToString()) + patches.Sum(patch => EstimateTokens(patch.Content));
            }

            var usage = await _tokenUsageService.RecordUsageAsync(
                userId,
                selectedModel,
                inputTokens,
                outputTokens,
                cancellationToken);

            var selectedFileId = await ApplyPatchesAsync(project, run.Id, patches, cancellationToken);
            run.Complete(outputTokens);

            var summary = BuildPatchSummary(patches);
            await _storage.AddMessageAsync(
                new ProjectMessage(
                    project.Id,
                    "assistant",
                    summary,
                    selectedModel,
                    0,
                    outputTokens),
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return await BuildWorkspaceAsync(project, usage, selectedFileId, cancellationToken);
        }
        catch (Exception exception) when (exception is DomainException or HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            run.Fail(exception.Message);
            await _storage.AddMessageAsync(
                new ProjectMessage(
                    project.Id,
                    "assistant",
                    $"The run could not be completed: {exception.Message}",
                    selectedModel,
                    0,
                    0),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
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

    private async Task<Guid?> ApplyPatchesAsync(
        AgentProject project,
        Guid runId,
        IReadOnlyList<GeneratedFilePatch> patches,
        CancellationToken cancellationToken)
    {
        Guid? selectedFileId = null;

        foreach (var patch in patches)
        {
            EnsureSafePatch(patch);
            var file = await _storage.GetFileByPathAsync(project.Id, patch.Path, cancellationToken);
            if (file is null)
            {
                file = new ProjectFile(project.Id, patch.Path, patch.Language, patch.Content);
                await _storage.AddFileAsync(file, cancellationToken);
            }
            else
            {
                file.SetLanguage(patch.Language);
                file.UpdateContent(patch.Content);
            }

            await _storage.AddArtifactAsync(
                new GeneratedArtifact(
                    project.Id,
                    runId,
                    $"Updated {patch.Path}",
                    "file-patch",
                    patch.Path,
                    patch.Content),
                cancellationToken);

            await _workspaceFileSystem.WriteFileAsync(project, file, cancellationToken);
            selectedFileId = file.Id;
        }

        return selectedFileId;
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

    private static void EnsureSafePatch(GeneratedFilePatch patch)
    {
        if (string.IsNullOrWhiteSpace(patch.Path))
        {
            throw new DomainException("A file patch path is required.");
        }

        if (string.IsNullOrWhiteSpace(patch.Content))
        {
            throw new DomainException($"File patch '{patch.Path}' does not include content.");
        }

        var normalizedPath = patch.Path.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(normalizedPath) || normalizedPath.StartsWith("/", StringComparison.Ordinal))
        {
            throw new DomainException("File patches must use relative paths.");
        }

        if (normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(segment => segment is "." or ".."))
        {
            throw new DomainException("File patches cannot use '.' or '..' path segments.");
        }
    }

    private static int EstimateTokens(string text)
    {
        var characters = string.IsNullOrWhiteSpace(text) ? 0 : text.Trim().Length;
        return Math.Max(1, (int)Math.Ceiling(characters / 4d));
    }

    private static int EstimateFileTokens(IReadOnlyList<ProjectFile> files) =>
        files.Sum(file => EstimateTokens(file.Content));

    private static Guid? ResolveSelectedFileId(IReadOnlyList<ProjectFile> files, Guid? selectedFileId)
    {
        if (selectedFileId.HasValue && files.Any(file => file.Id == selectedFileId.Value))
        {
            return selectedFileId.Value;
        }

        return files.FirstOrDefault()?.Id;
    }

    private static string BuildPatchSummary(IReadOnlyList<GeneratedFilePatch> patches)
    {
        var paths = string.Join(", ", patches.Select(patch => $"`{patch.Path}`"));
        return patches.Count == 1
            ? $"Updated {paths}. Open the Code tab to review the change."
            : $"Updated {patches.Count} files: {paths}. Open the Code tab to review the changes.";
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
