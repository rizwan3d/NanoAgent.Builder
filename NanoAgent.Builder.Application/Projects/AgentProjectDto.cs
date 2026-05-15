namespace NanoAgent.Builder.Application.Projects;

public sealed record AgentProjectDto(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAtUtc);
