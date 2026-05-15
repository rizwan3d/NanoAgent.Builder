using NanoAgent.Builder.Domain.Projects;

namespace NanoAgent.Builder.Application.Abstractions;

public interface IProjectStorageRepository
{
    Task<IReadOnlyList<ProjectFile>> ListFilesAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectMessage>> ListMessagesAsync(Guid projectId, int take = 50, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectRun>> ListRunsAsync(Guid projectId, int take = 25, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GeneratedArtifact>> ListArtifactsAsync(Guid projectId, int take = 25, CancellationToken cancellationToken = default);

    Task AddFileAsync(ProjectFile file, CancellationToken cancellationToken = default);

    Task AddMessageAsync(ProjectMessage message, CancellationToken cancellationToken = default);

    Task AddRunAsync(ProjectRun run, CancellationToken cancellationToken = default);

    Task AddArtifactAsync(GeneratedArtifact artifact, CancellationToken cancellationToken = default);
}
