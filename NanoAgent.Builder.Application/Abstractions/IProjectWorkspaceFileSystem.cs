using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Application.Abstractions;

public interface IProjectWorkspaceFileSystem
{
    string GetProjectRootPath(AgentProject project);

    Task EnsureProjectWorkspaceAsync(
        AgentProject project,
        IReadOnlyList<ProjectFile> files,
        CancellationToken cancellationToken = default);

    Task WriteFileAsync(
        AgentProject project,
        ProjectFile file,
        CancellationToken cancellationToken = default);
}
