namespace NanoAgent.Builder.Application.Admin;

public sealed record ApplicationUserSummaryDto(
    string Id,
    string Email,
    string? DisplayName,
    IReadOnlyCollection<string> Roles,
    DateTimeOffset CreatedAtUtc);
