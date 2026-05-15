namespace NanoAgent.Builder.Application.Workspace;

public sealed record ProjectMessageDto(
    Guid Id,
    Guid ProjectId,
    string Role,
    string Content,
    string LlmModel,
    int InputTokens,
    int OutputTokens,
    DateTimeOffset CreatedAtUtc);
