namespace NanoAgent.Builder.Application.Workspace;

public sealed record ProjectFileDto(
    Guid Id,
    Guid ProjectId,
    string Path,
    string? Language,
    string Content,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
