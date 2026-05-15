using NanoAgent.Builder.Application.Saas;

namespace NanoAgent.Builder.Application.Admin;

public sealed record AdminDashboardDto(
    int TotalUsers,
    int TotalProjects,
    int ActiveSubscriptions,
    IReadOnlyList<SaasPlanDto> Plans,
    IReadOnlyList<AdminUserRowDto> Users);
