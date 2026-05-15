using NanoAgent.Builder.Domain.Common;

namespace NanoAgent.Builder.Domain.Projects;

public sealed class GeneratedArtifact : Entity
{
    private GeneratedArtifact()
    {
    }

    public GeneratedArtifact(Guid projectId, Guid? projectRunId, string name, string artifactType, string? path, string? content)
    {
        SetProject(projectId);
        ProjectRunId = projectRunId;
        SetName(name);
        SetArtifactType(artifactType);
        SetPath(path);
        Content = string.IsNullOrWhiteSpace(content) ? null : content;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid ProjectId { get; private set; }

    public Guid? ProjectRunId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string ArtifactType { get; private set; } = string.Empty;

    public string? Path { get; private set; }

    public string? Content { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public AgentProject? Project { get; private set; }

    public ProjectRun? ProjectRun { get; private set; }

    private void SetProject(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new DomainException("An artifact must belong to a project.");
        }

        ProjectId = projectId;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Artifact name is required.");
        }

        if (name.Length > 200)
        {
            throw new DomainException("Artifact name cannot be longer than 200 characters.");
        }

        Name = name.Trim();
    }

    private void SetArtifactType(string artifactType)
    {
        if (string.IsNullOrWhiteSpace(artifactType))
        {
            throw new DomainException("Artifact type is required.");
        }

        if (artifactType.Length > 100)
        {
            throw new DomainException("Artifact type cannot be longer than 100 characters.");
        }

        ArtifactType = artifactType.Trim().ToLowerInvariant();
    }

    private void SetPath(string? path)
    {
        if (path is { Length: > 500 })
        {
            throw new DomainException("Artifact path cannot be longer than 500 characters.");
        }

        Path = string.IsNullOrWhiteSpace(path) ? null : path.Trim().Replace('\\', '/');
    }
}
