namespace NanoAgent.Builder.Application.Workspace;

public interface IProjectWorkspaceService
{
    Task<ProjectWorkspaceDto> GetWorkspaceAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<ProjectWorkspaceDto> SubmitMessageAsync(SubmitProjectMessageRequest request, CancellationToken cancellationToken = default);
}
