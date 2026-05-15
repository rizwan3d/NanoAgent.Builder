namespace NanoAgent.Builder.Application.Projects;

public sealed record AgentProjectDto(
    Guid Id,
    string OwnerUserId,
    string Name,
    string? Description,
    string LlmModel,
    DateTimeOffset CreatedAtUtc);
