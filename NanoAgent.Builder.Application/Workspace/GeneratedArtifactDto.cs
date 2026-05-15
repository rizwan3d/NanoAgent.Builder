namespace NanoAgent.Builder.Application.Workspace;

public sealed record GeneratedArtifactDto(
    Guid Id,
    Guid ProjectId,
    Guid? ProjectRunId,
    string Name,
    string ArtifactType,
    string? Path,
    string? Content,
    DateTimeOffset CreatedAtUtc);
