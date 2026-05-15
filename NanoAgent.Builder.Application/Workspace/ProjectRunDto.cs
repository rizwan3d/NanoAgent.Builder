namespace NanoAgent.Builder.Application.Workspace;

public sealed record ProjectRunDto(
    Guid Id,
    Guid ProjectId,
    string Status,
    string RequestedModel,
    string? Prompt,
    int InputTokens,
    int OutputTokens,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? ErrorMessage);
