namespace NanoAgent.Builder.Application.Projects;

public sealed record AgentProjectDto(
    Guid Id,
    string OwnerUserId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAtUtc);
