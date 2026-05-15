using NanoAgent.Builder.Application.Abstractions;
using NanoAgent.Builder.Application.Saas;

namespace NanoAgent.Builder.Application.Admin;

internal sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IApplicationUserReadRepository _users;
    private readonly IAgentProjectRepository _projects;
    private readonly ISaasPlanRepository _plans;
    private readonly IUserSubscriptionRepository _subscriptions;
    private readonly IUserTokenUsageRepository _tokenUsage;

    public AdminDashboardService(
        IApplicationUserReadRepository users,
        IAgentProjectRepository projects,
        ISaasPlanRepository plans,
        IUserSubscriptionRepository subscriptions,
        IUserTokenUsageRepository tokenUsage)
    {
        _users = users;
        _projects = projects;
        _plans = plans;
        _subscriptions = subscriptions;
        _tokenUsage = tokenUsage;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.ListAsync(cancellationToken);
        var projects = await _projects.ListAllAsync(cancellationToken);
        var plans = await _plans.ListAllAsync(cancellationToken);
        var subscriptions = await _subscriptions.ListCurrentAsync(cancellationToken);
        var tokenUsage = await _tokenUsage.ListForOpenPeriodsAsync(DateTimeOffset.UtcNow, cancellationToken);

        var plansById = plans.ToDictionary(plan => plan.Id);
        var subscriptionsByUserId = subscriptions.ToDictionary(subscription => subscription.UserId);
        var projectsByUserId = projects
            .GroupBy(project => project.OwnerUserId)
            .ToDictionary(group => group.Key, group => group.Count());
        var tokenUsageByUserId = tokenUsage
            .GroupBy(usage => usage.UserId)
            .ToDictionary(group => group.Key, group => group.Sum(usage => usage.UsedTokens));

        var userRows = users
            .OrderBy(user => user.Email)
            .Select(user =>
            {
                subscriptionsByUserId.TryGetValue(user.Id, out var subscription);
                var planName = "No package";
                var status = "None";
                var monthlyTokenLimit = 0;
                IReadOnlyList<string> allowedModels = Array.Empty<string>();

                if (subscription is not null && plansById.TryGetValue(subscription.SubscriptionPlanId, out var plan))
                {
                    planName = plan.Name;
                    status = subscription.Status.ToString();
                    monthlyTokenLimit = plan.MonthlyTokenLimit;
                    allowedModels = plan.GetAllowedLlmModels();
                }

                return new AdminUserRowDto(
                    user.Id,
                    user.Email,
                    user.DisplayName,
                    user.Roles,
                    planName,
                    status,
                    projectsByUserId.GetValueOrDefault(user.Id),
                    tokenUsageByUserId.GetValueOrDefault(user.Id),
                    monthlyTokenLimit,
                    allowedModels,
                    user.CreatedAtUtc);
            })
            .ToList();

        var planDtos = plans
            .OrderBy(plan => plan.DisplayOrder)
            .Select(plan => new SaasPlanDto(
                plan.Id,
                plan.Code,
                plan.Name,
                plan.Description,
                plan.Tier,
                plan.MonthlyPrice,
                plan.Currency,
                plan.ProjectLimit,
                plan.MonthlyTokenLimit,
                plan.GetAllowedLlmModels(),
                plan.IsActive,
                plan.DisplayOrder,
                plan.StripePriceId))
            .ToList();

        return new AdminDashboardDto(
            users.Count,
            projects.Count,
            subscriptions.Count,
            planDtos,
            userRows);
    }
}
