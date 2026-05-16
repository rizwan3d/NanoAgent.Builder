using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Security;

namespace NanoAgent.Builder.Workspaces;

[Authorize]
public sealed class ProjectWorkspaceLogHub : Hub
{
    private readonly IAgentProjectRepository _projects;

    public ProjectWorkspaceLogHub(IAgentProjectRepository projects)
    {
        _projects = projects;
    }

    public async Task JoinProject(Guid projectId)
    {
        await EnsureProjectAccessAsync(projectId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(projectId));
    }

    public async Task LeaveProject(Guid projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(projectId));
    }

    public static string GroupName(Guid projectId) => $"workspace-logs-{projectId:N}";

    private async Task EnsureProjectAccessAsync(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new HubException("Project id is required.");
        }

        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new HubException("A signed-in account is required.");
        }

        var project = await _projects.GetByIdAsync(projectId, Context.ConnectionAborted);
        if (project is null)
        {
            throw new HubException("Project was not found.");
        }

        var isAdmin = Context.User?.IsInRole(ApplicationRoles.Admin) == true;
        if (!isAdmin && !string.Equals(project.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new HubException("Project access was denied.");
        }
    }
}

public sealed record ProjectWorkspaceLogPayload(
    Guid ProjectId,
    string Level,
    string Message,
    DateTimeOffset CreatedAtUtc);
