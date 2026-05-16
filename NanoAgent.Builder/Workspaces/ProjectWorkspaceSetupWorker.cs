using Microsoft.AspNetCore.SignalR;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Workspaces;

internal sealed class ProjectWorkspaceSetupWorker : BackgroundService
{
    private readonly IProjectWorkspaceSetupQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ProjectWorkspaceLogHub> _hubContext;
    private readonly ILogger<ProjectWorkspaceSetupWorker> _logger;

    public ProjectWorkspaceSetupWorker(
        IProjectWorkspaceSetupQueue queue,
        IServiceScopeFactory scopeFactory,
        IHubContext<ProjectWorkspaceLogHub> hubContext,
        ILogger<ProjectWorkspaceSetupWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid projectId;

            try
            {
                projectId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await RunProjectSetupAsync(projectId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Workspace setup failed for project {ProjectId}.", projectId);
                await PublishAsync(
                    projectId,
                    "error",
                    $"Workspace setup failed: {exception.Message}",
                    CancellationToken.None);
            }
        }
    }

    private async Task RunProjectSetupAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var projects = scope.ServiceProvider.GetRequiredService<IAgentProjectRepository>();
        var setupRunner = scope.ServiceProvider.GetRequiredService<IProjectWorkspaceSetupRunner>();
        var storage = scope.ServiceProvider.GetRequiredService<IProjectStorageRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var project = await projects.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            await PublishAsync(projectId, "error", "Workspace setup skipped because the project was not found.", cancellationToken);
            return;
        }

        await PublishAsync(project.Id, "info", "Workspace setup started.", cancellationToken);

        var result = await setupRunner.PrepareAsync(
            project,
            async (entry, token) => await PublishAsync(entry.ProjectId, entry.Level, entry.Message, token),
            cancellationToken);

        await storage.AddArtifactAsync(
            new GeneratedArtifact(
                project.Id,
                null,
                result.Succeeded ? "Workspace setup completed" : "Workspace setup needs attention",
                "workspace-setup",
                null,
                result.ToArtifactContent()),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await PublishAsync(
            project.Id,
            result.Succeeded ? "success" : "error",
            result.Succeeded ? "Workspace setup completed." : result.ErrorMessage ?? "Workspace setup needs attention.",
            cancellationToken);
    }

    private Task PublishAsync(Guid projectId, string level, string message, CancellationToken cancellationToken)
    {
        var payload = new ProjectWorkspaceLogPayload(projectId, level, message, DateTimeOffset.UtcNow);
        return _hubContext.Clients
            .Group(ProjectWorkspaceLogHub.GroupName(projectId))
            .SendAsync("workspaceLog", payload, cancellationToken);
    }
}
